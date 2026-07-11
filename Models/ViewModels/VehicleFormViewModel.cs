using System.ComponentModel.DataAnnotations;

namespace ParkHub.Models;

public class VehicleFormViewModel
{
    public int VehicleId { get; set; }

    [Required]
    [StringLength(20)]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*[0-9])[A-Za-z0-9 ]+$", ErrorMessage = "Plate Number must contain both English letters and numbers.")]
    public string PlateNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string VehicleType { get; set; } = string.Empty;

    [StringLength(30)]
    public string Color { get; set; } = string.Empty;
}
