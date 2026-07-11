using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkHub.Data;
using ParkHub.Models;
using System.Security.Claims;

namespace ParkHub.Controllers;

[Authorize]
public class VehicleController : Controller
{
    private readonly ApplicationDbContext _context;

    public VehicleController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index(int? parkingSpaceId, string? areaName, string? returnUrl)
    {
        return View(new VehicleListViewModel
        {
            Vehicles = GetCurrentUserVehicles(),
            ParkingSpaceId = parkingSpaceId,
            AreaName = areaName,
            ReturnUrl = returnUrl
        });
    }

    public IActionResult MyVehicles()
    {
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ContinueToReservation(int parkingSpaceId, int vehicleId, string? returnUrl)
    {
        var vehicle = FindCurrentUserVehicle(vehicleId);

        if (vehicle == null)
        {
            return RedirectToAction(nameof(Index));
        }

        return RedirectToReservation(parkingSpaceId, vehicle.VehicleId, returnUrl);
    }

    public IActionResult Create()
    {
        return View(new VehicleFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(VehicleFormViewModel model, int? parkingSpaceId, string? areaName, string? returnUrl)
    {
        if (!ModelState.IsValid)
        {
            if (parkingSpaceId.GetValueOrDefault() > 0)
            {
                return View(nameof(Index), new VehicleListViewModel
                {
                    Vehicles = GetCurrentUserVehicles(),
                    NewVehicle = model,
                    ParkingSpaceId = parkingSpaceId,
                    AreaName = areaName,
                    ReturnUrl = returnUrl
                });
            }

            return View(model);
        }

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            TempData["SuccessMessage"] = "Please sign in before adding a vehicle.";
            return RedirectToAction("Login", "Account");
        }

        var vehicle = new Vehicle
        {
            PlateNumber = model.PlateNumber,
            VehicleType = model.VehicleType,
            Color = model.Color,
            UserId = userId.Value
        };

        _context.Vehicles.Add(vehicle);
        _context.SaveChanges();

        var selectedParkingSpaceId = parkingSpaceId.GetValueOrDefault();
        if (selectedParkingSpaceId > 0)
        {
            // Use the exact same ownership check and redirect path as the Select button.
            return ContinueToReservation(selectedParkingSpaceId, vehicle.VehicleId, returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int id)
    {
        var vehicle = FindCurrentUserVehicle(id);
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
    [ValidateAntiForgeryToken]
    public IActionResult Edit(VehicleFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var vehicle = FindCurrentUserVehicle(model.VehicleId);
        if (vehicle == null)
        {
            return NotFound();
        }

        vehicle.PlateNumber = model.PlateNumber;
        vehicle.VehicleType = model.VehicleType;
        vehicle.Color = model.Color;
        _context.SaveChanges();

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Delete(int id)
    {
        var vehicle = FindCurrentUserVehicle(id);
        if (vehicle == null)
        {
            return NotFound();
        }

        return View(vehicle);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        var vehicle = FindCurrentUserVehicle(id);
        if (vehicle == null)
        {
            return NotFound();
        }

        _context.Vehicles.Remove(vehicle);
        _context.SaveChanges();

        return RedirectToAction(nameof(Index));
    }

    private int? GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdValue, out var userId))
        {
            return userId;
        }

        return null;
    }

    private Vehicle? FindCurrentUserVehicle(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return null;
        }

        return _context.Vehicles.FirstOrDefault(v => v.VehicleId == id && v.UserId == userId.Value);
    }

    private List<VehicleItemViewModel> GetCurrentUserVehicles()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return new List<VehicleItemViewModel>();
        }

        return _context.Vehicles
            .Where(v => v.UserId == userId.Value)
            .Select(v => new VehicleItemViewModel
            {
                VehicleId = v.VehicleId,
                PlateNumber = v.PlateNumber,
                VehicleType = v.VehicleType,
                Color = v.Color
            })
            .ToList();
    }

    private IActionResult RedirectToReservation(int parkingSpaceId, int vehicleId, string? returnUrl)
    {
        var space = _context.ParkingSpaces.Find(parkingSpaceId);
        if (space == null)
        {
            return RedirectToAction(nameof(Index));
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            var separator = returnUrl.Contains('?') ? "&" : "?";
            return Redirect($"{returnUrl}{separator}openReservation=true&parkingSpaceId={parkingSpaceId}&vehicleId={vehicleId}");
        }

        var areaAction = space.AreaName switch
        {
            "Area A" => "AreaA",
            "Area B" => "AreaB",
            "Area C" => "AreaC",
            "Area D" => "AreaD",
            _ => "UserHome"
        };

        return RedirectToAction(areaAction, "Home", new
        {
            openReservation = true,
            parkingSpaceId,
            vehicleId
        });
    }
}
