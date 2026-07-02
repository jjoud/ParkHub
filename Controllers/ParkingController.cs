using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkHub.Data;
using ParkHub.Models;

namespace ParkHub.Controllers;

public class ParkingController : Controller
{
    private readonly ApplicationDbContext _context;

    public ParkingController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index(string? areaName = null)
    {
        var spacesQuery = _context.ParkingSpaces.AsQueryable();

        if (!string.IsNullOrWhiteSpace(areaName))
        {
            spacesQuery = spacesQuery.Where(p => p.AreaName == areaName);
        }

        var spaces = spacesQuery
            .Select(p => new ParkingSpaceItemViewModel
            {
                ParkingSpaceId = p.ParkingSpaceId,
                AreaName = p.AreaName,
                SpaceNumber = p.SpaceNumber,
                Status = p.Status
            })
            .ToList();

        return View(new ParkingIndexViewModel
        {
            SelectedArea = string.IsNullOrWhiteSpace(areaName) ? "All Areas" : areaName,
            ParkingSpaces = spaces
        });
    }

    public IActionResult Reserve(int id)
    {
        var space = _context.ParkingSpaces.Find(id);
        if (space == null)
        {
            return NotFound();
        }

        var user = _context.Users.Include(u => u.Vehicles).FirstOrDefault();
        var vehicleList = user?.Vehicles.Select(v => new VehicleItemViewModel
        {
            VehicleId = v.VehicleId,
            PlateNumber = v.PlateNumber
        }).ToList() ?? new List<VehicleItemViewModel>();

        var model = new ReservationFormViewModel
        {
            ParkingSpaceId = space.ParkingSpaceId,
            AreaName = space.AreaName,
            SpaceNumber = space.SpaceNumber,
            Vehicles = vehicleList,
            StartTime = DateTime.Now,
            EndTime = DateTime.Now.AddHours(1),
            TotalPrice = 0m
        };

        return View(model);
    }

    [HttpPost]
    public IActionResult Reserve(ReservationFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var user = _context.Users.Include(u => u.Vehicles).FirstOrDefault();
            model.Vehicles = user?.Vehicles.Select(v => new VehicleItemViewModel
            {
                VehicleId = v.VehicleId,
                PlateNumber = v.PlateNumber
            }).ToList() ?? new List<VehicleItemViewModel>();
            return View(model);
        }

        if (model.EndTime <= model.StartTime)
        {
            ModelState.AddModelError("EndTime", "EndTime must be later than StartTime.");
            var user = _context.Users.Include(u => u.Vehicles).FirstOrDefault();
            model.Vehicles = user?.Vehicles.Select(v => new VehicleItemViewModel
            {
                VehicleId = v.VehicleId,
                PlateNumber = v.PlateNumber
            }).ToList() ?? new List<VehicleItemViewModel>();
            return View(model);
        }

        var durationHours = (int)Math.Ceiling((model.EndTime - model.StartTime).TotalHours);
        model.TotalPrice = durationHours * 10m;

        var reservation = new Reservation
        {
            VehicleId = model.VehicleId,
            ParkingSpaceId = model.ParkingSpaceId,
            ReservationDate = DateTime.Now,
            StartTime = model.StartTime,
            EndTime = model.EndTime,
            DurationHours = durationHours,
            TotalPrice = model.TotalPrice,
            ReservationStatus = true
        };

        _context.Reservations.Add(reservation);
        _context.SaveChanges();

