using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkHub.Data;
using ParkHub.Models;

namespace ParkHub.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Contact()
    {
        return View();
    }

    public IActionResult Profile()
    {
        var user = _context.Users
            .Include(u => u.Vehicles)
            .FirstOrDefault();

        if (user == null)
        {
            return RedirectToAction(nameof(Index));
        }

        var recentReservations = _context.Reservations
            .Include(r => r.ParkingSpace)
            .Include(r => r.Vehicle)
            .Where(r => r.Vehicle.UserId == user.UserId)
            .OrderByDescending(r => r.ReservationDate)
            .Take(2)
            .Select(r => new RecentReservationItemViewModel
            {
                ReservationId = r.ReservationId,
                ReservationDate = r.ReservationDate,
                TotalPrice = r.TotalPrice,
                ReservationStatus = r.ReservationStatus ? "Reserved" : "Completed",
                AreaName = r.ParkingSpace.AreaName,
                SpaceNumber = r.ParkingSpace.SpaceNumber
            })
            .ToList();

        var model = new UserProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Vehicles = user.Vehicles.Select(v => new UserVehicleSummaryViewModel
            {
                VehicleId = v.VehicleId,
                PlateNumber = v.PlateNumber,
                VehicleType = v.VehicleType,
                Color = v.Color
            }).ToList(),
            RecentReservations = recentReservations
        };

        return View(model);
    }

    public IActionResult UserHome()
    {
        return View();
    }

    public IActionResult AreaA()
    {
        return View("AreaDetail", GetAreaViewModel("Area A"));
    }

    public IActionResult AreaB()
    {
        return View("AreaDetail", GetAreaViewModel("Area B"));
    }

    public IActionResult AreaC()
    {
        return View("AreaDetail", GetAreaViewModel("Area C"));
    }

    public IActionResult AreaD()
    {
        return View("AreaDetail", GetAreaViewModel("Area D"));
    }

    private ParkingIndexViewModel GetAreaViewModel(string areaName)
    {
        var spaces = _context.ParkingSpaces
            .Where(p => p.AreaName == areaName)
            .Select(p => new ParkingSpaceItemViewModel
            {
                ParkingSpaceId = p.ParkingSpaceId,
                AreaName = p.AreaName,
                SpaceNumber = p.SpaceNumber,
                Status = p.Status
            })
            .ToList();

        if (!spaces.Any())
        {
            spaces = Enumerable.Range(1, 12)
                .Select(i => new ParkingSpaceItemViewModel
                {
                    ParkingSpaceId = 0,
                    AreaName = areaName,
                    SpaceNumber = i.ToString("00"),
                    Status = false
                })
                .ToList();
        }

        return new ParkingIndexViewModel
        {
            SelectedArea = areaName,
            ParkingSpaces = spaces
        };
    }

    public IActionResult Dashboard()
    {
        var model = new DashboardViewModel
        {
            TotalParkingSpaces = _context.ParkingSpaces.Count(),
            AvailableParkingSpaces = _context.ParkingSpaces.Count(p => p.Status == false),
            ReservedParkingSpaces = _context.ParkingSpaces.Count(p => p.Status == true),
            TotalUsers = _context.Users.Count(),
            TotalReservations = _context.Reservations.Count(),
            TotalRevenue = _context.Payments.Sum(p => p.Amount),
            OccupancyRate = _context.ParkingSpaces.Any()
                ? Math.Round((double)_context.ParkingSpaces.Count(p => p.Status == true) / _context.ParkingSpaces.Count() * 100, 1)
                : 0
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
