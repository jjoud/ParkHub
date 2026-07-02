namespace ParkHub.Models;

public class UserProfileViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public List<UserVehicleSummaryViewModel> Vehicles { get; set; } = new();
    public List<RecentReservationItemViewModel> RecentReservations { get; set; } = new();
}
