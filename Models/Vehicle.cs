using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParkHub.Models
{

   public class Vehicle
{
    public int VehicleId { get; set; }

    [Required]
    [StringLength(20)]
    public string PlateNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string VehicleType { get; set; } = string.Empty;

    [StringLength(30)]
    public string Color { get; set; } = string.Empty;

    // Foreign Key
    [Required]
    public int UserId { get; set; }

    // Navigation Property
    public User User { get; set; } = null!;

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}

}


    
