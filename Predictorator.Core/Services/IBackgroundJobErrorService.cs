using Predictorator.Core.Models;

namespace Predictorator.Core.Services;

public interface IBackgroundJobErrorService
{
    Task AddErrorAsync(BackgroundJobError error);
    Task<IReadOnlyList<BackgroundJobError>> GetErrorsAsync();
    Task DeleteErrorAsync(string id);
}

