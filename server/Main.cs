using System.Data;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace YMS.Server;

[ApiController]
[Route("api/[controller]")]
public class MainController(DataAgent agent, IOpenLabMailer mailer) : ControllerBase
{
    // ──────────────────────────────────────────────
    // GET /api/main/lookups
    // ──────────────────────────────────────────────
    [HttpGet("lookups")]
    public async Task<ActionResult<OpenLabLookupResponse>> GetLookups([FromQuery] string? site)
    {
        var normalizedSite = OpenLabCodes.NormalizeSite(site);

        // 1) Lines
        var sbLine = new StringBuilder();
        sbLine.AppendLine("SELECT DISTINCT \"LineId\"");
        sbLine.AppendLine("  FROM \"DDB_EQUIPMENT_MST\"");
        sbLine.AppendLine(" ORDER BY \"LineId\"");

        var dtLine = await agent.Fill(sbLine.ToString(), []);
        var lines = new List<string>();
        foreach (DataRow row in dtLine.Rows)
        {
            lines.Add(ToStr(row["LineId"]));
        }

        // 2) Classes
        var sbClass = new StringBuilder();
        sbClass.AppendLine("SELECT DISTINCT \"LargeClass\"");
        sbClass.AppendLine("  FROM \"DDB_EQUIPMENT_MST\"");
        sbClass.AppendLine(" ORDER BY \"LargeClass\"");

        var dtClass = await agent.Fill(sbClass.ToString(), []);
        var classes = new List<string>();
        foreach (DataRow row in dtClass.Rows)
        {
            classes.Add(ToStr(row["LargeClass"]));
        }

        // 3) Purposes
        var sbPurpose = new StringBuilder();
        sbPurpose.AppendLine("SELECT DISTINCT \"Purpose\"");
        sbPurpose.AppendLine("  FROM \"DDB_EQUIPMENT_RESV\"");
        sbPurpose.AppendLine(" ORDER BY \"Purpose\"");

        var dtPurpose = await agent.Fill(sbPurpose.ToString(), []);
        var purposes = new List<string>();
        foreach (DataRow row in dtPurpose.Rows)
        {
            purposes.Add(ToStr(row["Purpose"]));
        }

        // 4) Equipments
        var sbEqp = new StringBuilder();
        sbEqp.AppendLine("SELECT E.\"Id\"");
        sbEqp.AppendLine("     , E.\"EqpId\"");
        sbEqp.AppendLine("     , E.\"LineId\"");
        sbEqp.AppendLine("     , E.\"LargeClass\"");
        sbEqp.AppendLine("     , E.\"EqpType\"");
        sbEqp.AppendLine("     , E.\"EqpGroupName\"");
        sbEqp.AppendLine("  FROM \"DDB_EQUIPMENT_MST\" E");
        sbEqp.AppendLine(" ORDER BY E.\"LineId\", E.\"LargeClass\", E.\"EqpId\"");

        var dtEqp = await agent.Fill(sbEqp.ToString(), []);
        var equipments = new List<OpenLabEquipmentRow>();
        foreach (DataRow row in dtEqp.Rows)
        {
            equipments.Add(new OpenLabEquipmentRow(
                ToInt(row["Id"]),
                ToStr(row["EqpId"]),
                ToStr(row["LineId"]),
                ToStr(row["LargeClass"]),
                ToStr(row["EqpType"]),
                ToStr(row["EqpGroupName"]),
                0));
        }

        // 5) Employees
        var sbEmp = new StringBuilder();
        var empParams = new List<DataParameter>();

        sbEmp.AppendLine("SELECT \"UserId\"");
        sbEmp.AppendLine("     , \"EmpNo\"");
        sbEmp.AppendLine("     , \"HName\"");
        sbEmp.AppendLine("     , \"DeptCode\"");
        sbEmp.AppendLine("     , \"DeptName\"");
        sbEmp.AppendLine("     , \"SingleId\"");
        sbEmp.AppendLine("     , \"SingleMailAddr\"");
        sbEmp.AppendLine("     , \"Site\"");
        sbEmp.AppendLine("  FROM \"MST_EMPLOYEE\"");
        sbEmp.AppendLine(" WHERE 1 = 1");

        if (normalizedSite != "ALL")
        {
            sbEmp.AppendLine("   AND \"Site\" = :SITE");
            empParams.Add(new DataParameter("SITE", normalizedSite));
        }

        sbEmp.AppendLine(" ORDER BY \"HName\"");

        var dtEmp = await agent.Fill(sbEmp.ToString(), empParams);
        var employees = new List<OpenLabEmployeeRow>();
        foreach (DataRow row in dtEmp.Rows)
        {
            employees.Add(new OpenLabEmployeeRow(
                ToStr(row["UserId"]),
                ToStr(row["EmpNo"]),
                ToStr(row["HName"]),
                ToStr(row["DeptCode"]),
                ToStr(row["DeptName"]),
                ToStr(row["SingleId"]),
                ToStr(row["SingleMailAddr"]),
                ToStr(row["Site"])));
        }

        return Ok(new OpenLabLookupResponse(lines, classes, purposes, equipments, employees));
    }

    // ──────────────────────────────────────────────
    // GET /api/main/openlab-resv
    // ──────────────────────────────────────────────
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

        var sb = new StringBuilder();
        var parameters = new List<DataParameter>();

        sb.AppendLine("SELECT R.\"Id\"");
        sb.AppendLine("     , R.\"IssueNo\"");
        sb.AppendLine("     , R.\"EquipmentId\"");
        sb.AppendLine("     , R.\"EqpId\"");
        sb.AppendLine("     , R.\"LineId\"");
        sb.AppendLine("     , R.\"LargeClass\"");
        sb.AppendLine("     , R.\"EmpName\"");
        sb.AppendLine("     , R.\"EmpNum\"");
        sb.AppendLine("     , R.\"ReservedDate\"");
        sb.AppendLine("     , R.\"Purpose\"");
        sb.AppendLine("     , R.\"Status\"");
        sb.AppendLine("     , R.\"CreatedAt\"");
        sb.AppendLine("  FROM \"DDB_EQUIPMENT_RESV\" R");
        sb.AppendLine("  LEFT JOIN \"MST_EMPLOYEE\" E ON R.\"EmpNum\" = E.\"EmpNo\"");
        sb.AppendLine(" WHERE 1 = 1");

