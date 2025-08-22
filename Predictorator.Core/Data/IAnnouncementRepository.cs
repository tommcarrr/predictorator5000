using Predictorator.Core.Models;

namespace Predictorator.Core.Data;

public interface IAnnouncementRepository
{
    Task<Announcement?> GetAsync();
    Task UpsertAsync(Announcement announcement);
}
