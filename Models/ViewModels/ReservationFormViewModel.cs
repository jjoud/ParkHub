using System.ComponentModel.DataAnnotations;

namespace ParkHub.Models;

public class ReservationFormViewModel
{
    public int ParkingSpaceId { get; set; }
    public string AreaName { get; set; } = string.Empty;
    public string SpaceNumber { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    [Required]
    public int VehicleId { get; set; }

    [Required]
    public decimal TotalPrice { get; set; }

    // Optional payment fields collected on the reservation form (not processed here)
    public string CardHolderName { get; set; } = string.Empty;
    public string CardNumber { get; set; } = string.Empty;
    public string CardExpiry { get; set; } = string.Empty;
    public string CardCVV { get; set; } = string.Empty;

    public List<VehicleItemViewModel> Vehicles { get; set; } = new();
}
