namespace ParkHub.Models;

public class ReservationAjaxRequest
{
    public int ParkingSpaceId { get; set; }
    public int VehicleId { get; set; }
    public int DurationHours { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string Expiry { get; set; } = string.Empty;
    public string CVV { get; set; } = string.Empty;
}
