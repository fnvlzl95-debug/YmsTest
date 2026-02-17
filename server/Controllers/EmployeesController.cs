using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YMS.Server.Data;

namespace YMS.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeLookupDto>>> GetEmployees([FromQuery] string? site)
    {
        var query = context.Employees.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(site))
        {
            var normalizedSite = site.Trim().ToUpperInvariant();
            query = query.Where(e => e.Site == normalizedSite);
        }

        var rows = await query
            .OrderBy(e => e.HName)
            .Select(e => new EmployeeLookupDto(
                e.UserId,
                e.EmpNo,
                e.HName,
                e.DeptCode,
                e.DeptName,
                e.SingleId,
                e.SingleMailAddr,
                e.Site))
            .ToListAsync();

        return Ok(rows);
    }

    [HttpGet("admins")]
    public async Task<ActionResult<IEnumerable<AdminCandidateDto>>> GetAdminCandidates([FromQuery] string? site)
    {
        var normalizedSite = string.IsNullOrWhiteSpace(site) ? null : site.Trim().ToUpperInvariant();

        var query =
            from a in context.OpenLabAuths.AsNoTracking()
            join e in context.Employees.AsNoTracking() on a.EmpNo equals e.EmpNo
            where a.AuthType == "ADMIN"
            select new { a.Site, e.UserId, e.EmpNo, e.HName, e.SingleId, e.DeptCode, e.DeptName };

        if (!string.IsNullOrWhiteSpace(normalizedSite))
        {
            query = query.Where(x => x.Site == normalizedSite);
        }

        var rows = await query
            .OrderBy(x => x.HName)
            .ToListAsync();

        var result = rows
            .GroupBy(x => x.EmpNo)
            .Select(g =>
            {
                var row = g.First();
                return new AdminCandidateDto(
                    row.EmpNo,
                    row.SingleId,
                    row.HName,
                    row.UserId,
                    row.DeptCode,
                    row.DeptName);
            })
            .ToList();

        return Ok(result);
    }
}

public record EmployeeLookupDto(
    string UserId,
    string EmpNo,
    string Name,
    string DeptCode,
    string DeptName,
    string SingleId,
    string SingleMailAddr,
    string Site);

public record AdminCandidateDto(
    string InputKey,
    string InputValue,
    string Name,
    string UserId,
    string DeptCode,
    string DeptName);
