namespace ParkHub.Models;

public class VehicleListViewModel
{
    public List<VehicleItemViewModel> Vehicles { get; set; } = new();

    public VehicleFormViewModel NewVehicle { get; set; } = new();

    public int? ParkingSpaceId { get; set; }

    public string? AreaName { get; set; }

    public string? ReturnUrl { get; set; }
}
