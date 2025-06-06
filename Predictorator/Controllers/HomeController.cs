using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Predictorator.Models;
using Predictorator.Services;

namespace Predictorator.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IFixtureService _fixtureService;
    private readonly IDateRangeCalculator _dateRangeCalculator;

    public HomeController(ILogger<HomeController> logger, IFixtureService fixtureService, IDateRangeCalculator dateRangeCalculator)
    {
        _logger = logger;
        _fixtureService = fixtureService;
        _dateRangeCalculator = dateRangeCalculator;
    }

    public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, int? weekOffset)
    {
        if(weekOffset is < -10 or > 10)
        {
            return BadRequest("Week offset must be between -10 and 10");
        }
        
        var (effectiveFrom, effectiveTo) = _dateRangeCalculator.GetDates(fromDate, toDate, weekOffset);

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