using System.ComponentModel.DataAnnotations;

namespace ParkHub.Models
{
    public class ParkingSpace
    {
        public int ParkingSpaceId { get; set; }

        [Required]
        [StringLength(50)]
        public string AreaName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string SpaceNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string SpaceType { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = string.Empty;

        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
