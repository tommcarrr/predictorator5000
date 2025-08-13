using Azure.Data.Tables;
using Predictorator.Core.Data;
using Predictorator.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Predictorator.Tests;

public class TableGameWeekRepositoryTests : IAsyncLifetime
{
    private Process? _azurite;
    private TableServiceClient _client = null!;

    public async Task InitializeAsync()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(path);
        var psi = new ProcessStartInfo
        {
            FileName = "npx",
            Arguments = $"azurite --silent --location {path}",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        _azurite = Process.Start(psi);
        await Task.Delay(3000);
        _client = new TableServiceClient("UseDevelopmentStorage=true");
    }

    public Task DisposeAsync()
    {
        if (_azurite != null && !_azurite.HasExited)
        {
            _azurite.Kill();
            _azurite.WaitForExit();
        }
        return Task.CompletedTask;
    }

    [Fact(Skip = "Requires Azurite")]
    public async Task ConcurrentAdds_AssignsUniqueIds()
    {
        var repo = new TableGameWeekRepository(_client);
        var tasks = new List<Task>();
        for (var i = 1; i <= 10; i++)
        {
            var gw = new GameWeek
            {
                Season = "2024",
                Number = i,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow
            };
            tasks.Add(repo.AddOrUpdateAsync(gw));
        }
        await Task.WhenAll(tasks);
        var items = await repo.GetGameWeeksAsync("2024");
        Assert.Equal(10, items.Select(g => g.Id).Distinct().Count());
    }

    [Fact(Skip = "Requires Azurite")]
    public async Task InitializesCounterFromExistingRecords()
    {
        var table = _client.GetTableClient("GameWeeks");
        await table.CreateIfNotExistsAsync();
        var existing = new TableEntity("2023", "1")
        {
            ["Id"] = 5,
            ["StartDate"] = DateTime.UtcNow,
            ["EndDate"] = DateTime.UtcNow
        };
        await table.AddEntityAsync(existing);

        var repo = new TableGameWeekRepository(_client);
        var gw = new GameWeek
        {
            Season = "2023",
            Number = 2,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow
        };
        await repo.AddOrUpdateAsync(gw);
        Assert.Equal(6, gw.Id);
    }
}
