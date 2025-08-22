using Predictorator.Core.Data;
using Predictorator.Core.Models;

namespace Predictorator.Tests.Helpers;

public class InMemoryAnnouncementRepository : IAnnouncementRepository
{
    public Announcement? Announcement { get; set; }

    public Task<Announcement?> GetAsync() => Task.FromResult(Announcement);

    public Task UpsertAsync(Announcement announcement)
    {
        Announcement = announcement;
        return Task.CompletedTask;
    }
}
