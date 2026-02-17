using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YMS.Server.Data;
using YMS.Server.Models;

namespace YMS.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EquipmentsController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Equipment>>> GetEquipments(
        [FromQuery] string? lineId,
        [FromQuery] string? largeClass,
        [FromQuery] string? eqpType)
    {
        var lineIds = SplitFilter(lineId);
        var classes = SplitFilter(largeClass);
        var types = SplitFilter(eqpType);

        var query = context.Equipments.AsNoTracking().AsQueryable();

        if (lineIds.Count > 0)
        {
            query = query.Where(e => lineIds.Contains(e.LineId));
        }

        if (classes.Count > 0)
        {
            query = query.Where(e => classes.Contains(e.LargeClass));
        }

        if (types.Count > 0)
        {
            query = query.Where(e => types.Contains(e.EqpType));
        }

        var result = await query
            .OrderBy(e => e.LineId)
            .ThenBy(e => e.LargeClass)
            .ThenBy(e => e.EqpId)
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("lines")]
    public async Task<ActionResult<IEnumerable<string>>> GetLines()
    {
        var lines = await context.Equipments
            .AsNoTracking()
            .Select(e => e.LineId)
            .Distinct()
            .OrderBy(line => line)
            .ToListAsync();

        return Ok(lines);
    }

    [HttpGet("classes")]
    public async Task<ActionResult<IEnumerable<string>>> GetClasses([FromQuery] string? lineId)
    {
        var lineIds = SplitFilter(lineId);

        var query = context.Equipments.AsNoTracking().AsQueryable();

        if (lineIds.Count > 0)
        {
            query = query.Where(e => lineIds.Contains(e.LineId));
        }

        var classes = await query
            .Select(e => e.LargeClass)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        return Ok(classes);
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
}