        if (lineIds.Count > 0)
        {
            var inClause = BuildInClause("R.\"LineId\"", "LINE", lineIds, parameters);
            sb.AppendLine($"   AND {inClause}");
        }

        if (classes.Count > 0)
        {
            var inClause = BuildInClause("R.\"LargeClass\"", "CLASS", classes, parameters);
            sb.AppendLine($"   AND {inClause}");
        }

        if (!string.IsNullOrWhiteSpace(purpose)
            && !string.Equals(purpose, "ALL", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("   AND R.\"Purpose\" LIKE '%' || :PURPOSE || '%'");
            parameters.Add(new DataParameter("PURPOSE", purpose.Trim()));
        }

        if (normalizedSite != "ALL")
        {
            sb.AppendLine("   AND (E.\"Site\" = :SITE OR E.\"Site\" IS NULL)");
            parameters.Add(new DataParameter("SITE", normalizedSite));
        }

        sb.AppendLine(" ORDER BY R.\"ReservedDate\", R.\"EqpId\"");

        var dt = await agent.Fill(sb.ToString(), parameters);
        var rows = new List<OpenLabReservationRow>();

        foreach (DataRow row in dt.Rows)
        {
            rows.Add(new OpenLabReservationRow(
                ToInt(row["Id"]),
                ToStr(row["IssueNo"]),
                ToInt(row["EquipmentId"]),
                ToStr(row["EqpId"]),
                ToStr(row["LineId"]),
                ToStr(row["LargeClass"]),
                ToStr(row["EmpName"]),
                ToStr(row["EmpNum"]),
                ToDate(row["ReservedDate"]),
                ToStr(row["Purpose"]),
                ToStr(row["Status"]),
                ToDate(row["CreatedAt"]),
                Array.Empty<string>()));
        }

        return Ok(rows);
    }

    // ──────────────────────────────────────────────
    // GET /api/main/openlab-resv/{id}
    // ──────────────────────────────────────────────
    [HttpGet("openlab-resv/{id:int}")]
    public async Task<ActionResult<OpenLabReservationRow>> GetOpenLabReservation(int id)
    {
        // 1) Reservation
        var sb = new StringBuilder();
        sb.AppendLine("SELECT \"Id\"");
        sb.AppendLine("     , \"IssueNo\"");
        sb.AppendLine("     , \"EquipmentId\"");
        sb.AppendLine("     , \"EqpId\"");
        sb.AppendLine("     , \"LineId\"");
        sb.AppendLine("     , \"LargeClass\"");
        sb.AppendLine("     , \"EmpName\"");
        sb.AppendLine("     , \"EmpNum\"");
        sb.AppendLine("     , \"ReservedDate\"");
        sb.AppendLine("     , \"Purpose\"");
        sb.AppendLine("     , \"Status\"");
        sb.AppendLine("     , \"CreatedAt\"");
        sb.AppendLine("  FROM \"DDB_EQUIPMENT_RESV\"");
        sb.AppendLine(" WHERE \"Id\" = :ID");

        var dt = await agent.Fill(sb.ToString(), [new DataParameter("ID", id)]);

        if (dt.Rows.Count == 0)
        {
            return NotFound();
        }

        var resv = dt.Rows[0];

        // 2) Receivers
        var receivers = await GetData_Receiver(ToStr(resv["IssueNo"]), "0");

        return Ok(new OpenLabReservationRow(
            ToInt(resv["Id"]),
            ToStr(resv["IssueNo"]),
            ToInt(resv["EquipmentId"]),
            ToStr(resv["EqpId"]),
            ToStr(resv["LineId"]),
            ToStr(resv["LargeClass"]),
            ToStr(resv["EmpName"]),
            ToStr(resv["EmpNum"]),
            ToDate(resv["ReservedDate"]),
            ToStr(resv["Purpose"]),
            ToStr(resv["Status"]),
            ToDate(resv["CreatedAt"]),
            receivers));
    }

