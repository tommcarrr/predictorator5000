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
        var effectiveFrom = fromDate;
        var effectiveTo = toDate;
        if(effectiveFrom == null || effectiveTo == null)
        {
            effectiveFrom = DateTime.Today;
            while (effectiveFrom.Value.DayOfWeek != DayOfWeek.Tuesday )
            {
                effectiveFrom = effectiveFrom.Value.AddDays(-1);
            }
            effectiveFrom = effectiveFrom.Value.AddDays(3);
            effectiveTo = effectiveFrom.Value.AddDays(7);
        }
        
        var fixtures = await _fixtureService.GetFixturesAsync(effectiveFrom.Value, effectiveTo.Value);
        fixtures.FromDate = fromDate ?? fixtures.Response.Min(x => x.Fixture.Date).Date;
        fixtures.ToDate = toDate ?? fixtures.Response.Max(x => x.Fixture.Date).Date;
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