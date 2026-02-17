using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YMS.Server.Data;
using YMS.Server.Models;

namespace YMS.Server;

[ApiController]
[Route("api/[controller]")]
public class MainController(AppDbContext context, IOpenLabMailer mailer) : ControllerBase
{
    [HttpGet("lookups")]
    public async Task<ActionResult<OpenLabLookupResponse>> GetLookups([FromQuery] string? site)
    {
        var normalizedSite = OpenLabCodes.NormalizeSite(site);

        var lines = await context.Equipments
            .AsNoTracking()
            .Select(e => e.LineId)
            .Distinct()
            .OrderBy(line => line)
            .ToListAsync();

        var classes = await context.Equipments
            .AsNoTracking()
            .Select(e => e.LargeClass)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        var purposes = await context.Reservations
            .AsNoTracking()
            .Select(r => r.Purpose)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync();

        var equipments = await context.Equipments
            .AsNoTracking()
            .OrderBy(e => e.LineId)
            .ThenBy(e => e.LargeClass)
            .ThenBy(e => e.EqpId)
            .Select(e => new OpenLabEquipmentRow(
                e.Id,
                e.EqpId,
                e.LineId,
                e.LargeClass,
                e.EqpType,
                e.EqpGroupName,
                0))
            .ToListAsync();

        var employees = await context.Employees
            .AsNoTracking()
            .Where(e => e.Site == normalizedSite || normalizedSite == "ALL")
            .OrderBy(e => e.HName)
            .Select(e => new OpenLabEmployeeRow(
                e.UserId,
                e.EmpNo,
                e.HName,
                e.DeptCode,
                e.DeptName,
                e.SingleId,
                e.SingleMailAddr,
                e.Site))
            .ToListAsync();

        return Ok(new OpenLabLookupResponse(
            lines,
            classes,
            purposes,
            equipments,
            employees));
    }

    [HttpGet("openlab-resv")]
    public async Task<ActionResult<IEnumerable<OpenLabReservationRow>>> GetOpenLabReservations(
        [FromQuery] string? lineId,
        [FromQuery] string? largeClass,
        [FromQuery] string? purpose,
        [FromQuery] string? site)
    {
        var lineIds = SplitFilter(lineId);
        var classes = SplitFilter(largeClass);
        var normalizedSite = OpenLabCodes.NormalizeSite(site);

        var query = context.Reservations.AsNoTracking().AsQueryable();

        if (lineIds.Count > 0)
        {
            query = query.Where(r => lineIds.Contains(r.LineId));
        }

        if (classes.Count > 0)
        {
            query = query.Where(r => classes.Contains(r.LargeClass));
        }

        if (!string.IsNullOrWhiteSpace(purpose) && !string.Equals(purpose, "ALL", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(r => EF.Functions.Like(r.Purpose, $"%{purpose.Trim()}%"));
        }

        var rows = await (
            from r in query
            join e in context.Employees.AsNoTracking() on r.EmpNum equals e.EmpNo into empGroup
            from emp in empGroup.DefaultIfEmpty()
            where normalizedSite == "ALL" || emp == null || emp.Site == normalizedSite
            orderby r.ReservedDate, r.EqpId
            select new OpenLabReservationRow(
                r.Id,
                r.IssueNo,
                r.EquipmentId,
                r.EqpId,
                r.LineId,
                r.LargeClass,
                r.EmpName,
                r.EmpNum,
                r.ReservedDate,
                r.Purpose,
                r.Status,
                r.CreatedAt,
                Array.Empty<string>()))
            .ToListAsync();

        return Ok(rows);
    }

    [HttpGet("openlab-resv/{id:int}")]
    public async Task<ActionResult<OpenLabReservationRow>> GetOpenLabReservation(int id)
    {
        var reservation = await context.Reservations.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);

        if (reservation is null)
        {
            return NotFound();
        }

        var receivers = await context.ApprovalNotifications
            .AsNoTracking()
            .Where(n => n.IssueNo == reservation.IssueNo && n.ApprovalSeq == "0")
            .Select(n => n.NotiUserId)
            .OrderBy(userId => userId)
            .ToListAsync();

        return Ok(new OpenLabReservationRow(
            reservation.Id,
            reservation.IssueNo,
            reservation.EquipmentId,
            reservation.EqpId,
            reservation.LineId,
            reservation.LargeClass,
            reservation.EmpName,
            reservation.EmpNum,
            reservation.ReservedDate,
            reservation.Purpose,
            reservation.Status,
            reservation.CreatedAt,
            receivers));
    }

