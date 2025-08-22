using Predictorator.Core.Models;
using Predictorator.Core.Services;
using Predictorator.Tests.Helpers;

namespace Predictorator.Tests;

public class AnnouncementServiceTests
{
    [Fact]
    public async Task GetCurrentAsync_returns_announcement_when_enabled_and_not_expired()
    {
        var repo = new InMemoryAnnouncementRepository
        {
            Announcement = new Announcement
            {
                Id = Guid.NewGuid(),
                Title = "Title",
                Message = "Message",
                IsEnabled = true,
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            }
        };
        var time = new FakeDateTimeProvider { UtcNow = DateTime.UtcNow };
        var svc = new AnnouncementService(repo, time);
        var result = await svc.GetCurrentAsync();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetCurrentAsync_returns_null_when_disabled()
    {
        var repo = new InMemoryAnnouncementRepository
        {
            Announcement = new Announcement
            {
                Id = Guid.NewGuid(),
                Title = "Title",
                Message = "Message",
                IsEnabled = false,
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            }
        };
        var time = new FakeDateTimeProvider { UtcNow = DateTime.UtcNow };
        var svc = new AnnouncementService(repo, time);
        var result = await svc.GetCurrentAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrentAsync_returns_null_when_expired()
    {
        var repo = new InMemoryAnnouncementRepository
        {
            Announcement = new Announcement
            {
                Id = Guid.NewGuid(),
                Title = "Title",
                Message = "Message",
                IsEnabled = true,
                ExpiresAt = DateTime.UtcNow.AddDays(-1)
            }
        };
        var time = new FakeDateTimeProvider { UtcNow = DateTime.UtcNow };
        var svc = new AnnouncementService(repo, time);
        var result = await svc.GetCurrentAsync();
        Assert.Null(result);
    }
}
