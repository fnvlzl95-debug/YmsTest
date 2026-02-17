namespace YMS.Server.Models;

public class Employee
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string EmpNo { get; set; } = string.Empty;
    public string HName { get; set; } = string.Empty;
    public string EName { get; set; } = string.Empty;
    public string DeptCode { get; set; } = string.Empty;
    public string DeptName { get; set; } = string.Empty;
    public string SingleId { get; set; } = string.Empty;
    public string SingleMailAddr { get; set; } = string.Empty;
    public string Site { get; set; } = "HQ";

    public ICollection<OpenLabAuth> OpenLabAuths { get; set; } = new List<OpenLabAuth>();
    public ICollection<ApprovalNotification> ApprovalNotifications { get; set; } = new List<ApprovalNotification>();
}
