using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Predictorator.Models;
using Predictorator.Services;

namespace Predictorator.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IFixtureService _fixtureService;

    public HomeController(ILogger<HomeController> logger, IFixtureService fixtureService)
    {
        _logger = logger;
        _fixtureService = fixtureService;
    }

    public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate)
    {
        fromDate ??= DateTime.Today;
        toDate ??= DateTime.Today + TimeSpan.FromDays(7);
        var fixtures = await _fixtureService.GetFixturesAsync(fromDate.Value, toDate.Value);
        return View(fixtures);
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