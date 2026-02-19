using System.Data;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace YMS.Server.Controllers;

/// <summary>
/// ★ 실무 핵심 패턴: DataInfo Dispatch Controller
///
/// 프론트의 DataController.execute(dataInfo) 호출을 받아서
/// className + methodName 조합으로 적절한 서버 메서드를 호출한다.
///
/// 예: { className: "Controls", methodName: "GetEmployeeList" }
///   → Controls.GetEmployeeList() 호출
///
/// 실무에서 공용 Input 컴포넌트가 내부적으로 이 경로를 타고
/// 서버 메서드를 호출한다. 개발자가 보기에는 Input에 dataSource를
/// 넘겼는데 실제로는 이 컨트롤러를 통해 다른 메서드가 호출되는 것.
/// </summary>
[ApiController]
[Route("api/datainfo")]
public class DataInfoController(DataAgent agent) : ControllerBase
{
    [HttpPost("execute")]
    public async Task<ActionResult<DataInfoResponse>> Execute([FromBody] DataInfoRequest request)
    {
        try
        {
            var result = request.ClassName?.ToUpper() switch
            {
                "CONTROLS" => await DispatchControls(request),
                "MAIN" => await DispatchMain(request),
                _ => throw new ArgumentException($"Unknown className: {request.ClassName}")
            };

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Controls 클래스 dispatch.
    /// ★ type="employee" Input이 내부에서 Controls.GetEmployeeList를 강제 호출할 때 여기로 온다.
    /// </summary>
    private async Task<DataInfoResponse> DispatchControls(DataInfoRequest request)
    {
        return request.MethodName?.ToUpper() switch
        {
            "GETEMPLOYEELIST" => await GetEmployeeList(request.Params),
            _ => throw new ArgumentException($"Unknown Controls method: {request.MethodName}")
        };
    }

    /// <summary>
    /// Main 클래스 dispatch.
    /// type="select" Input의 dataSource={Main.GetAdmin} 같은 경우에 여기로 온다.
    /// </summary>
    private async Task<DataInfoResponse> DispatchMain(DataInfoRequest request)
    {
        return request.MethodName?.ToUpper() switch
        {
            "GETADMIN" => await GetAdminList(request.Params),
            "GETEQUIPMENTLIST" => await GetEquipmentList(request.Params),
            _ => throw new ArgumentException($"Unknown Main method: {request.MethodName}")
        };
    }

    // ──────────────────────────────────────────────
    // Controls.GetEmployeeList
    // ★ type="employee" Input이 dataSource를 무시하고 이 메서드를 강제 호출
    // ──────────────────────────────────────────────
    private async Task<DataInfoResponse> GetEmployeeList(Dictionary<string, object>? paramDict)
    {
        var sb = new StringBuilder();
        sb.AppendLine("SELECT E.\"EmpNo\"   AS EMP_NO,");
        sb.AppendLine("       E.\"Name\"    AS USER_NAME,");
        sb.AppendLine("       E.\"UserId\"  AS SINGLE_ID");
        sb.AppendLine("  FROM \"MST_EMPLOYEE\" E");

        var parameters = new List<DataParameter>();

        // userNames 파라미터가 있으면 이름으로 LIKE 검색
        if (paramDict != null && paramDict.TryGetValue("userNames", out var userNamesObj))
        {
            var searchName = ExtractFirstString(userNamesObj);

            if (!string.IsNullOrWhiteSpace(searchName))
            {
                sb.AppendLine(" WHERE E.\"Name\" LIKE :searchName");
                parameters.Add(new DataParameter("searchName", $"%{searchName}%"));
            }
        }

        sb.AppendLine(" ORDER BY E.\"Name\"");

        var dt = await agent.Fill(sb.ToString(), parameters);

        return ToResponse(dt);
    }

    // ──────────────────────────────────────────────
    // Main.GetAdmin — 관리자 목록 (select dataSource용)
    // INPUT_KEY / INPUT_NAME 형태로 반환
    // ──────────────────────────────────────────────
    private async Task<DataInfoResponse> GetAdminList(Dictionary<string, object>? paramDict)
    {
        var sb = new StringBuilder();
        sb.AppendLine("SELECT E.\"EmpNo\"  AS INPUT_KEY,");
        sb.AppendLine("       E.\"Name\"   AS INPUT_NAME");
        sb.AppendLine("  FROM \"MST_EMPLOYEE\" E");
        sb.AppendLine(" ORDER BY E.\"Name\"");

        var dt = await agent.Fill(sb.ToString(), []);

        return ToResponse(dt);
    }

    // ──────────────────────────────────────────────
    // Main.GetEquipmentList — 설비 목록 (select dataSource용)
    // ──────────────────────────────────────────────
    private async Task<DataInfoResponse> GetEquipmentList(Dictionary<string, object>? paramDict)
    {
        var sb = new StringBuilder();
        sb.AppendLine("SELECT E.\"EqpId\"        AS INPUT_KEY,");
        sb.AppendLine("       E.\"EqpId\" || ' (' || E.\"EqpGroupName\" || ')' AS INPUT_NAME");
        sb.AppendLine("  FROM \"DDB_EQUIPMENT_MST\" E");

        var parameters = new List<DataParameter>();

        if (paramDict != null && paramDict.TryGetValue("lineId", out var lineIdObj))
        {
            var lineId = lineIdObj?.ToString();

            if (!string.IsNullOrWhiteSpace(lineId))
            {
                sb.AppendLine(" WHERE E.\"LineId\" = :lineId");
                parameters.Add(new DataParameter("lineId", lineId));
            }
        }

        sb.AppendLine(" ORDER BY E.\"EqpId\"");

        var dt = await agent.Fill(sb.ToString(), parameters);

        return ToResponse(dt);
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────
    private static DataInfoResponse ToResponse(DataTable dt)
    {
        var columns = new List<string>();

        foreach (DataColumn col in dt.Columns)
        {
            columns.Add(col.ColumnName);
        }

        var rows = new List<Dictionary<string, object?>>();

        foreach (DataRow row in dt.Rows)
        {
            var dict = new Dictionary<string, object?>();

            foreach (DataColumn col in dt.Columns)
            {
                dict[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
            }

            rows.Add(dict);
        }

        return new DataInfoResponse { Columns = columns, Rows = rows };
    }

    private static string ExtractFirstString(object? value)
    {
        if (value is System.Text.Json.JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array && jsonElement.GetArrayLength() > 0)
            {
                return jsonElement[0].GetString() ?? string.Empty;
            }

            if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                return jsonElement.GetString() ?? string.Empty;
            }
        }

        if (value is string s)
        {
            return s;
        }

        return value?.ToString() ?? string.Empty;
    }
}

// ──────────────────────────────────────────────
// Request / Response DTOs
// ──────────────────────────────────────────────
public class DataInfoRequest
{
    public string? ClassName { get; set; }
    public string? MethodName { get; set; }
    public Dictionary<string, object>? Params { get; set; }
    public string? Category { get; set; }
}

public class DataInfoResponse
{
    public List<string> Columns { get; set; } = [];
    public List<Dictionary<string, object?>> Rows { get; set; } = [];
}
