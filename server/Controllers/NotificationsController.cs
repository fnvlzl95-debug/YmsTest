using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YMS.Server.Data;
using YMS.Server.Models;

namespace YMS.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController(AppDbContext context) : ControllerBase
{
    [HttpGet("receivers")]
    public async Task<ActionResult<IEnumerable<ReceiverDto>>> GetReceivers(
        [FromQuery] string issueNo,
        [FromQuery] string approvalSeq = "0")
    {
        if (string.IsNullOrWhiteSpace(issueNo))
        {
            return BadRequest("issueNo는 필수입니다.");
        }

        var receiverRows = await (
            from m in context.Employees.AsNoTracking()
            join d in context.ApprovalNotifications.AsNoTracking() on m.UserId equals d.NotiUserId
            where d.IssueNo == issueNo.Trim()
                && d.ApprovalSeq == approvalSeq.Trim()
            select new ReceiverDto(
                m.UserId,
                m.HName,
                m.DeptCode,
                m.DeptName,
                m.SingleId,
                m.EName)
        )
            .OrderBy(x => x.UserName)
            .ToListAsync();

        var receivers = receiverRows
            .GroupBy(x => x.UserId)
            .Select(g => g.First())
            .ToList();

        return Ok(receivers);
    }

    [HttpPost("request")]
    public async Task<ActionResult<RequestNotificationResponse>> SetRequestNotification([FromBody] RequestNotificationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.IssueNo))
        {
            return BadRequest("issueNo는 필수입니다.");
        }

        var issueNo = request.IssueNo.Trim();
        var reqAnalType = string.IsNullOrWhiteSpace(request.ReqAnalType) ? "-" : request.ReqAnalType.Trim();
        var site = string.IsNullOrWhiteSpace(request.Site) ? "HQ" : request.Site.Trim().ToUpperInvariant();

        var templateIssueNo = site == "HQ" ? $"NOTICE-{reqAnalType}" : "NOTICE--";

        var templateRows = await context.ApprovalNotifications
            .AsNoTracking()
            .Where(n => n.IssueNo == templateIssueNo && n.ApprovalSeq == "0")
            .ToListAsync();

        var existingRows = await context.ApprovalNotifications
            .Where(n => n.IssueNo == issueNo)
            .ToListAsync();

        var existingUserIds = existingRows.Select(x => x.NotiUserId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var employees = await context.Employees.AsNoTracking().ToListAsync();

        var rowsToInsert = new List<ApprovalNotification>();

        foreach (var template in templateRows)
        {
            if (!string.IsNullOrWhiteSpace(request.AppUserId)
                && template.NotiUserId.Equals(request.AppUserId.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!existingUserIds.Add(template.NotiUserId))
            {
                continue;
            }

            rowsToInsert.Add(new ApprovalNotification
            {
                IssueNo = issueNo,
                ApprovalSeq = "0",
                ApprovalReq = "1",
                NotiUserId = template.NotiUserId,
                NotiUserName = template.NotiUserName,
                NotiUserDeptCode = template.NotiUserDeptCode,
                NotiUserDeptName = template.NotiUserDeptName,
                NotiSingleMailAddr = template.NotiSingleMailAddr,
                LastUpdateTime = DateTime.UtcNow,
            });
        }

        if (!string.IsNullOrWhiteSpace(request.ReqUserId))
        {
            var requesterKey = request.ReqUserId.Trim();

            var requester = employees.FirstOrDefault(e =>
                e.UserId.Equals(requesterKey, StringComparison.OrdinalIgnoreCase)
                || e.EmpNo.Equals(requesterKey, StringComparison.OrdinalIgnoreCase));

            if (requester is not null && existingUserIds.Add(requester.UserId))
            {
                rowsToInsert.Add(new ApprovalNotification
                {
                    IssueNo = issueNo,
                    ApprovalSeq = "0",
                    ApprovalReq = "1",
                    NotiUserId = requester.UserId,
                    NotiUserName = requester.HName,
                    NotiUserDeptCode = requester.DeptCode,
                    NotiUserDeptName = requester.DeptName,
                    NotiSingleMailAddr = requester.SingleMailAddr,
                    LastUpdateTime = DateTime.UtcNow,
                });
            }
        }

        if (rowsToInsert.Count > 0)
        {
            await context.ApprovalNotifications.AddRangeAsync(rowsToInsert);
            await context.SaveChangesAsync();
        }

        return Ok(new RequestNotificationResponse(rowsToInsert.Count));
    }
}

public class RequestNotificationRequest
{
    public string IssueNo { get; set; } = string.Empty;
    public string ReqAnalType { get; set; } = string.Empty;
    public string ReqUserId { get; set; } = string.Empty;
    public string AppUserId { get; set; } = string.Empty;
    public string Site { get; set; } = "HQ";
}

public record RequestNotificationResponse(int InsertedCount);

public record ReceiverDto(
    string UserId,
    string UserName,
    string DeptCode,
    string DeptName,
    string SingleId,
    string EName);
