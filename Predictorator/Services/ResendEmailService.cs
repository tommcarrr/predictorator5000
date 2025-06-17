using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Predictorator.Services;

public class ResendEmailService : IEmailService
{
    private readonly HttpClient _client;
    private readonly string _from;

    public ResendEmailService(HttpClient client, IConfiguration configuration)
    {
        _client = client;
        _client.BaseAddress = new Uri("https://api.resend.com/");
        var apiKey = configuration["Resend:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }
        _from = configuration["Resend:From"] ?? "no-reply@example.com";
    }

    public Task SendVerificationEmailAsync(string to, string link)
    {
        var subject = "Verify your email";
        var html = $"<p>Please verify your email by <a href='{link}'>clicking here</a>.</p>";
        return SendAsync(to, subject, html);
    }

    public Task SendUnsubscribeEmailAsync(string to, string link)
    {
        var subject = "Unsubscribe";
        var html = $"<p>If you wish to unsubscribe click <a href='{link}'>here</a>.</p>";
        return SendAsync(to, subject, html);
    }

    private async Task SendAsync(string to, string subject, string html)
    {
        var content = new
        {
            from = _from,
            to,
            subject,
            html
        };
        var json = JsonSerializer.Serialize(content);
        using var stringContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        await _client.PostAsync("emails", stringContent);
    }
}