        return RedirectToAction(nameof(Payment), new { reservationId = reservation.ReservationId, amount = reservation.TotalPrice });
    }

    public IActionResult Payment(int reservationId, decimal amount = 0m)
    {
        var model = new PaymentFormViewModel
        {
            ReservationId = reservationId,
            Amount = amount
        };

        return View(model);
    }

    [HttpPost]
    public IActionResult Payment(PaymentFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var reservation = _context.Reservations.Find(model.ReservationId);
        if (reservation == null)
        {
            return NotFound();
        }

        var payment = new Payment
        {
            ReservationId = model.ReservationId,
            Amount = model.Amount,
            PaymentDate = DateTime.Now,
            PaymentMethod = model.PaymentMethod,
            PaymentStatus = "Completed"
        };

        _context.Payments.Add(payment);
        _context.SaveChanges();

        return RedirectToAction(nameof(ReservationHistory));
    }

    public IActionResult Vehicles()
    {
        var user = _context.Users.Include(u => u.Vehicles).FirstOrDefault();
        var vehicles = user?.Vehicles.Select(v => new VehicleItemViewModel
        {
            VehicleId = v.VehicleId,
            PlateNumber = v.PlateNumber,
            VehicleType = v.VehicleType,
            Color = v.Color
        }).ToList() ?? new List<VehicleItemViewModel>();

        return View(new VehicleListViewModel { Vehicles = vehicles });
    }

    public IActionResult CreateVehicle()
    {
        return View(new VehicleFormViewModel());
    }

    [HttpPost]
    public IActionResult CreateVehicle(VehicleFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = _context.Users.FirstOrDefault();
        if (user == null)
        {
            return RedirectToAction(nameof(Vehicles));
        }

        var vehicle = new Vehicle
        {
            PlateNumber = model.PlateNumber,
            VehicleType = model.VehicleType,
            Color = model.Color,
            UserId = user.UserId
        };

        _context.Vehicles.Add(vehicle);
        _context.SaveChanges();

        return RedirectToAction(nameof(Vehicles));
    }

    public IActionResult EditVehicle(int id)
    {
        var vehicle = _context.Vehicles.Find(id);
        if (vehicle == null)
        {
            return NotFound();
        }

        return View(new VehicleFormViewModel
        {
            VehicleId = vehicle.VehicleId,
            PlateNumber = vehicle.PlateNumber,
            VehicleType = vehicle.VehicleType,
            Color = vehicle.Color
        });
    }

    [HttpPost]
    public IActionResult EditVehicle(VehicleFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var vehicle = _context.Vehicles.Find(model.VehicleId);
        if (vehicle == null)
        {
            return NotFound();
        }

        vehicle.PlateNumber = model.PlateNumber;
        vehicle.VehicleType = model.VehicleType;
        vehicle.Color = model.Color;
        _context.SaveChanges();

        return RedirectToAction(nameof(Vehicles));
    }

    public IActionResult DeleteVehicle(int id)
    {
        var vehicle = _context.Vehicles.Find(id);
        if (vehicle == null)
        {
            return NotFound();
        }

        return View(vehicle);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteVehicleConfirmed(int id)
    {
        var vehicle = _context.Vehicles.Find(id);
        if (vehicle == null)
        {
            return NotFound();
        }

        _context.Vehicles.Remove(vehicle);
        _context.SaveChanges();

        return RedirectToAction(nameof(Vehicles));
    }

    public IActionResult ReservationHistory()
    {
        var user = _context.Users.Include(u => u.Vehicles).FirstOrDefault();
        var reservations = _context.Reservations
            .Include(r => r.ParkingSpace)
            .Include(r => r.Vehicle)
            .Where(r => user != null && r.Vehicle.UserId == user.UserId)
            .OrderByDescending(r => r.ReservationDate)
            .Select(r => new ReservationHistoryItemViewModel
            {
                ReservationId = r.ReservationId,
                ReservationDate = r.ReservationDate,
                DurationHours = r.DurationHours,
                TotalPrice = r.TotalPrice,
                ReservationStatus = r.ReservationStatus ? "Reserved" : "Completed",
                AreaName = r.ParkingSpace.AreaName,
                SpaceNumber = r.ParkingSpace.SpaceNumber,
                PlateNumber = r.Vehicle.PlateNumber
            })
            .ToList();

        return View(new ReservationHistoryViewModel { Reservations = reservations });
    }
}