    [HttpPost("openlab-resv")]
    public async Task<ActionResult<OpenLabReservationRow>> CreateOpenLabReservation([FromBody] OpenLabReservationUpsertRequest request)
    {
        var equipment = await context.Equipments.FirstOrDefaultAsync(e => e.Id == request.EquipmentId);

        if (equipment is null)
        {
            return BadRequest("유효한 설비를 선택하세요.");
        }

        var authType = OpenLabCodes.ParseAuthType(request.AuthType).ToCode();
        var site = OpenLabCodes.NormalizeSite(request.Site);

        if (site == "HQ")
        {
            var hasAuthority = await HasReceptionAuthorityAsync(site, equipment.EqpId, authType, request.EmpNum, request.SingleId);
            if (!hasAuthority)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "접수 권한이 없습니다.");
            }
        }

        var issueNo = await CreateIssueNoAsync();

        var reservation = new Reservation
        {
            IssueNo = issueNo,
            EquipmentId = equipment.Id,
            EqpId = equipment.EqpId,
            LineId = equipment.LineId,
            LargeClass = equipment.LargeClass,
            EmpName = request.EmpName.Trim(),
            EmpNum = request.EmpNum.Trim(),
            ReservedDate = NormalizeDate(request.ReservedDate),
            Purpose = request.Purpose.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status)
                ? OpenLabCodes.ToDisplay(OpenLabReservationStatus.Waiting)
                : request.Status.Trim(),
            CreatedAt = DateTime.UtcNow,
        };

        context.Reservations.Add(reservation);
        var receiverEmails = await UpsertIssueReceiversAsync(issueNo, request.ReceiverUserIds, request.EmpNum);
        await context.SaveChangesAsync();

        await mailer.SendReservationEventAsync(new OpenLabMailMessage(
            issueNo,
            "CREATE",
            $"[OPENLAB 예약등록] {equipment.EqpId}",
            receiverEmails,
            $"{reservation.EmpName}님이 {reservation.ReservedDate:yyyy-MM-dd HH:mm}에 {reservation.EqpId} 예약을 등록했습니다."));

        return CreatedAtAction(nameof(GetOpenLabReservation), new { id = reservation.Id }, new OpenLabReservationRow(
            reservation.Id,
            reservation.IssueNo,
            reservation.EquipmentId,
            reservation.EqpId,
            reservation.LineId,
            reservation.LargeClass,
            reservation.EmpName,
            reservation.EmpNum,
            reservation.ReservedDate,
            reservation.Purpose,
            reservation.Status,
            reservation.CreatedAt,
            request.ReceiverUserIds));
    }

    [HttpPut("openlab-resv/{id:int}")]
    public async Task<ActionResult<OpenLabReservationRow>> UpdateOpenLabReservation(int id, [FromBody] OpenLabReservationUpsertRequest request)
    {
        var reservation = await context.Reservations.FirstOrDefaultAsync(r => r.Id == id);

        if (reservation is null)
        {
            return NotFound();
        }

        var equipment = await context.Equipments.FirstOrDefaultAsync(e => e.Id == request.EquipmentId);

        if (equipment is null)
        {
            return BadRequest("유효한 설비를 선택하세요.");
        }

        var authType = OpenLabCodes.ParseAuthType(request.AuthType).ToCode();
        var site = OpenLabCodes.NormalizeSite(request.Site);

        if (site == "HQ")
        {
            var hasAuthority = await HasReceptionAuthorityAsync(site, equipment.EqpId, authType, request.EmpNum, request.SingleId);
            if (!hasAuthority)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "접수 권한이 없습니다.");
            }
        }

        reservation.EquipmentId = equipment.Id;
        reservation.EqpId = equipment.EqpId;
        reservation.LineId = equipment.LineId;
        reservation.LargeClass = equipment.LargeClass;
        reservation.EmpName = request.EmpName.Trim();
        reservation.EmpNum = request.EmpNum.Trim();
        reservation.ReservedDate = NormalizeDate(request.ReservedDate);
        reservation.Purpose = request.Purpose.Trim();
        reservation.Status = string.IsNullOrWhiteSpace(request.Status)
            ? reservation.Status
            : request.Status.Trim();

        var receiverEmails = await UpsertIssueReceiversAsync(reservation.IssueNo, request.ReceiverUserIds, request.EmpNum);
        await context.SaveChangesAsync();

        await mailer.SendReservationEventAsync(new OpenLabMailMessage(
            reservation.IssueNo,
            "UPDATE",
            $"[OPENLAB 예약변경] {reservation.EqpId}",
            receiverEmails,
            $"{reservation.EmpName}님 예약이 변경되었습니다. ({reservation.ReservedDate:yyyy-MM-dd HH:mm})"));

        return Ok(new OpenLabReservationRow(
            reservation.Id,
            reservation.IssueNo,
            reservation.EquipmentId,
            reservation.EqpId,
            reservation.LineId,
            reservation.LargeClass,
            reservation.EmpName,
            reservation.EmpNum,
            reservation.ReservedDate,
            reservation.Purpose,
            reservation.Status,
            reservation.CreatedAt,
            request.ReceiverUserIds));
    }

    [HttpDelete("openlab-resv/{id:int}")]
    public async Task<IActionResult> DeleteOpenLabReservation(int id)
    {
        var reservation = await context.Reservations.FirstOrDefaultAsync(r => r.Id == id);

        if (reservation is null)
        {
            return NotFound();
        }

        var notifications = await context.ApprovalNotifications
            .Where(n => n.IssueNo == reservation.IssueNo)
            .ToListAsync();

        var mailTargets = notifications
            .Select(n => n.NotiSingleMailAddr)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (notifications.Count > 0)
        {
            context.ApprovalNotifications.RemoveRange(notifications);
        }

        context.Reservations.Remove(reservation);
        await context.SaveChangesAsync();

        await mailer.SendReservationEventAsync(new OpenLabMailMessage(
            reservation.IssueNo,
            "DELETE",
            $"[OPENLAB 예약취소] {reservation.EqpId}",
            mailTargets,
            $"{reservation.EmpName}님의 예약이 취소되었습니다."));

        return NoContent();
    }

    [HttpGet("openlab-eqp")]
    public async Task<ActionResult<IEnumerable<OpenLabEquipmentRow>>> GetOpenLabEquipments(
        [FromQuery] string? lineId,
        [FromQuery] string? largeClass)
    {
        var lineIds = SplitFilter(lineId);
        var classes = SplitFilter(largeClass);

        var query = context.Equipments.AsNoTracking().AsQueryable();

        if (lineIds.Count > 0)
        {
            query = query.Where(e => lineIds.Contains(e.LineId));
        }

        if (classes.Count > 0)
        {
            query = query.Where(e => classes.Contains(e.LargeClass));
        }

        var rows = await (
            from e in query
            join r in context.Reservations.AsNoTracking() on e.EqpId equals r.EqpId into resvGroup
            orderby e.LineId, e.LargeClass, e.EqpId
            select new OpenLabEquipmentRow(
                e.Id,
                e.EqpId,
                e.LineId,
                e.LargeClass,
                e.EqpType,
                e.EqpGroupName,
                resvGroup.Count()))
            .ToListAsync();

        return Ok(rows);
    }

    [HttpGet("openlab-auth")]
    public async Task<ActionResult<IEnumerable<OpenLabAuthRow>>> GetOpenLabAuths(
        [FromQuery] string? site,
        [FromQuery] string? eqpName,
        [FromQuery] string? authType)
    {
        var normalizedSite = OpenLabCodes.NormalizeSite(site);
        var normalizedEqpName = (eqpName ?? string.Empty).Trim().ToUpperInvariant();
        var normalizedAuthType = (authType ?? string.Empty).Trim().ToUpperInvariant();

        var query =
            from a in context.OpenLabAuths.AsNoTracking()
            join e in context.Employees.AsNoTracking() on a.EmpNo equals e.EmpNo
            select new OpenLabAuthRow(
                a.Id,
                a.Site,
                a.EqpName,
                a.AuthType,
                a.EmpNo,
                e.UserId,
                e.HName,
                e.SingleId,
                e.DeptCode,
                e.DeptName);

        if (!string.IsNullOrWhiteSpace(normalizedSite) && normalizedSite != "ALL")
        {
            query = query.Where(x => x.Site == normalizedSite);
        }

        if (!string.IsNullOrWhiteSpace(normalizedEqpName))
        {
            query = query.Where(x => x.EqpName == normalizedEqpName);
        }

        if (!string.IsNullOrWhiteSpace(normalizedAuthType))
        {
            query = query.Where(x => x.AuthType == normalizedAuthType);
        }

        var rows = await query
            .OrderBy(x => x.Site)
            .ThenBy(x => x.EqpName)
            .ThenBy(x => x.AuthType)
            .ThenBy(x => x.EmpName)
            .ToListAsync();

        return Ok(rows);
    }

    [HttpPost("openlab-auth")]
    public async Task<ActionResult<OpenLabAuthRow>> CreateOpenLabAuth([FromBody] OpenLabAuthUpsertRequest request)
    {
        var site = OpenLabCodes.NormalizeSite(request.Site);
        var eqpName = (request.EqpName ?? string.Empty).Trim().ToUpperInvariant();
        var authType = OpenLabCodes.ParseAuthType(request.AuthType).ToCode();
        var empNo = (request.EmpNo ?? string.Empty).Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(eqpName) || string.IsNullOrWhiteSpace(empNo))
        {
            return BadRequest("eqpName/empNo는 필수입니다.");
        }

        var employee = await context.Employees.FirstOrDefaultAsync(e => e.EmpNo == empNo);
        if (employee is null)
        {
            return BadRequest("존재하지 않는 사원번호입니다.");
        }

        var equipmentExists = await context.Equipments.AnyAsync(e => e.EqpId == eqpName);
        if (!equipmentExists)
        {
            return BadRequest("존재하지 않는 설비입니다.");
        }

        var existing = await context.OpenLabAuths
            .FirstOrDefaultAsync(a => a.Site == site && a.EqpName == eqpName && a.AuthType == authType && a.EmpNo == empNo);

        if (existing is not null)
        {
            return Ok(new OpenLabAuthRow(
                existing.Id,
                existing.Site,
                existing.EqpName,
                existing.AuthType,
                existing.EmpNo,
                employee.UserId,
                employee.HName,
                employee.SingleId,
                employee.DeptCode,
                employee.DeptName));
        }

        var row = new OpenLabAuth
        {
            Site = site,
            EqpName = eqpName,
            AuthType = authType,
            EmpNo = empNo,
        };

        context.OpenLabAuths.Add(row);
        await context.SaveChangesAsync();

        return Ok(new OpenLabAuthRow(
            row.Id,
            row.Site,
            row.EqpName,
            row.AuthType,
            row.EmpNo,
            employee.UserId,
            employee.HName,
            employee.SingleId,
            employee.DeptCode,
            employee.DeptName));
    }

    [HttpDelete("openlab-auth/{id:int}")]
    public async Task<IActionResult> DeleteOpenLabAuth(int id)
    {
        var row = await context.OpenLabAuths.FirstOrDefaultAsync(a => a.Id == id);

        if (row is null)
        {
            return NotFound();
        }

        context.OpenLabAuths.Remove(row);
        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("openlab-receivers")]
    public async Task<ActionResult<IEnumerable<OpenLabReceiverRow>>> GetOpenLabReceivers(
        [FromQuery] string issueNo,
        [FromQuery] string approvalSeq = "0")
    {
        if (string.IsNullOrWhiteSpace(issueNo))
        {
            return BadRequest("issueNo는 필수입니다.");
        }

        var rows = await (
            from n in context.ApprovalNotifications.AsNoTracking()
            join e in context.Employees.AsNoTracking() on n.NotiUserId equals e.UserId
            where n.IssueNo == issueNo.Trim() && n.ApprovalSeq == approvalSeq.Trim()
            orderby n.NotiUserName
            select new OpenLabReceiverRow(
                n.NotiUserId,
                n.NotiUserName,
                n.NotiUserDeptCode,
                n.NotiUserDeptName,
                e.SingleId,
                n.NotiSingleMailAddr))
            .ToListAsync();

        return Ok(rows);
    }

    private async Task<bool> HasReceptionAuthorityAsync(
        string site,
        string eqpName,
        string authType,
        string empNo,
        string? singleId)
    {
        var finalSingleId = (singleId ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(empNo) || string.IsNullOrWhiteSpace(finalSingleId))
        {
            return false;
        }

        return await (
            from a in context.OpenLabAuths.AsNoTracking()
            join s in context.Employees.AsNoTracking() on a.EmpNo equals s.EmpNo
            where a.Site == site
                && a.EqpName == eqpName
                && a.AuthType == authType
                && a.EmpNo == empNo.Trim().ToUpperInvariant()
                && s.SingleId.ToLower() == finalSingleId
            select a.Id
        ).AnyAsync();
    }

    private async Task<List<string>> UpsertIssueReceiversAsync(string issueNo, List<string>? receiverUserIds, string requesterEmpNo)
    {
        var existingRows = await context.ApprovalNotifications
            .Where(n => n.IssueNo == issueNo && n.ApprovalSeq == "0")
            .ToListAsync();

        if (existingRows.Count > 0)
        {
            context.ApprovalNotifications.RemoveRange(existingRows);
        }

        var employees = await context.Employees.AsNoTracking().ToListAsync();
        var employeeByUserId = employees.ToDictionary(e => e.UserId, StringComparer.OrdinalIgnoreCase);

        var receiverSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var receiverId in receiverUserIds ?? [])
        {
            if (!string.IsNullOrWhiteSpace(receiverId))
            {
                receiverSet.Add(receiverId.Trim());
            }
        }

        var requester = employees.FirstOrDefault(e => e.EmpNo.Equals(requesterEmpNo, StringComparison.OrdinalIgnoreCase));
        if (requester is not null)
        {
            receiverSet.Add(requester.UserId);
        }

        var emailSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var receiverId in receiverSet)
        {
            if (!employeeByUserId.TryGetValue(receiverId, out var employee))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(employee.SingleMailAddr))
            {
                emailSet.Add(employee.SingleMailAddr);
            }

            context.ApprovalNotifications.Add(new ApprovalNotification
            {
                IssueNo = issueNo,
                ApprovalSeq = "0",
                ApprovalReq = "1",
                NotiUserId = employee.UserId,
                NotiUserName = employee.HName,
                NotiUserDeptCode = employee.DeptCode,
                NotiUserDeptName = employee.DeptName,
                NotiSingleMailAddr = employee.SingleMailAddr,
                LastUpdateTime = DateTime.UtcNow,
            });
        }

        return emailSet.OrderBy(v => v).ToList();
    }

    private async Task<string> CreateIssueNoAsync()
    {
        while (true)
        {
            var candidate = $"RESV-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Random.Shared.Next(100, 999)}";

            var exists = await context.Reservations.AnyAsync(r => r.IssueNo == candidate);
            if (!exists)
            {
                return candidate;
            }
        }
    }

    private static List<string> SplitFilter(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return [];
        }

        return source
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static DateTime NormalizeDate(DateTime date)
    {
        if (date.Kind == DateTimeKind.Utc)
        {
            return date;
        }

        if (date.Kind == DateTimeKind.Local)
        {
            return date.ToUniversalTime();
        }

        return DateTime.SpecifyKind(date, DateTimeKind.Utc);
    }
}

