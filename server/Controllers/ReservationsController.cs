using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YMS.Server.Data;
using YMS.Server.Models;

namespace YMS.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationsController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Reservation>>> GetReservations(
        [FromQuery] string? lineId,
        [FromQuery] string? largeClass,
        [FromQuery] string? tab)
    {
        var lineIds = SplitFilter(lineId);
        var classes = SplitFilter(largeClass);

        var query = context.Reservations.AsNoTracking().AsQueryable();

        if (lineIds.Count > 0)
        {
            query = query.Where(r => lineIds.Contains(r.LineId));
        }

        if (classes.Count > 0)
        {
            query = query.Where(r => classes.Contains(r.LargeClass));
        }

        if (!string.IsNullOrWhiteSpace(tab))
        {
            query = query.Where(r => EF.Functions.Like(r.Purpose, $"%{tab.Trim()}%"));
        }

        var result = await query
            .OrderBy(r => r.ReservedDate)
            .ThenBy(r => r.EqpId)
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Reservation>> GetReservation(int id)
    {
        var reservation = await context.Reservations.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);

        if (reservation is null)
        {
            return NotFound();
        }

        return Ok(reservation);
    }

    [HttpPost]
    public async Task<ActionResult<Reservation>> CreateReservation([FromBody] ReservationUpsertRequest request)
    {
        var equipment = await context.Equipments.FirstOrDefaultAsync(e => e.Id == request.EquipmentId);

        if (equipment is null)
        {
            return BadRequest("유효한 설비를 선택하세요.");
        }

        if (IsHeadQuarter(request.Site))
        {
            var isAuthorized = await HasReceptionAuthorityAsync(
                request.Site,
                equipment.EqpId,
                request.AuthType,
                request.EmpNum,
                request.SingleId);

            if (!isAuthorized)
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
            Status = string.IsNullOrWhiteSpace(request.Status) ? "대기" : request.Status.Trim(),
            CreatedAt = DateTime.UtcNow,
        };

        context.Reservations.Add(reservation);
        await UpsertIssueReceiversAsync(issueNo, request.ReceiverUserIds, request.EmpNum);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetReservation), new { id = reservation.Id }, reservation);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Reservation>> UpdateReservation(int id, [FromBody] ReservationUpsertRequest request)
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

        if (IsHeadQuarter(request.Site))
        {
            var isAuthorized = await HasReceptionAuthorityAsync(
                request.Site,
                equipment.EqpId,
                request.AuthType,
                request.EmpNum,
                request.SingleId);

            if (!isAuthorized)
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
        reservation.Status = string.IsNullOrWhiteSpace(request.Status) ? reservation.Status : request.Status.Trim();

        await UpsertIssueReceiversAsync(reservation.IssueNo, request.ReceiverUserIds, request.EmpNum);
        await context.SaveChangesAsync();

        return Ok(reservation);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteReservation(int id)
    {
        var reservation = await context.Reservations.FirstOrDefaultAsync(r => r.Id == id);

        if (reservation is null)
        {
            return NotFound();
        }

        var notifications = await context.ApprovalNotifications
            .Where(n => n.IssueNo == reservation.IssueNo)
            .ToListAsync();

        if (notifications.Count > 0)
        {
            context.ApprovalNotifications.RemoveRange(notifications);
        }

        context.Reservations.Remove(reservation);
        await context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<bool> HasReceptionAuthorityAsync(
        string site,
        string eqpName,
        string? authType,
        string empNo,
        string? singleId)
    {
        var finalSite = string.IsNullOrWhiteSpace(site) ? "HQ" : site.Trim().ToUpperInvariant();
        var finalEqpName = (eqpName ?? string.Empty).Trim().ToUpperInvariant();
        var finalAuthType = string.IsNullOrWhiteSpace(authType) ? "RESV" : authType.Trim().ToUpperInvariant();
        var finalEmpNo = (empNo ?? string.Empty).Trim().ToUpperInvariant();
        var finalSingleId = (singleId ?? string.Empty).Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(finalEmpNo) || string.IsNullOrWhiteSpace(finalSingleId))
        {
            return false;
        }

        return await (
            from a in context.OpenLabAuths.AsNoTracking()
            join s in context.Employees.AsNoTracking() on a.EmpNo equals s.EmpNo
            where a.Site == finalSite
                && a.EqpName == finalEqpName
                && a.AuthType == finalAuthType
                && a.EmpNo == finalEmpNo
                && s.SingleId.ToLower() == finalSingleId
            select a.Id
        ).AnyAsync();
    }

    private async Task UpsertIssueReceiversAsync(string issueNo, List<string>? receiverUserIds, string requesterEmpNo)
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

        foreach (var receiverId in receiverSet)
        {
            if (!employeeByUserId.TryGetValue(receiverId, out var employee))
            {
                continue;
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

    private static bool IsHeadQuarter(string? site)
    {
        return string.Equals(site?.Trim(), "HQ", StringComparison.OrdinalIgnoreCase);
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

public class ReservationUpsertRequest
{
    public int EquipmentId { get; set; }
    public string EmpName { get; set; } = string.Empty;
    public string EmpNum { get; set; } = string.Empty;
    public DateTime ReservedDate { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string Site { get; set; } = "FAB";
    public string AuthType { get; set; } = "RESV";
    public string? SingleId { get; set; }
    public List<string> ReceiverUserIds { get; set; } = [];
}
