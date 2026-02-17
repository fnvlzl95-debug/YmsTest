namespace YMS.Server.Models;

public class Reservation
{
    public int Id { get; set; }
    public string IssueNo { get; set; } = string.Empty;
    public int EquipmentId { get; set; }
    public string EqpId { get; set; } = string.Empty;
    public string LineId { get; set; } = string.Empty;
    public string LargeClass { get; set; } = string.Empty;
    public string EmpName { get; set; } = string.Empty;
    public string EmpNum { get; set; } = string.Empty;
    public DateTime ReservedDate { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public string Status { get; set; } = "대기";
    public DateTime CreatedAt { get; set; }

    public Equipment? Equipment { get; set; }
}
