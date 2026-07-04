namespace ParkHub.Models;

public class ReservationHistoryViewModel
{
    public List<ReservationHistoryItemViewModel> Reservations { get; set; } = new();
    public string UserFullName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public List<UserVehicleSummaryViewModel> Vehicles { get; set; } = new();
    public List<RecentReservationItemViewModel> RecentReservations { get; set; } = new();
}
