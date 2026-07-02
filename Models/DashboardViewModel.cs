using System.Collections.Generic;

namespace ParkHub.Models;

public class DashboardViewModel
{
    public int TotalParkingSpaces { get; set; }
    public int AvailableParkingSpaces { get; set; }
    public int ReservedParkingSpaces { get; set; }
    public int TotalUsers { get; set; }
    public int TotalReservations { get; set; }
    public decimal TotalRevenue { get; set; }
    public double OccupancyRate { get; set; }

    public List<ParkingSpace> ParkingSpaces { get; set; } = new();
    public List<Reservation> Reservations { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
}
