using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkHub.Data;
using ParkHub.Models;
using ParkHub.Models.ViewModels;
using System.Security.Claims;

namespace ParkHub.Controllers;

[Authorize]
public class ReservationController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReservationController(ApplicationDbContext context)
    {
        _context = context;
    }

    private int? GetCurrentUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (int.TryParse(id, out int userId))
            return userId;

        return null;
    }

    private User? GetCurrentUser()
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return null;

        return _context.Users
            .Include(u => u.Vehicles)
            .FirstOrDefault(u => u.UserId == userId);
    }

    private bool IsAdmin()
    {
        return User.IsInRole("Admin");
    }

    public IActionResult Index()
{
    var user = GetCurrentUser();

    if (user == null)
        return RedirectToAction("Login", "Account");

    var reservations = _context.Reservations
        .Include(r => r.Vehicle)
        .Include(r => r.ParkingSpace)
        .Where(r => r.Vehicle.UserId == user.UserId)
        .OrderByDescending(r => r.ReservationDate)
        .ToList();

    return View(reservations);
}

public IActionResult Details(int id)
{
    var userId = GetCurrentUserId();
    if (userId == null)
        return RedirectToAction("Login", "Account");

    var reservationsQuery = _context.Reservations
        .Include(r => r.Vehicle)
        .Include(r => r.ParkingSpace)
        .Include(r => r.Payment)
        .AsQueryable();

    if (!IsAdmin())
    {
        reservationsQuery = reservationsQuery.Where(r => r.Vehicle.UserId == userId.Value);
    }

    var reservation = reservationsQuery.FirstOrDefault(r => r.ReservationId == id);

    if (reservation == null)
        return NotFound();

    return View(reservation);
}

[HttpGet]
public IActionResult CheckAvailability(int parkingSpaceId)
{
    var space = _context.ParkingSpaces
        .FirstOrDefault(p => p.ParkingSpaceId == parkingSpaceId);

    if (space == null)
    {
        return Json(new
        {
            success = false,
            message = "Parking space not found."
        });
    }

    return Json(new
    {
        success = true,
        available = !space.Status
    });
}

[HttpGet]
public IActionResult CalculatePrice(int hours)
{
    if (hours <= 0)
    {
        return Json(new
        {
            success = false,
            message = "Invalid duration."
        });
    }

    decimal total = hours * 10m;

    return Json(new
    {
        success = true,
        totalPrice = total
    });
}

