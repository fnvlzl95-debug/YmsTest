using Microsoft.AspNetCore.Mvc;
using YMS.Server.Data;
using YMS.Server.Models;

namespace YMS.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UiAuditController(AppDbContext context) : ControllerBase
{
    [HttpPost("search-history")]
    public async Task<IActionResult> SaveSearchHistory([FromBody] SaveSearchHistoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AppId)
            || string.IsNullOrWhiteSpace(request.ControlId)
            || string.IsNullOrWhiteSpace(request.UserId))
        {
            return BadRequest("appId/controlId/userId는 필수입니다.");
        }

        var row = new UiSearchHistory
        {
            AppId = request.AppId.Trim(),
            ControlId = request.ControlId.Trim(),
            UserId = request.UserId.Trim(),
            SearchValue = (request.SearchValue ?? string.Empty).Trim(),
            SearchTime = DateTime.UtcNow,
        };

        context.UiSearchHistories.Add(row);
        await context.SaveChangesAsync();

        return NoContent();
    }
}

public class SaveSearchHistoryRequest
{
    public string AppId { get; set; } = string.Empty;
    public string ControlId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string SearchValue { get; set; } = string.Empty;
}
