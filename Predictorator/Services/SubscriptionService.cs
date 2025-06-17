using Microsoft.EntityFrameworkCore;
using Resend;
using Predictorator.Data;
using Predictorator.Models;

namespace Predictorator.Services;

public class SubscriptionService
{
    private readonly ApplicationDbContext _db;
    private readonly IResend _resend;
    private readonly IConfiguration _config;

    public SubscriptionService(ApplicationDbContext db, IResend resend, IConfiguration config)
    {
        _db = db;
        _resend = resend;
        _config = config;
    }

    public async Task AddSubscriberAsync(string email, string baseUrl)
    {
        if (await _db.Subscribers.AnyAsync(s => s.Email == email))
            return;

        var subscriber = new Subscriber
        {
            Email = email,
            IsVerified = false,
            VerificationToken = Guid.NewGuid().ToString("N"),
            UnsubscribeToken = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow
        };
        _db.Subscribers.Add(subscriber);
        await _db.SaveChangesAsync();

        var verifyLink = $"{baseUrl}/Subscription/Verify?token={subscriber.VerificationToken}";
        var unsubscribeLink = $"{baseUrl}/Subscription/Unsubscribe?token={subscriber.UnsubscribeToken}";

        var message = new EmailMessage
        {
            From = _config["Resend:From"] ?? "no-reply@example.com",
            Subject = "Verify your email",
            HtmlBody = $"<p>Please <a href=\"{verifyLink}\">verify your email</a>.</p><p>If you did not request this, you can <a href=\"{unsubscribeLink}\">unsubscribe</a>.</p>"
        };
        message.To.Add(email);

        await _resend.EmailSendAsync(message);
    }

    public async Task<bool> VerifyAsync(string token)
    {
        var subscriber = await _db.Subscribers.FirstOrDefaultAsync(s => s.VerificationToken == token);
        if (subscriber == null) return false;
        subscriber.IsVerified = true;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UnsubscribeAsync(string token)
    {
        var subscriber = await _db.Subscribers.FirstOrDefaultAsync(s => s.UnsubscribeToken == token);
        if (subscriber == null) return false;
        _db.Subscribers.Remove(subscriber);
        await _db.SaveChangesAsync();
        return true;
    }
}
