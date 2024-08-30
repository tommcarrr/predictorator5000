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

    public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, int? weekOffset)
    {
        if(weekOffset < -10 || weekOffset > 10)
        {
            return BadRequest("Week offset must be between -10 and 10");
        }
        
        var (effectiveFrom, effectiveTo) = GetEffectiveDates(fromDate, toDate, weekOffset);

        var fixtures = await _fixtureService.GetFixturesAsync(effectiveFrom, effectiveTo);
        
        fixtures.CurrentWeekOffset = weekOffset ?? 0;
        fixtures.AutoWeek = fromDate == null && toDate == null;

        if (fixtures.Response.Count == 0)
        {
            fixtures.FromDate = fromDate ?? effectiveFrom;
            fixtures.ToDate = toDate ?? effectiveTo;
            return View(fixtures);
        }
        
        fixtures.FromDate = fromDate ?? fixtures.Response.Min(x => x.Fixture.Date).Date;
        fixtures.ToDate = toDate ?? fixtures.Response.Max(x => x.Fixture.Date).Date;
        return View(fixtures);
    }

    private static (DateTime effectiveFrom, DateTime effectiveTo) GetEffectiveDates(DateTime? fromDate, DateTime? toDate,
        int? weekOffset)
    {
        var effectiveFrom = fromDate;
        var effectiveTo = toDate;
        if (effectiveFrom != null && effectiveTo != null) return (effectiveFrom.Value, effectiveTo.Value);
        
        effectiveFrom = DateTime.Today;
        while (effectiveFrom.Value.DayOfWeek != DayOfWeek.Tuesday )
        {
            effectiveFrom = effectiveFrom.Value.AddDays(-1);
        }
        effectiveFrom = effectiveFrom.Value.AddDays(3);
            
        if(weekOffset.HasValue)
        {
            effectiveFrom = effectiveFrom.Value.AddDays(weekOffset.Value * 7);
        }
        effectiveTo = effectiveFrom.Value.AddDays(7);

        return (effectiveFrom.Value, effectiveTo.Value);
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