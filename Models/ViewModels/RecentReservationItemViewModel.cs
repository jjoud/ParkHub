namespace ParkHub.Models;

public class RecentReservationItemViewModel
{
    public int ReservationId { get; set; }
    public DateTime ReservationDate { get; set; }
    public decimal TotalPrice { get; set; }
    public string ReservationStatus { get; set; } = string.Empty;
    public string AreaName { get; set; } = string.Empty;
    public string SpaceNumber { get; set; } = string.Empty;
}
