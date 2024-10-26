using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BeanScene.Models;

namespace BeanScene.Controllers;

public class RestaurantController : Controller
{
    private readonly ILogger<RestaurantController> _logger;

    public RestaurantController(ILogger<RestaurantController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
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
