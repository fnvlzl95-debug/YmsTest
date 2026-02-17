namespace YMS.Server.Models;

public class Equipment
{
    public int Id { get; set; }
    public string LineId { get; set; } = string.Empty;
    public string LargeClass { get; set; } = string.Empty;
    public string EqpType { get; set; } = string.Empty;
    public string EqpId { get; set; } = string.Empty;
    public string EqpGroupName { get; set; } = string.Empty;

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
