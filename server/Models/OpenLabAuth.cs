namespace YMS.Server.Models;

public class OpenLabAuth
{
    public int Id { get; set; }
    public string Site { get; set; } = string.Empty;
    public string EqpName { get; set; } = string.Empty;
    public string AuthType { get; set; } = string.Empty;
    public string EmpNo { get; set; } = string.Empty;

    public Employee? Employee { get; set; }
}
