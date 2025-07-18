using Microsoft.EntityFrameworkCore;
using Predictorator.Data;
using Predictorator.Models;
using Resend;

namespace Predictorator.Services;

public class AdminSubscriberDto
{
    public int Id { get; init; }
    public string Contact { get; init; } = string.Empty;
    public bool IsVerified { get; set; }
    public string Type { get; init; } = string.Empty;

    public AdminSubscriberDto(int id, string contact, bool isVerified, string type)
    {
        Id = id;
        Contact = contact;
        IsVerified = isVerified;
        Type = type;
    }
}

public class AdminService
{
    private readonly ApplicationDbContext _db;
    private readonly IResend _resend;
    private readonly ITwilioSmsSender _sms;
    private readonly IConfiguration _config;

    public AdminService(ApplicationDbContext db, IResend resend, ITwilioSmsSender sms, IConfiguration config)
    {
        _db = db;
        _resend = resend;
        _sms = sms;
        _config = config;
    }

    public async Task<List<AdminSubscriberDto>> GetSubscribersAsync()
    {
        var emails = await _db.Subscribers
            .Select(s => new AdminSubscriberDto(s.Id, s.Email, s.IsVerified, "Email"))
            .ToListAsync();
        var phones = await _db.SmsSubscribers
            .Select(s => new AdminSubscriberDto(s.Id, s.PhoneNumber, s.IsVerified, "SMS"))
            .ToListAsync();
        return emails.Concat(phones).OrderBy(s => s.Contact).ToList();
    }

    public async Task ConfirmAsync(string type, int id)
    {
        if (type == "Email")
        {
            var entity = await _db.Subscribers.FindAsync(id);
            if (entity != null) entity.IsVerified = true;
        }
        else
        {
            var entity = await _db.SmsSubscribers.FindAsync(id);
            if (entity != null) entity.IsVerified = true;
        }
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(string type, int id)
    {
        if (type == "Email")
        {
            var entity = await _db.Subscribers.FindAsync(id);
            if (entity != null) _db.Subscribers.Remove(entity);
        }
        else
        {
            var entity = await _db.SmsSubscribers.FindAsync(id);
            if (entity != null) _db.SmsSubscribers.Remove(entity);
        }
        await _db.SaveChangesAsync();
    }

    public async Task SendTestAsync(IEnumerable<AdminSubscriberDto> recipients)
    {
        foreach (var s in recipients)
        {
            if (s.Type == "Email")
            {
                var message = new EmailMessage
                {
                    From = _config["Resend:From"] ?? "Predictorator <noreply@example.com>",
                    Subject = "Test Notification",
                    HtmlBody = "<p>This is a test notification.</p>"
                };
                message.To.Add(s.Contact);
                await _resend.EmailSendAsync(message);
            }
            else
            {
                await _sms.SendSmsAsync(s.Contact, "Test notification");
            }
        }
    }
}
