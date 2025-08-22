using Predictorator.Core.Data;
using Predictorator.Core.Models;

namespace Predictorator.Core.Services;

public class AnnouncementService
{
    private readonly IAnnouncementRepository _repo;
    private readonly IDateTimeProvider _time;

    public AnnouncementService(IAnnouncementRepository repo, IDateTimeProvider time)
    {
        _repo = repo;
        _time = time;
    }

    public Task<Announcement?> GetAsync() => _repo.GetAsync();

    public async Task<Announcement?> GetCurrentAsync()
    {
        var ann = await _repo.GetAsync();
        if (ann == null) return null;
        if (!ann.IsEnabled) return null;
        if (ann.ExpiresAt <= _time.UtcNow) return null;
        return ann;
    }

    public async Task SaveAsync(Announcement announcement)
    {
        if (announcement.Id == Guid.Empty)
            announcement.Id = Guid.NewGuid();

        // Azure Table storage requires DateTime values to be in UTC. Convert the
        // expiry to UTC to avoid serialization errors when persisting the
        // announcement.
        announcement.ExpiresAt = announcement.ExpiresAt.ToUniversalTime();

        await _repo.UpsertAsync(announcement);
    }
}
