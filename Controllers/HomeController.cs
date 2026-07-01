using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ParkHub.Models;

namespace ParkHub.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View("~/Views/Account/Login.cshtml");
    }

    public IActionResult UserHome()
    {
        return View();
    }

    public IActionResult AreaA()
    {
        return View();
    }

    public IActionResult AreaB()
    {
        return View();
    }

    public IActionResult AreaC()
    {
        return View();
    }

    public IActionResult AreaD()
    {
        return View();
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
