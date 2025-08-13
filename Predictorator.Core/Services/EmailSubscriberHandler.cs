using System;
using System.Threading.Tasks;
using Predictorator.Core.Data;
using Predictorator.Core.Models;

namespace Predictorator.Core.Services;

public class EmailSubscriberHandler : ISubscriberHandler
{
    private readonly IDataStore _store;
    private readonly IDateTimeProvider _time;

    public EmailSubscriberHandler(IDataStore store, IDateTimeProvider time)
    {
        _store = store;
        _time = time;
    }

    public string Type => "Email";

    public async Task ConfirmAsync(int id)
    {
        var entity = await _store.GetEmailSubscriberByIdAsync(id);
        if (entity != null)
        {
            entity.IsVerified = true;
            await _store.UpdateEmailSubscriberAsync(entity);
        }
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _store.GetEmailSubscriberByIdAsync(id);
        if (entity != null)
        {
            await _store.RemoveEmailSubscriberAsync(entity);
        }
    }

    public async Task<AdminSubscriberDto?> AddSubscriberAsync(string contact)
    {
        if (await _store.EmailSubscriberExistsAsync(contact))
            return null;
        var sub = new Subscriber
        {
            Email = contact,
            IsVerified = true,
            VerificationToken = Guid.NewGuid().ToString("N"),
            UnsubscribeToken = Guid.NewGuid().ToString("N"),
            CreatedAt = _time.UtcNow
        };
        await _store.AddEmailSubscriberAsync(sub);
        return new AdminSubscriberDto(sub.Id, sub.Email, sub.IsVerified, Type);
    }
}

