using System.ComponentModel.DataAnnotations;

namespace ParkHub.Models
{
    public class Reservation
    {
        [Key]
        public int ReservationId { get; set; }

        // Foreign Keys
        [Required]
        public int VehicleId { get; set; }

        [Required]
        public int ParkingSpaceId { get; set; }

        // Reservation Information
        [Required]
        public DateTime ReservationDate { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        [Required]
        [Range(1, 24)]
        public int DurationHours { get; set; }

        [Required]
        [Range(0.01, 100000)]
        public decimal TotalPrice { get; set; }

        [Required]
        [StringLength(20)]
        public string ReservationStatus { get; set; } = string.Empty;

        // Navigation Properties
        public Vehicle Vehicle { get; set; } = null!;

        public ParkingSpace ParkingSpace { get; set; } = null!;

        public Payment? Payment { get; set; }
    }
}
