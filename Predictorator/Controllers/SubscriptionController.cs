using Microsoft.AspNetCore.Mvc;
using Predictorator.Services;

namespace Predictorator.Controllers;

public class SubscriptionController : Controller
{
    private readonly ISubscriberService _subscriberService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;

    public SubscriptionController(ISubscriberService subscriberService, IEmailService emailService, IConfiguration config)
    {
        _subscriberService = subscriberService;
        _emailService = emailService;
        _config = config;
    }

    [HttpGet]
    public IActionResult Subscribe()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Subscribe(string email)
    {
        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
        {
            ModelState.AddModelError("email", "Invalid email");
            return View();
        }
        var sub = await _subscriberService.AddAsync(email);
        var baseUrl = _config["BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
        var verifyLink = $"{baseUrl}/Subscription/Verify?token={sub.Token}";
        await _emailService.SendVerificationEmailAsync(email, verifyLink);
        ViewData["Message"] = "Please check your email to verify.";
        return View("SubscribeResult");
    }

    [HttpGet]
    public async Task<IActionResult> Verify(Guid token)
    {
        var sub = await _subscriberService.GetByTokenAsync(token);
        if (sub == null) return NotFound();
        await _subscriberService.VerifyAsync(sub);
        ViewData["Message"] = "Email verified!";
        var baseUrl = _config["BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
        var unsubscribe = $"{baseUrl}/Subscription/Unsubscribe?token={token}";
        await _emailService.SendUnsubscribeEmailAsync(sub.Email, unsubscribe);
        return View("SubscribeResult");
    }

    [HttpGet]
    public async Task<IActionResult> Unsubscribe(Guid token)
    {
        var sub = await _subscriberService.GetByTokenAsync(token);
        if (sub == null) return NotFound();
        await _subscriberService.UnsubscribeAsync(sub);
        ViewData["Message"] = "You have been unsubscribed.";
        return View("SubscribeResult");
    }
}
