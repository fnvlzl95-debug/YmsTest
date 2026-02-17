using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YMS.Server.Data;

namespace YMS.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AppDbContext context) : ControllerBase
{
    [HttpPost("check-reception")]
    public async Task<ActionResult<CheckReceptionAuthResponse>> CheckReceptionAuth([FromBody] CheckReceptionAuthRequest request)
    {
        var site = (request.Site ?? string.Empty).Trim().ToUpperInvariant();
        var eqpName = (request.EqpName ?? string.Empty).Trim().ToUpperInvariant();
        var authType = string.IsNullOrWhiteSpace(request.AuthType)
            ? "RESV"
            : request.AuthType.Trim().ToUpperInvariant();
        var empNo = (request.EmpNo ?? string.Empty).Trim().ToUpperInvariant();
        var singleId = (request.SingleId ?? string.Empty).Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(site)
            || string.IsNullOrWhiteSpace(eqpName)
            || string.IsNullOrWhiteSpace(empNo)
            || string.IsNullOrWhiteSpace(singleId))
        {
            return BadRequest("권한 조회 파라미터가 누락되었습니다.");
        }

        var isAuthorized = await (
            from a in context.OpenLabAuths.AsNoTracking()
            join s in context.Employees.AsNoTracking() on a.EmpNo equals s.EmpNo
            where a.Site == site
                && a.EqpName == eqpName
                && a.AuthType == authType
                && a.EmpNo == empNo
                && s.SingleId.ToLower() == singleId
            select a.Id
        ).AnyAsync();

        return Ok(new CheckReceptionAuthResponse(isAuthorized));
    }
}

public class CheckReceptionAuthRequest
{
    public string Site { get; set; } = string.Empty;
    public string EqpName { get; set; } = string.Empty;
    public string AuthType { get; set; } = "RESV";
    public string EmpNo { get; set; } = string.Empty;
    public string SingleId { get; set; } = string.Empty;
}

public record CheckReceptionAuthResponse(bool IsAuthorized);
