namespace YMS.Server.Models;

public class UiSearchHistory
{
    public int Id { get; set; }
    public string AppId { get; set; } = string.Empty;
    public string ControlId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime SearchTime { get; set; }
    public string SearchValue { get; set; } = string.Empty;
}
