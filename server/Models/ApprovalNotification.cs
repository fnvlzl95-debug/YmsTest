namespace YMS.Server.Models;

public class ApprovalNotification
{
    public int Id { get; set; }
    public string IssueNo { get; set; } = string.Empty;
    public string ApprovalSeq { get; set; } = "0";
    public string ApprovalReq { get; set; } = "1";
    public string NotiUserId { get; set; } = string.Empty;
    public string NotiUserName { get; set; } = string.Empty;
    public string NotiUserDeptCode { get; set; } = string.Empty;
    public string NotiUserDeptName { get; set; } = string.Empty;
    public string NotiSingleMailAddr { get; set; } = string.Empty;
    public DateTime LastUpdateTime { get; set; }

    public Employee? Employee { get; set; }
}
