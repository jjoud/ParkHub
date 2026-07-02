namespace ParkHub.Models;

public class ParkingSpaceItemViewModel
{
    public int ParkingSpaceId { get; set; }
    public string AreaName { get; set; } = string.Empty;
    public string SpaceNumber { get; set; } = string.Empty;
    public bool Status { get; set; }
}