public record OpenLabLookupResponse(
    IReadOnlyList<string> Lines,
    IReadOnlyList<string> Classes,
    IReadOnlyList<string> Purposes,
    IReadOnlyList<OpenLabEquipmentRow> Equipments,
    IReadOnlyList<OpenLabEmployeeRow> Employees);

public record OpenLabEquipmentRow(
    int Id,
    string EqpId,
    string LineId,
    string LargeClass,
    string EqpType,
    string EqpGroupName,
    int ReservationCount);

public record OpenLabEmployeeRow(
    string UserId,
    string EmpNo,
    string Name,
    string DeptCode,
    string DeptName,
    string SingleId,
    string SingleMailAddr,
    string Site);

public record OpenLabReservationRow(
    int Id,
    string IssueNo,
    int EquipmentId,
    string EqpId,
    string LineId,
    string LargeClass,
    string EmpName,
    string EmpNum,
    DateTime ReservedDate,
    string Purpose,
    string Status,
    DateTime CreatedAt,
    IReadOnlyList<string> ReceiverUserIds);

public class OpenLabReservationUpsertRequest
{
    public int EquipmentId { get; set; }
    public string EmpName { get; set; } = string.Empty;
    public string EmpNum { get; set; } = string.Empty;
    public DateTime ReservedDate { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public string Status { get; set; } = "대기";
    public string Site { get; set; } = "HQ";
    public string AuthType { get; set; } = "RESV";
    public string SingleId { get; set; } = string.Empty;
    public List<string> ReceiverUserIds { get; set; } = [];
}

public record OpenLabAuthRow(
    int Id,
    string Site,
    string EqpName,
    string AuthType,
    string EmpNo,
    string UserId,
    string EmpName,
    string SingleId,
    string DeptCode,
    string DeptName);

public class OpenLabAuthUpsertRequest
{
    public string Site { get; set; } = "HQ";
    public string EqpName { get; set; } = string.Empty;
    public string AuthType { get; set; } = "RESV";
    public string EmpNo { get; set; } = string.Empty;
}

public record OpenLabReceiverRow(
    string UserId,
    string UserName,
    string DeptCode,
    string DeptName,
    string SingleId,
    string SingleMailAddr);