    // ──────────────────────────────────────────────
    // POST /api/main/openlab-resv
    // ──────────────────────────────────────────────
    [HttpPost("openlab-resv")]
    public async Task<ActionResult<OpenLabReservationRow>> CreateOpenLabReservation(
        [FromBody] OpenLabReservationUpsertRequest request)
    {
        // 1) Equipment check
        var sbEqp = new StringBuilder();
        sbEqp.AppendLine("SELECT \"Id\"");
        sbEqp.AppendLine("     , \"EqpId\"");
        sbEqp.AppendLine("     , \"LineId\"");
        sbEqp.AppendLine("     , \"LargeClass\"");
        sbEqp.AppendLine("  FROM \"DDB_EQUIPMENT_MST\"");
        sbEqp.AppendLine(" WHERE \"Id\" = :EQP_ID");

        var dtEqp = await agent.Fill(sbEqp.ToString(), [new DataParameter("EQP_ID", request.EquipmentId)]);

        if (dtEqp.Rows.Count == 0)
        {
            return BadRequest("유효한 설비를 선택하세요.");
        }

        var eqpRow = dtEqp.Rows[0];
        var eqpId = ToStr(eqpRow["EqpId"]);
        var lineId = ToStr(eqpRow["LineId"]);
        var largeClass = ToStr(eqpRow["LargeClass"]);

        // 2) Authority check
        var authType = OpenLabCodes.ParseAuthType(request.AuthType).ToCode();
        var siteName = OpenLabCodes.NormalizeSite(request.Site);

        if (siteName == "HQ")
        {
            var hasAuth = await CheckAuth(siteName, eqpId, authType, request.EmpNum, request.SingleId);

            if (!hasAuth)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "접수 권한이 없습니다.");
            }
        }

        // 3) Generate issue number
        var issueNo = await CreateIssueNo();

        // 4) INSERT reservation
        var sbInsert = new StringBuilder();
        sbInsert.AppendLine("INSERT INTO \"DDB_EQUIPMENT_RESV\"");
        sbInsert.AppendLine("  (\"IssueNo\", \"EquipmentId\", \"EqpId\", \"LineId\", \"LargeClass\"");
        sbInsert.AppendLine("  ,\"EmpName\", \"EmpNum\", \"ReservedDate\", \"Purpose\", \"Status\", \"CreatedAt\")");
        sbInsert.AppendLine("VALUES");
        sbInsert.AppendLine("  (:ISSUE_NO, :EQUIPMENT_ID, :EQP_ID, :LINE_ID, :LARGE_CLASS");
        sbInsert.AppendLine("  ,:EMP_NAME, :EMP_NUM, :RESERVED_DATE, :PURPOSE, :STATUS, :CREATED_AT)");

        var createdAt = DateTime.UtcNow;
        var status = string.IsNullOrWhiteSpace(request.Status)
            ? OpenLabCodes.ToDisplay(OpenLabReservationStatus.Waiting)
            : request.Status.Trim();

        var insertParams = new List<DataParameter>
        {
            new DataParameter("ISSUE_NO", issueNo),
            new DataParameter("EQUIPMENT_ID", request.EquipmentId),
            new DataParameter("EQP_ID", eqpId),
            new DataParameter("LINE_ID", lineId),
            new DataParameter("LARGE_CLASS", largeClass),
            new DataParameter("EMP_NAME", request.EmpName.Trim()),
            new DataParameter("EMP_NUM", request.EmpNum.Trim()),
            new DataParameter("RESERVED_DATE", NormalizeDate(request.ReservedDate)),
            new DataParameter("PURPOSE", request.Purpose.Trim()),
            new DataParameter("STATUS", status),
            new DataParameter("CREATED_AT", createdAt),
        };

        await agent.Execute(sbInsert.ToString(), insertParams);

        // 5) Get generated Id
        var sbId = new StringBuilder();
        sbId.AppendLine("SELECT \"Id\" FROM \"DDB_EQUIPMENT_RESV\" WHERE \"IssueNo\" = :ISSUE_NO");
        var newId = Convert.ToInt32(await agent.ExecuteScalar(sbId.ToString(), [new DataParameter("ISSUE_NO", issueNo)]));

        // 6) Upsert receivers
        var receiverEmails = await SetDataRequestNoti(issueNo, request.ReceiverUserIds, request.EmpNum);

        // 7) Send mail
        await mailer.SendReservationEventAsync(new OpenLabMailMessage(
            issueNo,
            "CREATE",
            $"[OPENLAB 예약등록] {eqpId}",
            receiverEmails,
            $"{request.EmpName.Trim()}님이 {NormalizeDate(request.ReservedDate):yyyy-MM-dd HH:mm}에 {eqpId} 예약을 등록했습니다."));

        return CreatedAtAction(nameof(GetOpenLabReservation), new { id = newId }, new OpenLabReservationRow(
            newId,
            issueNo,
            request.EquipmentId,
            eqpId,
            lineId,
            largeClass,
            request.EmpName.Trim(),
            request.EmpNum.Trim(),
            NormalizeDate(request.ReservedDate),
            request.Purpose.Trim(),
            status,
            createdAt,
            request.ReceiverUserIds));
    }

    // ──────────────────────────────────────────────
    // PUT /api/main/openlab-resv/{id}
    // ──────────────────────────────────────────────
    [HttpPut("openlab-resv/{id:int}")]
    public async Task<ActionResult<OpenLabReservationRow>> UpdateOpenLabReservation(
        int id,
        [FromBody] OpenLabReservationUpsertRequest request)
    {
        // 1) Reservation check
        var sbResv = new StringBuilder();
        sbResv.AppendLine("SELECT \"Id\", \"IssueNo\", \"Status\", \"CreatedAt\"");
        sbResv.AppendLine("  FROM \"DDB_EQUIPMENT_RESV\"");
        sbResv.AppendLine(" WHERE \"Id\" = :ID");

        var dtResv = await agent.Fill(sbResv.ToString(), [new DataParameter("ID", id)]);

        if (dtResv.Rows.Count == 0)
        {
            return NotFound();
        }

        var resvRow = dtResv.Rows[0];
        var issueNo = ToStr(resvRow["IssueNo"]);
        var existingCreatedAt = ToDate(resvRow["CreatedAt"]);

        // 2) Equipment check
        var sbEqp = new StringBuilder();
        sbEqp.AppendLine("SELECT \"Id\", \"EqpId\", \"LineId\", \"LargeClass\"");
        sbEqp.AppendLine("  FROM \"DDB_EQUIPMENT_MST\"");
        sbEqp.AppendLine(" WHERE \"Id\" = :EQP_ID");

        var dtEqp = await agent.Fill(sbEqp.ToString(), [new DataParameter("EQP_ID", request.EquipmentId)]);

        if (dtEqp.Rows.Count == 0)
        {
            return BadRequest("유효한 설비를 선택하세요.");
        }

        var eqpRow = dtEqp.Rows[0];
        var eqpId = ToStr(eqpRow["EqpId"]);
        var lineId = ToStr(eqpRow["LineId"]);
        var largeClass = ToStr(eqpRow["LargeClass"]);

        // 3) Authority check
        var authType = OpenLabCodes.ParseAuthType(request.AuthType).ToCode();
        var siteName = OpenLabCodes.NormalizeSite(request.Site);

        if (siteName == "HQ")
        {
            var hasAuth = await CheckAuth(siteName, eqpId, authType, request.EmpNum, request.SingleId);

            if (!hasAuth)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "접수 권한이 없습니다.");
            }
        }

        // 4) UPDATE reservation
        var status = string.IsNullOrWhiteSpace(request.Status)
            ? ToStr(resvRow["Status"])
            : request.Status.Trim();

        var sbUpdate = new StringBuilder();
        sbUpdate.AppendLine("UPDATE \"DDB_EQUIPMENT_RESV\"");
        sbUpdate.AppendLine("   SET \"EquipmentId\"  = :EQUIPMENT_ID");
        sbUpdate.AppendLine("     , \"EqpId\"        = :EQP_ID");
        sbUpdate.AppendLine("     , \"LineId\"        = :LINE_ID");
        sbUpdate.AppendLine("     , \"LargeClass\"    = :LARGE_CLASS");
        sbUpdate.AppendLine("     , \"EmpName\"       = :EMP_NAME");
        sbUpdate.AppendLine("     , \"EmpNum\"        = :EMP_NUM");
        sbUpdate.AppendLine("     , \"ReservedDate\"  = :RESERVED_DATE");
        sbUpdate.AppendLine("     , \"Purpose\"       = :PURPOSE");
        sbUpdate.AppendLine("     , \"Status\"        = :STATUS");
        sbUpdate.AppendLine(" WHERE \"Id\" = :ID");

        var updateParams = new List<DataParameter>
        {
            new DataParameter("EQUIPMENT_ID", request.EquipmentId),
            new DataParameter("EQP_ID", eqpId),
            new DataParameter("LINE_ID", lineId),
            new DataParameter("LARGE_CLASS", largeClass),
            new DataParameter("EMP_NAME", request.EmpName.Trim()),
            new DataParameter("EMP_NUM", request.EmpNum.Trim()),
            new DataParameter("RESERVED_DATE", NormalizeDate(request.ReservedDate)),
            new DataParameter("PURPOSE", request.Purpose.Trim()),
            new DataParameter("STATUS", status),
            new DataParameter("ID", id),
        };

        await agent.Execute(sbUpdate.ToString(), updateParams);

        // 5) Upsert receivers
        var receiverEmails = await SetDataRequestNoti(issueNo, request.ReceiverUserIds, request.EmpNum);

        // 6) Send mail
        await mailer.SendReservationEventAsync(new OpenLabMailMessage(
            issueNo,
            "UPDATE",
            $"[OPENLAB 예약변경] {eqpId}",
            receiverEmails,
            $"{request.EmpName.Trim()}님 예약이 변경되었습니다. ({NormalizeDate(request.ReservedDate):yyyy-MM-dd HH:mm})"));

        return Ok(new OpenLabReservationRow(
            id,
            issueNo,
            request.EquipmentId,
            eqpId,
            lineId,
            largeClass,
            request.EmpName.Trim(),
            request.EmpNum.Trim(),
            NormalizeDate(request.ReservedDate),
            request.Purpose.Trim(),
            status,
            existingCreatedAt,
            request.ReceiverUserIds));
    }

    // ──────────────────────────────────────────────
    // DELETE /api/main/openlab-resv/{id}
    // ──────────────────────────────────────────────
    [HttpDelete("openlab-resv/{id:int}")]
    public async Task<IActionResult> DeleteOpenLabReservation(int id)
    {
        // 1) Reservation check
        var sbResv = new StringBuilder();
        sbResv.AppendLine("SELECT \"Id\", \"IssueNo\", \"EqpId\", \"EmpName\"");
        sbResv.AppendLine("  FROM \"DDB_EQUIPMENT_RESV\"");
        sbResv.AppendLine(" WHERE \"Id\" = :ID");

        var dtResv = await agent.Fill(sbResv.ToString(), [new DataParameter("ID", id)]);

        if (dtResv.Rows.Count == 0)
        {
            return NotFound();
        }

        var resvRow = dtResv.Rows[0];
        var issueNo = ToStr(resvRow["IssueNo"]);
        var eqpId = ToStr(resvRow["EqpId"]);
        var empName = ToStr(resvRow["EmpName"]);

        // 2) Get notification emails before delete
        var sbNoti = new StringBuilder();
        sbNoti.AppendLine("SELECT DISTINCT \"NotiSingleMailAddr\"");
        sbNoti.AppendLine("  FROM \"DDB_APPROVAL_NOTI\"");
        sbNoti.AppendLine(" WHERE \"IssueNo\" = :ISSUE_NO");

        var dtNoti = await agent.Fill(sbNoti.ToString(), [new DataParameter("ISSUE_NO", issueNo)]);
        var mailTargets = new List<string>();
        foreach (DataRow row in dtNoti.Rows)
        {
            var addr = ToStr(row["NotiSingleMailAddr"]);
            if (!string.IsNullOrWhiteSpace(addr))
            {
                mailTargets.Add(addr);
            }
        }

        // 3) Delete notifications
        var sbDeleteNoti = new StringBuilder();
        sbDeleteNoti.AppendLine("DELETE FROM \"DDB_APPROVAL_NOTI\"");
        sbDeleteNoti.AppendLine(" WHERE \"IssueNo\" = :ISSUE_NO");

        await agent.Execute(sbDeleteNoti.ToString(), [new DataParameter("ISSUE_NO", issueNo)]);

        // 4) Delete reservation
        var sbDelete = new StringBuilder();
        sbDelete.AppendLine("DELETE FROM \"DDB_EQUIPMENT_RESV\"");
        sbDelete.AppendLine(" WHERE \"Id\" = :ID");

        await agent.Execute(sbDelete.ToString(), [new DataParameter("ID", id)]);

        // 5) Send mail
        await mailer.SendReservationEventAsync(new OpenLabMailMessage(
            issueNo,
            "DELETE",
            $"[OPENLAB 예약취소] {eqpId}",
            mailTargets,
            $"{empName}님의 예약이 취소되었습니다."));

        return NoContent();
    }

    // ──────────────────────────────────────────────
    // GET /api/main/openlab-eqp
    // ──────────────────────────────────────────────
    [HttpGet("openlab-eqp")]
    public async Task<ActionResult<IEnumerable<OpenLabEquipmentRow>>> GetOpenLabEquipments(
        [FromQuery] string? lineId,
        [FromQuery] string? largeClass)
    {
        var lineIds = SplitFilter(lineId);
        var classes = SplitFilter(largeClass);

        var sb = new StringBuilder();
        var parameters = new List<DataParameter>();

        sb.AppendLine("SELECT E.\"Id\"");
        sb.AppendLine("     , E.\"EqpId\"");
        sb.AppendLine("     , E.\"LineId\"");
        sb.AppendLine("     , E.\"LargeClass\"");
        sb.AppendLine("     , E.\"EqpType\"");
        sb.AppendLine("     , E.\"EqpGroupName\"");
        sb.AppendLine("     , COUNT(R.\"Id\") AS \"ReservationCount\"");
        sb.AppendLine("  FROM \"DDB_EQUIPMENT_MST\" E");
        sb.AppendLine("  LEFT JOIN \"DDB_EQUIPMENT_RESV\" R ON E.\"EqpId\" = R.\"EqpId\"");
        sb.AppendLine(" WHERE 1 = 1");

        if (lineIds.Count > 0)
        {
            var inClause = BuildInClause("E.\"LineId\"", "LINE", lineIds, parameters);
            sb.AppendLine($"   AND {inClause}");
        }

        if (classes.Count > 0)
        {
            var inClause = BuildInClause("E.\"LargeClass\"", "CLASS", classes, parameters);
            sb.AppendLine($"   AND {inClause}");
        }

        sb.AppendLine(" GROUP BY E.\"Id\", E.\"EqpId\", E.\"LineId\", E.\"LargeClass\", E.\"EqpType\", E.\"EqpGroupName\"");
        sb.AppendLine(" ORDER BY E.\"LineId\", E.\"LargeClass\", E.\"EqpId\"");

        var dt = await agent.Fill(sb.ToString(), parameters);
        var rows = new List<OpenLabEquipmentRow>();

        foreach (DataRow row in dt.Rows)
        {
            rows.Add(new OpenLabEquipmentRow(
                ToInt(row["Id"]),
                ToStr(row["EqpId"]),
                ToStr(row["LineId"]),
                ToStr(row["LargeClass"]),
                ToStr(row["EqpType"]),
                ToStr(row["EqpGroupName"]),
                ToInt(row["ReservationCount"])));
        }

        return Ok(rows);
    }

    // ──────────────────────────────────────────────
    // GET /api/main/openlab-auth
    // ──────────────────────────────────────────────
    [HttpGet("openlab-auth")]
    public async Task<ActionResult<IEnumerable<OpenLabAuthRow>>> GetOpenLabAuths(
        [FromQuery] string? site,
        [FromQuery] string? eqpName,
        [FromQuery] string? authType)
    {
        var normalizedSite = OpenLabCodes.NormalizeSite(site);
        var normalizedEqpName = (eqpName ?? string.Empty).Trim().ToUpperInvariant();
        var normalizedAuthType = (authType ?? string.Empty).Trim().ToUpperInvariant();

        var sb = new StringBuilder();
        var parameters = new List<DataParameter>();

        sb.AppendLine("SELECT A.\"Id\"");
        sb.AppendLine("     , A.\"Site\"");
        sb.AppendLine("     , A.\"EqpName\"");
        sb.AppendLine("     , A.\"AuthType\"");
        sb.AppendLine("     , A.\"EmpNo\"");
        sb.AppendLine("     , S.\"UserId\"");
        sb.AppendLine("     , S.\"HName\"");
        sb.AppendLine("     , S.\"SingleId\"");
        sb.AppendLine("     , S.\"DeptCode\"");
        sb.AppendLine("     , S.\"DeptName\"");
        sb.AppendLine("  FROM \"DDB_OPENLAB_AUTH\" A");
        sb.AppendLine(" INNER JOIN \"MST_EMPLOYEE\" S ON A.\"EmpNo\" = S.\"EmpNo\"");
        sb.AppendLine(" WHERE 1 = 1");

        if (!string.IsNullOrWhiteSpace(normalizedSite) && normalizedSite != "ALL")
        {
            sb.AppendLine("   AND A.\"Site\" = :SITE");
            parameters.Add(new DataParameter("SITE", normalizedSite));
        }

        if (!string.IsNullOrWhiteSpace(normalizedEqpName))
        {
            sb.AppendLine("   AND A.\"EqpName\" = :EQP_NAME");
            parameters.Add(new DataParameter("EQP_NAME", normalizedEqpName));
        }

        if (!string.IsNullOrWhiteSpace(normalizedAuthType))
        {
            sb.AppendLine("   AND A.\"AuthType\" = :AUTH_TYPE");
            parameters.Add(new DataParameter("AUTH_TYPE", normalizedAuthType));
        }

        sb.AppendLine(" ORDER BY A.\"Site\", A.\"EqpName\", A.\"AuthType\", S.\"HName\"");

        var dt = await agent.Fill(sb.ToString(), parameters);
        var rows = new List<OpenLabAuthRow>();

        foreach (DataRow row in dt.Rows)
        {
            rows.Add(new OpenLabAuthRow(
                ToInt(row["Id"]),
                ToStr(row["Site"]),
                ToStr(row["EqpName"]),
                ToStr(row["AuthType"]),
                ToStr(row["EmpNo"]),
                ToStr(row["UserId"]),
                ToStr(row["HName"]),
                ToStr(row["SingleId"]),
                ToStr(row["DeptCode"]),
                ToStr(row["DeptName"])));
        }

        return Ok(rows);
    }

    // ──────────────────────────────────────────────
    // POST /api/main/openlab-auth
    // ──────────────────────────────────────────────
    [HttpPost("openlab-auth")]
    public async Task<ActionResult<OpenLabAuthRow>> CreateOpenLabAuth(
        [FromBody] OpenLabAuthUpsertRequest request)
    {
        var site = OpenLabCodes.NormalizeSite(request.Site);
        var eqpName = (request.EqpName ?? string.Empty).Trim().ToUpperInvariant();
        var authTypeCode = OpenLabCodes.ParseAuthType(request.AuthType).ToCode();
        var empNo = (request.EmpNo ?? string.Empty).Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(eqpName) || string.IsNullOrWhiteSpace(empNo))
        {
            return BadRequest("eqpName/empNo는 필수입니다.");
        }

        // 1) Employee check
        var sbEmp = new StringBuilder();
        sbEmp.AppendLine("SELECT \"UserId\", \"EmpNo\", \"HName\", \"SingleId\", \"DeptCode\", \"DeptName\"");
        sbEmp.AppendLine("  FROM \"MST_EMPLOYEE\"");
        sbEmp.AppendLine(" WHERE \"EmpNo\" = :EMP_NO");

        var dtEmp = await agent.Fill(sbEmp.ToString(), [new DataParameter("EMP_NO", empNo)]);

        if (dtEmp.Rows.Count == 0)
        {
            return BadRequest("존재하지 않는 사원번호입니다.");
        }

        var empRow = dtEmp.Rows[0];

        // 2) Equipment check
        var sbEqp = new StringBuilder();
        sbEqp.AppendLine("SELECT COUNT(*) AS \"CNT\" FROM \"DDB_EQUIPMENT_MST\" WHERE \"EqpId\" = :EQP_ID");

        var eqpCount = Convert.ToInt32(await agent.ExecuteScalar(sbEqp.ToString(), [new DataParameter("EQP_ID", eqpName)]));

        if (eqpCount == 0)
        {
            return BadRequest("존재하지 않는 설비입니다.");
        }

        // 3) Duplicate check
        var sbDup = new StringBuilder();
        sbDup.AppendLine("SELECT \"Id\"");
        sbDup.AppendLine("  FROM \"DDB_OPENLAB_AUTH\"");
        sbDup.AppendLine(" WHERE \"Site\"     = :SITE");
        sbDup.AppendLine("   AND \"EqpName\"  = :EQP_NAME");
        sbDup.AppendLine("   AND \"AuthType\" = :AUTH_TYPE");
        sbDup.AppendLine("   AND \"EmpNo\"    = :EMP_NO");

        var dupParams = new List<DataParameter>
        {
            new DataParameter("SITE", site),
            new DataParameter("EQP_NAME", eqpName),
            new DataParameter("AUTH_TYPE", authTypeCode),
            new DataParameter("EMP_NO", empNo),
        };

        var dtDup = await agent.Fill(sbDup.ToString(), dupParams);

        if (dtDup.Rows.Count > 0)
        {
            return Ok(new OpenLabAuthRow(
                ToInt(dtDup.Rows[0]["Id"]),
                site,
                eqpName,
                authTypeCode,
                empNo,
                ToStr(empRow["UserId"]),
                ToStr(empRow["HName"]),
                ToStr(empRow["SingleId"]),
                ToStr(empRow["DeptCode"]),
                ToStr(empRow["DeptName"])));
        }

        // 4) INSERT
        var sbInsert = new StringBuilder();
        sbInsert.AppendLine("INSERT INTO \"DDB_OPENLAB_AUTH\"");
        sbInsert.AppendLine("  (\"Site\", \"EqpName\", \"AuthType\", \"EmpNo\")");
        sbInsert.AppendLine("VALUES");
        sbInsert.AppendLine("  (:SITE, :EQP_NAME, :AUTH_TYPE, :EMP_NO)");

        var insertParams = new List<DataParameter>
        {
            new DataParameter("SITE", site),
            new DataParameter("EQP_NAME", eqpName),
            new DataParameter("AUTH_TYPE", authTypeCode),
            new DataParameter("EMP_NO", empNo),
        };

        await agent.Execute(sbInsert.ToString(), insertParams);

        // 5) Get generated Id
        var sbId = new StringBuilder();
        sbId.AppendLine("SELECT \"Id\" FROM \"DDB_OPENLAB_AUTH\"");
        sbId.AppendLine(" WHERE \"Site\"     = :SITE");
        sbId.AppendLine("   AND \"EqpName\"  = :EQP_NAME");
        sbId.AppendLine("   AND \"AuthType\" = :AUTH_TYPE");
        sbId.AppendLine("   AND \"EmpNo\"    = :EMP_NO");

        var idParams = new List<DataParameter>
        {
            new DataParameter("SITE", site),
            new DataParameter("EQP_NAME", eqpName),
            new DataParameter("AUTH_TYPE", authTypeCode),
            new DataParameter("EMP_NO", empNo),
        };

        var newId = Convert.ToInt32(await agent.ExecuteScalar(sbId.ToString(), idParams));

        return Ok(new OpenLabAuthRow(
            newId,
            site,
            eqpName,
            authTypeCode,
            empNo,
            ToStr(empRow["UserId"]),
            ToStr(empRow["HName"]),
            ToStr(empRow["SingleId"]),
            ToStr(empRow["DeptCode"]),
            ToStr(empRow["DeptName"])));
    }

    // ──────────────────────────────────────────────
    // DELETE /api/main/openlab-auth/{id}
    // ──────────────────────────────────────────────
    [HttpDelete("openlab-auth/{id:int}")]
    public async Task<IActionResult> DeleteOpenLabAuth(int id)
    {
        // 1) Check exists
        var sbCheck = new StringBuilder();
        sbCheck.AppendLine("SELECT COUNT(*) AS \"CNT\" FROM \"DDB_OPENLAB_AUTH\" WHERE \"Id\" = :ID");

        var count = Convert.ToInt32(await agent.ExecuteScalar(sbCheck.ToString(), [new DataParameter("ID", id)]));

        if (count == 0)
        {
            return NotFound();
        }

        // 2) DELETE
        var sb = new StringBuilder();
        sb.AppendLine("DELETE FROM \"DDB_OPENLAB_AUTH\"");
        sb.AppendLine(" WHERE \"Id\" = :ID");

        await agent.Execute(sb.ToString(), [new DataParameter("ID", id)]);

        return NoContent();
    }

    // ──────────────────────────────────────────────
    // GET /api/main/openlab-receivers
    // ──────────────────────────────────────────────
    [HttpGet("openlab-receivers")]
    public async Task<ActionResult<IEnumerable<OpenLabReceiverRow>>> GetOpenLabReceivers(
        [FromQuery] string issueNo,
        [FromQuery] string approvalSeq = "0")
    {
        if (string.IsNullOrWhiteSpace(issueNo))
        {
            return BadRequest("issueNo는 필수입니다.");
        }

        var sb = new StringBuilder();
        var parameters = new List<DataParameter>();

        sb.AppendLine("SELECT N.\"NotiUserId\"");
        sb.AppendLine("     , N.\"NotiUserName\"");
        sb.AppendLine("     , N.\"NotiUserDeptCode\"");
        sb.AppendLine("     , N.\"NotiUserDeptName\"");
        sb.AppendLine("     , E.\"SingleId\"");
        sb.AppendLine("     , N.\"NotiSingleMailAddr\"");
        sb.AppendLine("  FROM \"DDB_APPROVAL_NOTI\" N");
        sb.AppendLine(" INNER JOIN \"MST_EMPLOYEE\" E ON N.\"NotiUserId\" = E.\"UserId\"");
        sb.AppendLine(" WHERE N.\"IssueNo\"     = :ISSUE_NO");
        sb.AppendLine("   AND N.\"ApprovalSeq\" = :APPROVAL_SEQ");
        sb.AppendLine(" ORDER BY N.\"NotiUserName\"");

        parameters.Add(new DataParameter("ISSUE_NO", issueNo.Trim()));
        parameters.Add(new DataParameter("APPROVAL_SEQ", approvalSeq.Trim()));

        var dt = await agent.Fill(sb.ToString(), parameters);
        var rows = new List<OpenLabReceiverRow>();

        foreach (DataRow row in dt.Rows)
        {
            rows.Add(new OpenLabReceiverRow(
                ToStr(row["NotiUserId"]),
                ToStr(row["NotiUserName"]),
                ToStr(row["NotiUserDeptCode"]),
                ToStr(row["NotiUserDeptName"]),
                ToStr(row["SingleId"]),
                ToStr(row["NotiSingleMailAddr"])));
        }

        return Ok(rows);
    }

    // ──────────────────────────────────────────────
    // Private: CheckAuth  (접수 권한 확인)
    // ──────────────────────────────────────────────
    private async Task<bool> CheckAuth(
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

        var sb = new StringBuilder();
        var parameters = new List<DataParameter>();

        sb.AppendLine("SELECT COUNT(*) AS \"CNT\"");
        sb.AppendLine("  FROM \"DDB_OPENLAB_AUTH\" A");
        sb.AppendLine(" INNER JOIN \"MST_EMPLOYEE\" S ON A.\"EmpNo\" = S.\"EmpNo\"");
        sb.AppendLine(" WHERE A.\"Site\"     = :SITE");
        sb.AppendLine("   AND A.\"EqpName\"  = :EQP_NAME");
        sb.AppendLine("   AND A.\"AuthType\" = :AUTH_TYPE");
        sb.AppendLine("   AND A.\"EmpNo\"    = :EMP_NO");
        sb.AppendLine("   AND LOWER(S.\"SingleId\") = :SINGLE_ID");

        parameters.Add(new DataParameter("SITE", site));
        parameters.Add(new DataParameter("EQP_NAME", eqpName));
        parameters.Add(new DataParameter("AUTH_TYPE", authType));
        parameters.Add(new DataParameter("EMP_NO", empNo.Trim().ToUpperInvariant()));
        parameters.Add(new DataParameter("SINGLE_ID", finalSingleId));

        var result = await agent.ExecuteScalar(sb.ToString(), parameters);
        var count = Convert.ToInt32(result);

        return count > 0;
    }

    // ──────────────────────────────────────────────
    // Private: GetData_Receiver  (수신자 목록)
    // ──────────────────────────────────────────────
    private async Task<List<string>> GetData_Receiver(string issueNo, string approvalSeq)
    {
        var sb = new StringBuilder();
        var parameters = new List<DataParameter>();

        sb.AppendLine("SELECT N.\"NotiUserId\"");
        sb.AppendLine("  FROM \"DDB_APPROVAL_NOTI\" N");
        sb.AppendLine(" WHERE N.\"IssueNo\"     = :ISSUE_NO");
        sb.AppendLine("   AND N.\"ApprovalSeq\" = :APPROVAL_SEQ");
        sb.AppendLine(" ORDER BY N.\"NotiUserId\"");

        parameters.Add(new DataParameter("ISSUE_NO", issueNo));
        parameters.Add(new DataParameter("APPROVAL_SEQ", approvalSeq));

        var dt = await agent.Fill(sb.ToString(), parameters);
        var receivers = new List<string>();

        foreach (DataRow row in dt.Rows)
        {
            receivers.Add(ToStr(row["NotiUserId"]));
        }

        return receivers;
    }

    // ──────────────────────────────────────────────
    // Private: SetDataRequestNoti  (수신자 등록/갱신)
    // ──────────────────────────────────────────────
    private async Task<List<string>> SetDataRequestNoti(
        string issueNo,
        List<string>? receiverUserIds,
        string requesterEmpNo)
    {
        // 1) Delete existing notifications
        var sbDelete = new StringBuilder();
        sbDelete.AppendLine("DELETE FROM \"DDB_APPROVAL_NOTI\"");
        sbDelete.AppendLine(" WHERE \"IssueNo\"     = :ISSUE_NO");
        sbDelete.AppendLine("   AND \"ApprovalSeq\" = '0'");

        await agent.Execute(sbDelete.ToString(), [new DataParameter("ISSUE_NO", issueNo)]);

        // 2) Get all employees
        var sbEmp = new StringBuilder();
        sbEmp.AppendLine("SELECT \"UserId\", \"EmpNo\", \"HName\"");
        sbEmp.AppendLine("     , \"DeptCode\", \"DeptName\", \"SingleMailAddr\"");
        sbEmp.AppendLine("  FROM \"MST_EMPLOYEE\"");

        var dtEmp = await agent.Fill(sbEmp.ToString(), []);
        var employeeByUserId = new Dictionary<string, DataRow>(StringComparer.OrdinalIgnoreCase);

        foreach (DataRow row in dtEmp.Rows)
        {
            var userId = ToStr(row["UserId"]);

            if (!string.IsNullOrWhiteSpace(userId))
            {
                employeeByUserId[userId] = row;
            }
        }

        // 3) Build receiver set
        var receiverSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var receiverId in receiverUserIds ?? [])
        {
            if (!string.IsNullOrWhiteSpace(receiverId))
            {
                receiverSet.Add(receiverId.Trim());
            }
        }

        // Add requester
        foreach (DataRow row in dtEmp.Rows)
        {
            if (string.Equals(ToStr(row["EmpNo"]), requesterEmpNo, StringComparison.OrdinalIgnoreCase))
            {
                receiverSet.Add(ToStr(row["UserId"]));
                break;
            }
        }

        // 4) Insert each receiver
        var emailSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var receiverId in receiverSet)
        {
            if (!employeeByUserId.TryGetValue(receiverId, out var empRow))
            {
                continue;
            }

            var mailAddr = ToStr(empRow["SingleMailAddr"]);

            if (!string.IsNullOrWhiteSpace(mailAddr))
            {
                emailSet.Add(mailAddr);
            }

            var sbInsert = new StringBuilder();
            sbInsert.AppendLine("INSERT INTO \"DDB_APPROVAL_NOTI\"");
            sbInsert.AppendLine("  (\"IssueNo\", \"ApprovalSeq\", \"ApprovalReq\"");
            sbInsert.AppendLine("  ,\"NotiUserId\", \"NotiUserName\", \"NotiUserDeptCode\"");
            sbInsert.AppendLine("  ,\"NotiUserDeptName\", \"NotiSingleMailAddr\", \"LastUpdateTime\")");
            sbInsert.AppendLine("VALUES");
            sbInsert.AppendLine("  (:ISSUE_NO, '0', '1'");
            sbInsert.AppendLine("  ,:NOTI_USER_ID, :NOTI_USER_NAME, :NOTI_DEPT_CODE");
            sbInsert.AppendLine("  ,:NOTI_DEPT_NAME, :NOTI_MAIL_ADDR, :LAST_UPDATE_TIME)");

            var insertParams = new List<DataParameter>
            {
                new DataParameter("ISSUE_NO", issueNo),
                new DataParameter("NOTI_USER_ID", ToStr(empRow["UserId"])),
                new DataParameter("NOTI_USER_NAME", ToStr(empRow["HName"])),
                new DataParameter("NOTI_DEPT_CODE", ToStr(empRow["DeptCode"])),
                new DataParameter("NOTI_DEPT_NAME", ToStr(empRow["DeptName"])),
                new DataParameter("NOTI_MAIL_ADDR", mailAddr),
                new DataParameter("LAST_UPDATE_TIME", DateTime.UtcNow),
            };

            await agent.Execute(sbInsert.ToString(), insertParams);
        }

        return emailSet.OrderBy(v => v).ToList();
    }

    // ──────────────────────────────────────────────
    // Private: CreateIssueNo  (고유 이슈번호 생성)
    // ──────────────────────────────────────────────
    private async Task<string> CreateIssueNo()
    {
        while (true)
        {
            var candidate = $"RESV-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Random.Shared.Next(100, 999)}";

            var sb = new StringBuilder();
            sb.AppendLine("SELECT COUNT(*) AS \"CNT\"");
            sb.AppendLine("  FROM \"DDB_EQUIPMENT_RESV\"");
            sb.AppendLine(" WHERE \"IssueNo\" = :ISSUE_NO");

            var result = await agent.ExecuteScalar(sb.ToString(), [new DataParameter("ISSUE_NO", candidate)]);
            var count = Convert.ToInt32(result);

            if (count == 0)
            {
                return candidate;
            }
        }
    }

    // ──────────────────────────────────────────────
    // Utility Methods
    // ──────────────────────────────────────────────
    private static string BuildInClause(
        string columnName,
        string paramPrefix,
        List<string> values,
        List<DataParameter> parameters)
    {
        var paramNames = new List<string>();

        for (var i = 0; i < values.Count; i++)
        {
            var paramName = $"{paramPrefix}_{i}";
            paramNames.Add($":{paramName}");
            parameters.Add(new DataParameter(paramName, values[i]));
        }

        return $"{columnName} IN ({string.Join(", ", paramNames)})";
    }

    private static int ToInt(object? value)
    {
        if (value is null || value is DBNull) return 0;
        return Convert.ToInt32(value);
    }

    private static string ToStr(object? value)
    {
        if (value is null || value is DBNull) return string.Empty;
        return value.ToString() ?? string.Empty;
    }

    private static DateTime ToDate(object? value)
    {
        if (value is DateTime dt) return dt;
        if (value is null || value is DBNull) return DateTime.MinValue;
        return DateTime.TryParse(value.ToString(), out var parsed) ? parsed : DateTime.MinValue;
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
        if (date.Kind == DateTimeKind.Utc) return date;
        if (date.Kind == DateTimeKind.Local) return date.ToUniversalTime();
        return DateTime.SpecifyKind(date, DateTimeKind.Utc);
    }
}

// ──────────────────────────────────────────────
// DTO Records
// ──────────────────────────────────────────────

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
