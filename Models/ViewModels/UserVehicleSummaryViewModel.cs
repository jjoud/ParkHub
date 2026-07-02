namespace ParkHub.Models;

public class UserVehicleSummaryViewModel
{
    public int VehicleId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}
