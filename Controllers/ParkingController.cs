using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkHub.Data;
using ParkHub.Models;
using ParkHub.Models.ViewModels;

namespace ParkHub.Controllers;
[Authorize]

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

        var user = GetCurrentUser();
        var vehicleList = user?.Vehicles.Select(v => new VehicleItemViewModel
        {
            VehicleId = v.VehicleId,
            PlateNumber = v.PlateNumber,
            VehicleType = v.VehicleType,
            Color = v.Color
        }).ToList() ?? new List<VehicleItemViewModel>();

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
            ParkingSpaces = spaces,
            Vehicles = vehicleList
        });
    }

    public IActionResult Reserve(int id)
    {
        if (id <= 0)
        {
            return RedirectToAction(nameof(Index));
        }

        var space = _context.ParkingSpaces.Find(id);
        if (space == null)
        {
            return RedirectToAction(nameof(Index));
        }

        var user = GetCurrentUser();
        var reservationOwnedByCurrentUser = user != null
            ? _context.Reservations.Include(r => r.Vehicle).FirstOrDefault(r => r.ParkingSpaceId == id && r.ReservationStatus && r.Vehicle.UserId == user.UserId)
            : null;

        if (space.Status && reservationOwnedByCurrentUser == null)
        {
            // this spot is already reserved by someone else
            return RedirectToAction(nameof(Index), new { areaName = space.AreaName });
        }

        if (reservationOwnedByCurrentUser != null)
        {
            return RedirectToAction(nameof(EditReservation), new { id = reservationOwnedByCurrentUser.ReservationId });
        }

        var availableVehicles = user?.Vehicles
            .Where(v => !_context.Reservations.Any(r => r.VehicleId == v.VehicleId && r.ReservationStatus))
            .Select(v => new VehicleItemViewModel
            {
                VehicleId = v.VehicleId,
                PlateNumber = v.PlateNumber
            })
            .ToList() ?? new List<VehicleItemViewModel>();

        if (!availableVehicles.Any())
        {
            TempData["ErrorMessage"] = "You must have an available vehicle before creating a new reservation. Please add or free up a vehicle.";
            return RedirectToAction(nameof(Vehicles));
        }

        var vehicleList = availableVehicles;

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
    public IActionResult Reserve(ReservationFormViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            var user = GetCurrentUser();
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
            var user = GetCurrentUser();
            model.Vehicles = user?.Vehicles.Select(v => new VehicleItemViewModel
            {
                VehicleId = v.VehicleId,
                PlateNumber = v.PlateNumber
            }).ToList() ?? new List<VehicleItemViewModel>();
            return View(model);
        }

        // Ensure the parking space exists and is available
        var space = _context.ParkingSpaces.Find(model.ParkingSpaceId);
        if (space == null)
        {
            ModelState.AddModelError(string.Empty, "Selected parking space does not exist.");
            var user = GetCurrentUser();
            model.Vehicles = user?.Vehicles.Select(v => new VehicleItemViewModel
            {
                VehicleId = v.VehicleId,
                PlateNumber = v.PlateNumber
            }).ToList() ?? new List<VehicleItemViewModel>();
            return View(model);
        }

        if (space.Status)
        {
            ModelState.AddModelError(string.Empty, "This parking space is already reserved.");
            var user = GetCurrentUser();
            model.Vehicles = user?.Vehicles.Select(v => new VehicleItemViewModel
            {
                VehicleId = v.VehicleId,
                PlateNumber = v.PlateNumber
            }).ToList() ?? new List<VehicleItemViewModel>();
            return View(model);
        }

        // Ensure selected vehicle exists and belongs to current user
        var currentUser = GetCurrentUser();
        var vehicle = currentUser == null
            ? null
            : _context.Vehicles.FirstOrDefault(v => v.VehicleId == model.VehicleId && v.UserId == currentUser.UserId);
        if (vehicle == null)
        {
            ModelState.AddModelError("VehicleId", "Please select a valid vehicle.");
            var user = GetCurrentUser();
            model.Vehicles = user?.Vehicles
                .Where(v => !_context.Reservations.Any(r => r.VehicleId == v.VehicleId && r.ReservationStatus))
                .Select(v => new VehicleItemViewModel
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

        // Mark the space as reserved
        space.Status = true;

        _context.Reservations.Add(reservation);
        _context.SaveChanges();

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            TempData["SuccessMessage"] = "Reservation created successfully.";
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Payment), new { reservationId = reservation.ReservationId, amount = reservation.TotalPrice });
    }

    public IActionResult EditReservation(int id)
    {
        if (id <= 0)
        {
            return RedirectToAction(nameof(Index));
        }

        var currentUser = GetCurrentUser();
        if (currentUser == null)
        {
            return RedirectToAction(nameof(Index));
        }

        var reservation = _context.Reservations
            .Include(r => r.Vehicle)
            .Include(r => r.ParkingSpace)
            .FirstOrDefault(r => r.ReservationId == id && r.ReservationStatus && r.Vehicle.UserId == currentUser.UserId);

        if (reservation == null)
        {
            return NotFound();
        }

        var availableVehicles = currentUser.Vehicles
            .Select(v => new VehicleItemViewModel
            {
                VehicleId = v.VehicleId,
                PlateNumber = v.PlateNumber
            })
            .ToList();

        var model = new ReservationFormViewModel
        {
            ReservationId = reservation.ReservationId,
            ParkingSpaceId = reservation.ParkingSpaceId,
            AreaName = reservation.ParkingSpace.AreaName,
            SpaceNumber = reservation.ParkingSpace.SpaceNumber,
            FullName = currentUser.FullName,
            StartTime = reservation.StartTime,
            EndTime = reservation.EndTime,
            VehicleId = reservation.VehicleId,
            TotalPrice = reservation.TotalPrice,
            Vehicles = availableVehicles
        };

        return View("Reserve", model);
    }

    [HttpPost]
    public IActionResult EditReservation(ReservationFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var user = GetCurrentUser();
            model.Vehicles = user?.Vehicles.Select(v => new VehicleItemViewModel
            {
                VehicleId = v.VehicleId,
                PlateNumber = v.PlateNumber
            }).ToList() ?? new List<VehicleItemViewModel>();
            return View("Reserve", model);
        }

        if (model.EndTime <= model.StartTime)
        {
            ModelState.AddModelError("EndTime", "EndTime must be later than StartTime.");
            var user = GetCurrentUser();
            model.Vehicles = user?.Vehicles.Select(v => new VehicleItemViewModel
            {
                VehicleId = v.VehicleId,
                PlateNumber = v.PlateNumber
            }).ToList() ?? new List<VehicleItemViewModel>();
            return View("Reserve", model);
        }

        var currentUser = GetCurrentUser();
        if (currentUser == null)
        {
            return RedirectToAction(nameof(Index));
        }

        var reservation = _context.Reservations
            .Include(r => r.Vehicle)
            .Include(r => r.ParkingSpace)
            .FirstOrDefault(r => r.ReservationId == model.ReservationId && r.Vehicle.UserId == currentUser.UserId && r.ReservationStatus);
        if (reservation == null)
        {
            return NotFound();
        }

        var vehicle = _context.Vehicles.FirstOrDefault(v => v.VehicleId == model.VehicleId && v.UserId == currentUser.UserId);
        if (vehicle == null)
        {
            ModelState.AddModelError("VehicleId", "Please select a valid vehicle.");
            model.Vehicles = currentUser.Vehicles.Select(v => new VehicleItemViewModel
            {
                VehicleId = v.VehicleId,
                PlateNumber = v.PlateNumber
            }).ToList();
            return View("Reserve", model);
        }

        reservation.VehicleId = model.VehicleId;
        reservation.StartTime = model.StartTime;
        reservation.EndTime = model.EndTime;
        reservation.DurationHours = (int)Math.Ceiling((model.EndTime - model.StartTime).TotalHours);
        reservation.TotalPrice = reservation.DurationHours * 10m;
        _context.SaveChanges();

        return RedirectToAction(nameof(ReservationHistory));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ReserveAjax([FromBody] ReservationAjaxRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { success = false, message = "Invalid reservation request." });
        }

        if (request.ParkingSpaceId <= 0 || request.VehicleId <= 0 || request.DurationHours <= 0)
        {
            return BadRequest(new { success = false, message = "Please provide valid reservation details." });
        }

        if (string.IsNullOrWhiteSpace(request.CardNumber) || string.IsNullOrWhiteSpace(request.Expiry) || string.IsNullOrWhiteSpace(request.CVV))
        {
            return BadRequest(new { success = false, message = "Please complete all payment fields." });
        }

        var space = _context.ParkingSpaces.Find(request.ParkingSpaceId);
        if (space == null)
        {
            return NotFound(new { success = false, message = "Parking space not found." });
        }

        if (space.Status)
        {
            return BadRequest(new { success = false, message = "This parking space is already reserved." });
        }

        var user = GetCurrentUser();
        if (user == null || !user.Vehicles.Any(v => v.VehicleId == request.VehicleId))
        {
            return BadRequest(new { success = false, message = "Selected vehicle is not available." });
        }

        var durationHours = request.DurationHours;
        if (durationHours <= 0)
        {
            return BadRequest(new { success = false, message = "Reservation duration must be at least one hour." });
        }

        var reservation = new Reservation
        {
            VehicleId = request.VehicleId,
            ParkingSpaceId = request.ParkingSpaceId,
            ReservationDate = DateTime.Now,
            StartTime = DateTime.Now,
            EndTime = DateTime.Now.AddHours(durationHours),
            DurationHours = durationHours,
            TotalPrice = durationHours * 10m,
            ReservationStatus = true
        };

        space.Status = true;
        _context.Reservations.Add(reservation);
        _context.SaveChanges();

        return Json(new { success = true, message = "Reservation confirmed successfully.", reservationId = reservation.ReservationId, totalPrice = reservation.TotalPrice });
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
        var user = GetCurrentUser();
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

        var user = GetCurrentUser();
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
        var currentUser = GetCurrentUser();
        var vehicle = currentUser == null
            ? null
            : _context.Vehicles.FirstOrDefault(v => v.VehicleId == id && v.UserId == currentUser.UserId);
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

        var currentUser = GetCurrentUser();
        var vehicle = currentUser == null
            ? null
            : _context.Vehicles.FirstOrDefault(v => v.VehicleId == model.VehicleId && v.UserId == currentUser.UserId);
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

    public IActionResult DeleteVehicle(int id, string? returnUrl = null)
    {
        var currentUser = GetCurrentUser();
        var vehicle = currentUser == null
            ? null
            : _context.Vehicles.FirstOrDefault(v => v.VehicleId == id && v.UserId == currentUser.UserId);
        if (vehicle == null)
        {
            return NotFound();
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(vehicle);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteVehicleConfirmed(int id, string? returnUrl = null)
    {
        var currentUser = GetCurrentUser();
        var vehicle = currentUser == null
            ? null
            : _context.Vehicles.FirstOrDefault(v => v.VehicleId == id && v.UserId == currentUser.UserId);
        if (vehicle == null)
        {
            return NotFound();
        }

        _context.Vehicles.Remove(vehicle);
        _context.SaveChanges();

        TempData["SuccessMessage"] = "Vehicle deleted successfully.";

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Vehicles));
    }

    public IActionResult ReservationHistory()
    {
        var user = GetCurrentUser();
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
                IsReserved = r.ReservationStatus,
                AreaName = r.ParkingSpace.AreaName,
                SpaceNumber = r.ParkingSpace.SpaceNumber,
                PlateNumber = r.Vehicle.PlateNumber
            })
            .ToList();

        return View(new ReservationHistoryViewModel { Reservations = reservations });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteReservationConfirmed(int id)
    {
        var currentUser = GetCurrentUser();
        if (currentUser == null)
        {
            return RedirectToAction(nameof(Index));
        }

        var reservation = _context.Reservations
            .Include(r => r.Vehicle)
            .FirstOrDefault(r => r.ReservationId == id && r.Vehicle.UserId == currentUser.UserId);

        if (reservation == null)
        {
            return NotFound();
        }

        if (reservation.ReservationStatus)
        {
            var parkingSpace = _context.ParkingSpaces.Find(reservation.ParkingSpaceId);
            if (parkingSpace != null)
            {
                parkingSpace.Status = false;
            }
        }

        _context.Reservations.Remove(reservation);
        _context.SaveChanges();

        TempData["SuccessMessage"] = "Your reservation has been deleted.";
        return RedirectToAction(nameof(ReservationHistory));
    }

    [Authorize(Roles = "Admin")]
    public IActionResult ManageSpace(int id)
    {
        if (id <= 0)
        {
            return RedirectToAction(nameof(Index));
        }

        var space = _context.ParkingSpaces.Find(id);
        if (space == null)
        {
            return NotFound();
        }

        var reservation = _context.Reservations
            .Include(r => r.Vehicle)
            .FirstOrDefault(r => r.ParkingSpaceId == id && r.ReservationStatus == true);

        var model = new ParkingAdminViewModel
        {
            ParkingSpaceId = space.ParkingSpaceId,
            AreaName = space.AreaName,
            SpaceNumber = space.SpaceNumber,
            Status = space.Status,
            ReservationId = reservation?.ReservationId,
            ReservedBy = reservation?.Vehicle?.PlateNumber,
            StartTime = reservation?.StartTime,
            EndTime = reservation?.EndTime,
            DurationHours = reservation?.DurationHours,
            TotalPrice = reservation?.TotalPrice
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public IActionResult ManageSpace(ParkingAdminViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var space = _context.ParkingSpaces.Find(model.ParkingSpaceId);
        if (space == null)
        {
            return NotFound();
        }

        // Update parking space status
        space.Status = model.Status;

        // If admin requested reset/delete reservation
        if (model.ResetReservation && model.ReservationId.HasValue)
        {
            var reservation = _context.Reservations.Find(model.ReservationId.Value);
            if (reservation != null)
            {
                _context.Reservations.Remove(reservation);
            }
            space.Status = false;
        }
        else if (model.ReservationId.HasValue)
        {
            var reservation = _context.Reservations.Find(model.ReservationId.Value);
            if (reservation != null)
            {
                reservation.StartTime = model.StartTime ?? reservation.StartTime;
                reservation.EndTime = model.EndTime ?? reservation.EndTime;
                if (model.StartTime.HasValue && model.EndTime.HasValue)
                {
                    reservation.DurationHours = (int)Math.Ceiling((model.EndTime.Value - model.StartTime.Value).TotalHours);
                    reservation.TotalPrice = reservation.DurationHours * 10m;
                }
                reservation.ReservationStatus = model.Status;
            }
        }

        _context.SaveChanges();

        return RedirectToAction(nameof(Index), new { areaName = space.AreaName });
    }

    private User? GetCurrentUser()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return _context.Users
            .Include(u => u.Vehicles)
            .FirstOrDefault(u => u.UserId == userId);
    }
}
