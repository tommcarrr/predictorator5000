using Microsoft.EntityFrameworkCore;
using Predictorator.Data;
using Predictorator.Models;

namespace Predictorator.Services;

public class SubscriberService : ISubscriberService
{
    private readonly ApplicationDbContext _db;

    public SubscriberService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Subscriber> AddAsync(string email)
    {
        var subscriber = await _db.Set<Subscriber>().FirstOrDefaultAsync(x => x.Email == email);
        if (subscriber == null)
        {
            subscriber = new Subscriber { Email = email, Token = Guid.NewGuid(), Verified = false };
            _db.Add(subscriber);
            await _db.SaveChangesAsync();
        }
        return subscriber;
    }

    public Task<Subscriber?> GetByTokenAsync(Guid token) =>
        _db.Set<Subscriber>().FirstOrDefaultAsync(x => x.Token == token);

    public async Task VerifyAsync(Subscriber subscriber)
    {
        subscriber.Verified = true;
        await _db.SaveChangesAsync();
    }

    public async Task UnsubscribeAsync(Subscriber subscriber)
    {
        _db.Remove(subscriber);
        await _db.SaveChangesAsync();
    }
}
