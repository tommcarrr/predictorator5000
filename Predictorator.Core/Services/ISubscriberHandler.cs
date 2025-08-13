using System.Threading.Tasks;

namespace Predictorator.Core.Services;

public interface ISubscriberHandler
{
    string Type { get; }
    Task ConfirmAsync(int id);
    Task DeleteAsync(int id);
    Task<AdminSubscriberDto?> AddSubscriberAsync(string contact);
}

