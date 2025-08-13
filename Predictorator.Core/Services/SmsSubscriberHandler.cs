using System;
using System.Threading.Tasks;
using Predictorator.Core.Data;
using Predictorator.Core.Models;
using Microsoft.Extensions.Logging;

namespace Predictorator.Core.Services;

public class SmsSubscriberHandler : ISubscriberHandler
{
    private readonly IDataStore _store;
    private readonly IDateTimeProvider _time;
    private readonly ILogger<SmsSubscriberHandler> _logger;

    public SmsSubscriberHandler(IDataStore store, IDateTimeProvider time, ILogger<SmsSubscriberHandler> logger)
    {
        _store = store;
        _time = time;
        _logger = logger;
    }

    public string Type => "SMS";

    public async Task ConfirmAsync(int id)
    {
        _logger.LogInformation("Confirming SMS subscriber {Id}", id);
        var entity = await _store.GetSmsSubscriberByIdAsync(id);
        if (entity != null)
        {
            entity.IsVerified = true;
            await _store.UpdateSmsSubscriberAsync(entity);
        }
    }

    public async Task DeleteAsync(int id)
    {
        _logger.LogInformation("Deleting SMS subscriber {Id}", id);
        var entity = await _store.GetSmsSubscriberByIdAsync(id);
        if (entity != null)
        {
            await _store.RemoveSmsSubscriberAsync(entity);
        }
    }

    public async Task<AdminSubscriberDto?> AddSubscriberAsync(string contact)
    {
        if (await _store.SmsSubscriberExistsAsync(contact))
            return null;
        var sub = new SmsSubscriber
        {
            PhoneNumber = contact,
            IsVerified = true,
            VerificationToken = Guid.NewGuid().ToString("N"),
            UnsubscribeToken = Guid.NewGuid().ToString("N"),
            CreatedAt = _time.UtcNow
        };
        await _store.AddSmsSubscriberAsync(sub);
        return new AdminSubscriberDto(sub.Id, sub.PhoneNumber, sub.IsVerified, Type);
    }
}

