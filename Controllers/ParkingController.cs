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

    public IActionResult Index(string? areaName = null, string? searchTerm = null)
    {
        var spacesQuery = _context.ParkingSpaces.AsQueryable();

        if (!string.IsNullOrWhiteSpace(areaName))
        {
            spacesQuery = spacesQuery.Where(p => p.AreaName == areaName);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            spacesQuery = spacesQuery.Where(p =>
                p.AreaName.Contains(searchTerm) ||
                p.SpaceNumber.Contains(searchTerm) ||
                p.SpaceType.Contains(searchTerm));
        }

        var user = _context.Users.Include(u => u.Vehicles).FirstOrDefault();
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

        ViewData["SearchTerm"] = searchTerm;

        return View(new ParkingIndexViewModel
        {
            SelectedArea = string.IsNullOrWhiteSpace(areaName) ? "All Areas" : areaName,
            ParkingSpaces = spaces,
            Vehicles = vehicleList
        });
    }

    public IActionResult Details(int id)
    {
        var space = _context.ParkingSpaces
            .FirstOrDefault(p => p.ParkingSpaceId == id);

        if (space == null)
        {
            return NotFound();
        }

        return View(space);
    }

    public IActionResult Create()
    {
        return View(new ParkingSpace());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(ParkingSpace parkingSpace)
    {
        if (!ModelState.IsValid)
        {
            return View(parkingSpace);
        }

        _context.ParkingSpaces.Add(parkingSpace);
        _context.SaveChanges();

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int id)
    {
        var space = _context.ParkingSpaces.Find(id);
        if (space == null)
        {
            return NotFound();
        }

        return View(space);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, ParkingSpace parkingSpace)
    {
        if (id != parkingSpace.ParkingSpaceId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(parkingSpace);
        }

        var space = _context.ParkingSpaces.Find(id);
        if (space == null)
        {
            return NotFound();
        }

        space.AreaName = parkingSpace.AreaName;
        space.SpaceNumber = parkingSpace.SpaceNumber;
        space.SpaceType = parkingSpace.SpaceType;
        space.Status = parkingSpace.Status;
        _context.SaveChanges();

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Delete(int id)
    {
        var space = _context.ParkingSpaces.Find(id);
        if (space == null)
        {
            return NotFound();
        }

        return View(space);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        var space = _context.ParkingSpaces.Find(id);
        if (space == null)
        {
            return NotFound();
        }

        _context.ParkingSpaces.Remove(space);
        _context.SaveChanges();

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Available()
    {
        return FilteredParkingSpaces("Available Parking Spaces", _context.ParkingSpaces.Where(p => !p.Status));
    }

    public IActionResult Reserved()
    {
        return FilteredParkingSpaces("Reserved Parking Spaces", _context.ParkingSpaces.Where(p => p.Status));
    }

    public IActionResult VIP()
    {
        return FilteredParkingSpaces("VIP Parking Spaces", _context.ParkingSpaces.Where(p => p.SpaceType == "VIP"));
    }

    public IActionResult Search(string? searchTerm)
    {
        return RedirectToAction(nameof(Index), new { searchTerm });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ChangeStatus(int id, bool status)
    {
        var space = _context.ParkingSpaces.Find(id);
        if (space == null)
        {
            return NotFound();
        }

        space.Status = status;
        _context.SaveChanges();

        return RedirectToAction(nameof(Index), new { areaName = space.AreaName });
    }

    private IActionResult FilteredParkingSpaces(string title, IQueryable<ParkingSpace> query)
    {
        var spaces = query
            .Select(p => new ParkingSpaceItemViewModel
            {
                ParkingSpaceId = p.ParkingSpaceId,
                AreaName = p.AreaName,
                SpaceNumber = p.SpaceNumber,
                Status = p.Status
            })
            .ToList();

        return View(nameof(Index), new ParkingIndexViewModel
        {
            SelectedArea = title,
            ParkingSpaces = spaces
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

        var user = _context.Users.Include(u => u.Vehicles).FirstOrDefault();
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

    public IActionResult DeleteVehicle(int id, string? returnUrl = null)
    {
        var vehicle = _context.Vehicles.Find(id);
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
        var vehicle = _context.Vehicles.Find(id);
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
}
