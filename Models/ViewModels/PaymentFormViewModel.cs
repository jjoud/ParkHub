using System.ComponentModel.DataAnnotations;

namespace ParkHub.Models;

public class PaymentFormViewModel
{
    public int ReservationId { get; set; }

    [Required]
    public string PaymentMethod { get; set; } = string.Empty;

    [Required]
    public decimal Amount { get; set; }
}
