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

    public IActionResult Index()
    {
        return View(new VehicleListViewModel { Vehicles = GetCurrentUserVehicles() });
    }

    public IActionResult MyVehicles()
    {
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Create()
    {
        return View(new VehicleFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(VehicleFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
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

        return _context.Users.Select(u => (int?)u.UserId).FirstOrDefault();
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
}