[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult FinishReservation(int id)
{
    var userId = GetCurrentUserId();
    if (userId == null)
        return RedirectToAction("Login", "Account");

    var reservationsQuery = _context.Reservations
        .Include(r => r.ParkingSpace)
        .Include(r => r.Vehicle)
        .AsQueryable();

    if (!IsAdmin())
    {
        reservationsQuery = reservationsQuery.Where(r => r.Vehicle.UserId == userId.Value);
    }

    var reservation = reservationsQuery.FirstOrDefault(r => r.ReservationId == id);

    if (reservation == null)
    {
        return NotFound();
    }

    reservation.ReservationStatus = false;

    if (reservation.ParkingSpace != null)
    {
        reservation.ParkingSpace.Status = false;
    }

    _context.SaveChanges();

    TempData["SuccessMessage"] = "Reservation completed successfully.";

    return RedirectToAction(nameof(Index));
}

public IActionResult ReservationHistory()
{
    var user = GetCurrentUser();

    if (user == null)
        return RedirectToAction("Login", "Account");

    var reservations = _context.Reservations
        .Include(r => r.Vehicle)
        .Include(r => r.ParkingSpace)
        .Where(r => r.Vehicle.UserId == user.UserId)
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

    return View(new ReservationHistoryViewModel
    {
        Reservations = reservations
    });
}

public IActionResult Payment(int reservationId, decimal amount = 0)
{
    var userId = GetCurrentUserId();
    if (userId == null)
        return RedirectToAction("Login", "Account");

    var reservation = _context.Reservations
        .Include(r => r.Vehicle)
        .Include(r => r.Payment)
        .FirstOrDefault(r => r.ReservationId == reservationId && (IsAdmin() || r.Vehicle.UserId == userId.Value));

    if (reservation == null)
    {
        return NotFound();
    }

    if (reservation.Payment != null)
    {
        TempData["SuccessMessage"] = "Payment already completed for this reservation.";
        return RedirectToAction(nameof(ReservationHistory));
    }

    var model = new PaymentFormViewModel
    {
        ReservationId = reservationId,
        Amount = amount > 0 ? amount : reservation.TotalPrice
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

    var userId = GetCurrentUserId();
    if (userId == null)
        return RedirectToAction("Login", "Account");

    var reservation = _context.Reservations
        .Include(r => r.Vehicle)
        .Include(r => r.Payment)
        .FirstOrDefault(r => r.ReservationId == model.ReservationId && (IsAdmin() || r.Vehicle.UserId == userId.Value));

    if (reservation == null)
    {
        return NotFound();
    }

    if (reservation.Payment != null)
    {
        TempData["SuccessMessage"] = "Payment already completed for this reservation.";
        return RedirectToAction(nameof(ReservationHistory));
    }

    var payment = new Payment
    {
        ReservationId = model.ReservationId,
        Amount = reservation.TotalPrice,
        PaymentDate = DateTime.Now,
        PaymentMethod = model.PaymentMethod,
        PaymentStatus = "Completed"
    };

    _context.Payments.Add(payment);

    reservation.ReservationStatus = true;

    _context.SaveChanges();

    TempData["SuccessMessage"] = "Payment completed successfully.";

    return RedirectToAction(nameof(ReservationHistory));
}
public IActionResult Create(int parkingSpaceId)
{
    var space = _context.ParkingSpaces
        .FirstOrDefault(p => p.ParkingSpaceId == parkingSpaceId);

    if (space == null)
    {
        return NotFound();
    }

    var user = GetCurrentUser();

    if (user == null)
    {
        return RedirectToAction("Login", "Account");
    }

    var model = new ReservationFormViewModel
    {
        ParkingSpaceId = space.ParkingSpaceId,
        AreaName = space.AreaName,
        SpaceNumber = space.SpaceNumber,
        StartTime = DateTime.Now,
        EndTime = DateTime.Now.AddHours(1),

        Vehicles = user.Vehicles.Select(v => new VehicleItemViewModel
        {
            VehicleId = v.VehicleId,
            PlateNumber = v.PlateNumber,
            VehicleType = v.VehicleType,
            Color = v.Color
        }).ToList()
    };

    return View(model);
}

[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult Create(ReservationFormViewModel model)
{
    if (!ModelState.IsValid)
    {
        var user = GetCurrentUser();

        model.Vehicles = user?.Vehicles.Select(v => new VehicleItemViewModel
        {
            VehicleId = v.VehicleId,
            PlateNumber = v.PlateNumber,
            VehicleType = v.VehicleType,
            Color = v.Color
        }).ToList() ?? new List<VehicleItemViewModel>();

        return View(model);
    }

    var parkingSpace = _context.ParkingSpaces
        .FirstOrDefault(p => p.ParkingSpaceId == model.ParkingSpaceId);

    if (parkingSpace == null)
    {
        return NotFound();
    }

    if (parkingSpace.Status)
    {
        ModelState.AddModelError("", "Parking space is already reserved.");

        return View(model);
    }

    if (model.EndTime <= model.StartTime)
    {
        ModelState.AddModelError("", "End time must be after start time.");

        return View(model);
    }

    var currentUser = GetCurrentUser();
    if (currentUser == null)
    {
        return RedirectToAction("Login", "Account");
    }

    if (!currentUser.Vehicles.Any(v => v.VehicleId == model.VehicleId))
    {
        ModelState.AddModelError("VehicleId", "Please select one of your vehicles.");
        model.Vehicles = currentUser.Vehicles.Select(v => new VehicleItemViewModel
        {
            VehicleId = v.VehicleId,
            PlateNumber = v.PlateNumber,
            VehicleType = v.VehicleType,
            Color = v.Color
        }).ToList();

        return View(model);
    }

    int duration =
        (int)Math.Ceiling((model.EndTime - model.StartTime).TotalHours);

    decimal totalPrice = duration * 10;

    var reservation = new Reservation
    {
        VehicleId = model.VehicleId,
        ParkingSpaceId = model.ParkingSpaceId,
        ReservationDate = DateTime.Now,
        StartTime = model.StartTime,
        EndTime = model.EndTime,
        DurationHours = duration,
        TotalPrice = totalPrice,
        ReservationStatus = true
    };

    parkingSpace.Status = true;

    _context.Reservations.Add(reservation);
    _context.SaveChanges();

    return RedirectToAction(nameof(Payment),
        new
        {
            reservationId = reservation.ReservationId,
            amount = reservation.TotalPrice
        });
}
        public IActionResult Edit(int id)
{
    var userId = GetCurrentUserId();
    if (userId == null)
        return RedirectToAction("Login", "Account");

    var reservationsQuery = _context.Reservations
        .Include(r => r.Vehicle)
        .Include(r => r.ParkingSpace)
        .AsQueryable();

    if (!IsAdmin())
    {
        reservationsQuery = reservationsQuery.Where(r => r.Vehicle.UserId == userId.Value);
    }

    var reservation = reservationsQuery.FirstOrDefault(r => r.ReservationId == id);

    if (reservation == null)
    {
        return NotFound();
    }

    var user = GetCurrentUser();

    var model = new ReservationFormViewModel
    {
        ParkingSpaceId = reservation.ParkingSpaceId,
        VehicleId = reservation.VehicleId,
        AreaName = reservation.ParkingSpace.AreaName,
        SpaceNumber = reservation.ParkingSpace.SpaceNumber,
        StartTime = reservation.StartTime,
        EndTime = reservation.EndTime,

        Vehicles = user?.Vehicles.Select(v => new VehicleItemViewModel
        {
            VehicleId = v.VehicleId,
            PlateNumber = v.PlateNumber,
            VehicleType = v.VehicleType,
            Color = v.Color
        }).ToList() ?? new List<VehicleItemViewModel>()
    };

    ViewBag.ReservationId = reservation.ReservationId;

    return View(model);
}
[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult Edit(int id, ReservationFormViewModel model)
{
    if (!ModelState.IsValid)
    {
        var user = GetCurrentUser();

        model.Vehicles = user?.Vehicles.Select(v => new VehicleItemViewModel
        {
            VehicleId = v.VehicleId,
            PlateNumber = v.PlateNumber,
            VehicleType = v.VehicleType,
            Color = v.Color
        }).ToList() ?? new List<VehicleItemViewModel>();

        return View(model);
    }

    var userId = GetCurrentUserId();
    if (userId == null)
        return RedirectToAction("Login", "Account");

    var reservation = _context.Reservations
        .Include(r => r.Vehicle)
        .FirstOrDefault(r => r.ReservationId == id && (IsAdmin() || r.Vehicle.UserId == userId.Value));

    if (reservation == null)
    {
        return NotFound();
    }

    var currentUser = GetCurrentUser();
    if (!IsAdmin() && (currentUser == null || !currentUser.Vehicles.Any(v => v.VehicleId == model.VehicleId)))
    {
        ModelState.AddModelError("VehicleId", "Please select one of your vehicles.");
        model.Vehicles = currentUser?.Vehicles.Select(v => new VehicleItemViewModel
        {
            VehicleId = v.VehicleId,
            PlateNumber = v.PlateNumber,
            VehicleType = v.VehicleType,
            Color = v.Color
        }).ToList() ?? new List<VehicleItemViewModel>();

        return View(model);
    }

    reservation.VehicleId = model.VehicleId;
    reservation.StartTime = model.StartTime;
    reservation.EndTime = model.EndTime;

    reservation.DurationHours =
        (int)Math.Ceiling((model.EndTime - model.StartTime).TotalHours);

    reservation.TotalPrice =
        reservation.DurationHours * 10;

    _context.SaveChanges();

    TempData["SuccessMessage"] = "Reservation updated successfully.";

    return RedirectToAction(nameof(Index));
}


public IActionResult Delete(int id)
{
    var userId = GetCurrentUserId();
    if (userId == null)
        return RedirectToAction("Login", "Account");

    var reservationsQuery = _context.Reservations
        .Include(r => r.Vehicle)
        .Include(r => r.ParkingSpace)
        .AsQueryable();

    if (!IsAdmin())
    {
        reservationsQuery = reservationsQuery.Where(r => r.Vehicle.UserId == userId.Value);
    }

    var reservation = reservationsQuery.FirstOrDefault(r => r.ReservationId == id);

    if (reservation == null)
    {
        return NotFound();
    }

    return View(reservation);
}
[HttpPost, ActionName("Delete")]
[ValidateAntiForgeryToken]
public IActionResult DeleteConfirmed(int id)
{
    var userId = GetCurrentUserId();
    if (userId == null)
        return RedirectToAction("Login", "Account");

    var reservationsQuery = _context.Reservations
        .Include(r => r.ParkingSpace)
        .Include(r => r.Vehicle)
        .AsQueryable();

    if (!IsAdmin())
    {
        reservationsQuery = reservationsQuery.Where(r => r.Vehicle.UserId == userId.Value);
    }

    var reservation = reservationsQuery.FirstOrDefault(r => r.ReservationId == id);

    if (reservation == null)
    {
        return NotFound();
    }

    if (reservation.ParkingSpace != null)
    {
        reservation.ParkingSpace.Status = false;
    }

    _context.Reservations.Remove(reservation);
    _context.SaveChanges();

    TempData["SuccessMessage"] = "Reservation deleted successfully.";

    return RedirectToAction(nameof(Index));
}
}
