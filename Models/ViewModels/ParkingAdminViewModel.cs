using System;
using System.ComponentModel.DataAnnotations;

namespace ParkHub.Models.ViewModels
{
    public class ParkingAdminViewModel
    {
        public int ParkingSpaceId { get; set; }
        [Display(Name = "Area")]
        public string AreaName { get; set; } = string.Empty;
        [Display(Name = "Space Number")]
        public string SpaceNumber { get; set; } = string.Empty;
        [Display(Name = "Occupied / Reserved")]
        public bool Status { get; set; }

        // Reservation info (if any)
        public int? ReservationId { get; set; }
        [Display(Name = "Reserved By")]
        public string? ReservedBy { get; set; }
        [Display(Name = "Start Time")]
        public DateTime? StartTime { get; set; }
        [Display(Name = "End Time")]
        public DateTime? EndTime { get; set; }
        [Display(Name = "Duration (hours)")]
        public int? DurationHours { get; set; }
        [Display(Name = "Total Price")]
        public decimal? TotalPrice { get; set; }

        // admin actions
        public bool ResetReservation { get; set; }
    }
}
