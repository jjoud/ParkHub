namespace ParkHub.Models;

public class ParkingIndexViewModel
{
    public string SelectedArea { get; set; } = "All Areas";
    public List<ParkingSpaceItemViewModel> ParkingSpaces { get; set; } = new();
}
