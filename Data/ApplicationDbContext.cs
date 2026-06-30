using Microsoft.EntityFrameworkCore;
using ParkHub.Models;

namespace ParkHub.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    public DbSet<Vehicle> Vehicles { get; set; }

    public DbSet<ParkingSpace> ParkingSpaces { get; set; }

    public DbSet<Reservation> Reservations { get; set; }

    public DbSet<Payment> Payments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasMany(user => user.Vehicles)
            .WithOne(vehicle => vehicle.User)
            .HasForeignKey(vehicle => vehicle.UserId);

        modelBuilder.Entity<Vehicle>()
            .HasMany(vehicle => vehicle.Reservations)
            .WithOne(reservation => reservation.Vehicle)
            .HasForeignKey(reservation => reservation.VehicleId);

        modelBuilder.Entity<Reservation>()
            .HasOne(reservation => reservation.Payment)
            .WithOne(payment => payment.Reservation)
            .HasForeignKey<Payment>(payment => payment.ReservationId);

        modelBuilder.Entity<Reservation>()
            .Property(reservation => reservation.TotalPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Payment>()
            .Property(payment => payment.Amount)
            .HasPrecision(18, 2);
    }
}
