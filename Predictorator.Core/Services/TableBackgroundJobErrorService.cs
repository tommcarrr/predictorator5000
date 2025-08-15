using Azure.Data.Tables;
using Predictorator.Core.Models;

namespace Predictorator.Core.Services;

public class TableBackgroundJobErrorService : IBackgroundJobErrorService
{
    private readonly TableClient _table;

    public TableBackgroundJobErrorService(TableServiceClient client)
    {
        _table = client.GetTableClient("BackgroundJobErrors");
        _table.CreateIfNotExists();
    }

    public Task AddErrorAsync(BackgroundJobError error)
    {
        return _table.AddEntityAsync(error);
    }

    public Task<IReadOnlyList<BackgroundJobError>> GetErrorsAsync()
    {
        var items = _table.Query<BackgroundJobError>().OrderByDescending(e => e.OccurredAt).ToList();
        return Task.FromResult<IReadOnlyList<BackgroundJobError>>(items);
    }

    public Task DeleteErrorAsync(string id)
    {
        return _table.DeleteEntityAsync("errors", id);
    }
}

