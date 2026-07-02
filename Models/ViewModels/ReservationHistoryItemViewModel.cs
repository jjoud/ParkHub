namespace ParkHub.Models;

public class ReservationHistoryItemViewModel
{
    public int ReservationId { get; set; }
    public DateTime ReservationDate { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int DurationHours { get; set; }
    public decimal TotalPrice { get; set; }
    public string ReservationStatus { get; set; } = string.Empty;
    public string AreaName { get; set; } = string.Empty;
    public string SpaceNumber { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;
}
