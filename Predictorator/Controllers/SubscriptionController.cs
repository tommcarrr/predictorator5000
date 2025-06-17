using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Predictorator.Services;

namespace Predictorator.Controllers;

public class SubscriptionController : Controller
{
    private readonly SubscriptionService _service;

    public SubscriptionController(SubscriptionService service)
    {
        _service = service;
    }

    [HttpGet]
    public IActionResult Subscribe()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Subscribe(string email)
    {
        if (!new EmailAddressAttribute().IsValid(email))
        {
            ModelState.AddModelError("email", "Invalid email");
            return View();
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        await _service.AddSubscriberAsync(email, baseUrl);
        return View("CheckEmail");
    }

    [HttpGet]
    public async Task<IActionResult> Verify(string token)
    {
        var result = await _service.VerifyAsync(token);
        return View(model: result);
    }

    [HttpGet]
    public async Task<IActionResult> Unsubscribe(string token)
    {
        var result = await _service.UnsubscribeAsync(token);
        return View(model: result);
    }
}
