using Azure;
using Azure.Data.Tables;
using Predictorator.Models;

namespace Predictorator.Data;

public class TableDataStore : IDataStore
{
    private readonly TableClient _emailSubscribers;
    private readonly TableClient _smsSubscribers;
    private readonly TableClient _sentNotifications;

    public TableDataStore(TableServiceClient client)
    {
        _emailSubscribers = client.GetTableClient("EmailSubscribers");
        _emailSubscribers.CreateIfNotExists();
        _smsSubscribers = client.GetTableClient("SmsSubscribers");
        _smsSubscribers.CreateIfNotExists();
        _sentNotifications = client.GetTableClient("SentNotifications");
        _sentNotifications.CreateIfNotExists();
    }

    private static Subscriber ToSubscriber(SubscriberEntity e) => new()
    {
        Id = e.Id,
        Email = e.Email,
        IsVerified = e.IsVerified,
        VerificationToken = e.VerificationToken,
        UnsubscribeToken = e.UnsubscribeToken,
        CreatedAt = e.CreatedAt
    };

    private static SmsSubscriber ToSmsSubscriber(SmsSubscriberEntity e) => new()
    {
        Id = e.Id,
        PhoneNumber = e.PhoneNumber,
        IsVerified = e.IsVerified,
        VerificationToken = e.VerificationToken,
        UnsubscribeToken = e.UnsubscribeToken,
        CreatedAt = e.CreatedAt
    };

    private async Task<int> GetNextIdAsync(TableClient table)
    {
        var max = 0;
        await foreach (var e in table.QueryAsync<TableEntity>(select: new[] { "Id" }))
        {
            if (e.TryGetValue("Id", out object? obj) && obj is int id && id > max)
                max = id;
        }
        return max + 1;
    }

    // Email subscribers
    public async Task<bool> EmailSubscriberExistsAsync(string email)
    {
        await foreach (var _ in _emailSubscribers.QueryAsync<SubscriberEntity>(e => e.Email == email))
            return true;
        return false;
    }

    public async Task AddEmailSubscriberAsync(Subscriber subscriber)
    {
        subscriber.Id = await GetNextIdAsync(_emailSubscribers);
        var entity = new SubscriberEntity
        {
            PartitionKey = "E",
            RowKey = subscriber.Id.ToString(),
            Id = subscriber.Id,
            Email = subscriber.Email,
            IsVerified = subscriber.IsVerified,
            VerificationToken = subscriber.VerificationToken,
            UnsubscribeToken = subscriber.UnsubscribeToken,
            CreatedAt = subscriber.CreatedAt
        };
        await _emailSubscribers.AddEntityAsync(entity);
    }

    public async Task<Subscriber?> GetEmailSubscriberByVerificationTokenAsync(string token)
    {
        await foreach (var e in _emailSubscribers.QueryAsync<SubscriberEntity>(e => e.VerificationToken == token))
            return ToSubscriber(e);
        return null;
    }

    public async Task<Subscriber?> GetEmailSubscriberByUnsubscribeTokenAsync(string token)
    {
        await foreach (var e in _emailSubscribers.QueryAsync<SubscriberEntity>(e => e.UnsubscribeToken == token))
            return ToSubscriber(e);
        return null;
    }

    public async Task<int> CountExpiredEmailSubscribersAsync(DateTime cutoff)
    {
        var count = 0;
        await foreach (var _ in _emailSubscribers.QueryAsync<SubscriberEntity>(e => !e.IsVerified && e.CreatedAt < cutoff))
            count++;
        return count;
    }

    public async Task<Subscriber?> GetEmailSubscriberByEmailAsync(string normalizedEmail)
    {
        await foreach (var e in _emailSubscribers.QueryAsync<SubscriberEntity>(e => e.Email.ToLower() == normalizedEmail))
            return ToSubscriber(e);
        return null;
    }

    public async Task<List<Subscriber>> GetEmailSubscribersAsync()
    {
        var list = new List<Subscriber>();
        await foreach (var e in _emailSubscribers.QueryAsync<SubscriberEntity>())
            list.Add(ToSubscriber(e));
        return list;
    }

    public async Task<List<Subscriber>> GetVerifiedEmailSubscribersAsync()
    {
        var list = new List<Subscriber>();
        await foreach (var e in _emailSubscribers.QueryAsync<SubscriberEntity>(e => e.IsVerified))
            list.Add(ToSubscriber(e));
        return list;
    }

    public async Task<Subscriber?> GetEmailSubscriberByIdAsync(int id)
    {
        try
        {
            var e = await _emailSubscribers.GetEntityAsync<SubscriberEntity>("E", id.ToString());
            return ToSubscriber(e.Value);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task UpdateEmailSubscriberAsync(Subscriber subscriber)
    {
        var entity = new SubscriberEntity
        {
            PartitionKey = "E",
            RowKey = subscriber.Id.ToString(),
            Id = subscriber.Id,
            Email = subscriber.Email,
            IsVerified = subscriber.IsVerified,
            VerificationToken = subscriber.VerificationToken,
            UnsubscribeToken = subscriber.UnsubscribeToken,
            CreatedAt = subscriber.CreatedAt
        };
        await _emailSubscribers.UpsertEntityAsync(entity, TableUpdateMode.Replace);
    }

    public Task RemoveEmailSubscriberAsync(Subscriber subscriber) =>
        _emailSubscribers.DeleteEntityAsync("E", subscriber.Id.ToString());

    // SMS subscribers
    public async Task<bool> SmsSubscriberExistsAsync(string phoneNumber)
    {
        await foreach (var _ in _smsSubscribers.QueryAsync<SmsSubscriberEntity>(e => e.PhoneNumber == phoneNumber))
            return true;
        return false;
    }

    public async Task AddSmsSubscriberAsync(SmsSubscriber subscriber)
    {
        subscriber.Id = await GetNextIdAsync(_smsSubscribers);
        var entity = new SmsSubscriberEntity
        {
            PartitionKey = "S",
            RowKey = subscriber.Id.ToString(),
            Id = subscriber.Id,
            PhoneNumber = subscriber.PhoneNumber,
            IsVerified = subscriber.IsVerified,
            VerificationToken = subscriber.VerificationToken,
            UnsubscribeToken = subscriber.UnsubscribeToken,
            CreatedAt = subscriber.CreatedAt
        };
        await _smsSubscribers.AddEntityAsync(entity);
    }

    public async Task<SmsSubscriber?> GetSmsSubscriberByVerificationTokenAsync(string token)
    {
        await foreach (var e in _smsSubscribers.QueryAsync<SmsSubscriberEntity>(e => e.VerificationToken == token))
            return ToSmsSubscriber(e);
        return null;
    }

    public async Task<SmsSubscriber?> GetSmsSubscriberByUnsubscribeTokenAsync(string token)
    {
        await foreach (var e in _smsSubscribers.QueryAsync<SmsSubscriberEntity>(e => e.UnsubscribeToken == token))
            return ToSmsSubscriber(e);
        return null;
    }

    public async Task<int> CountExpiredSmsSubscribersAsync(DateTime cutoff)
    {
        var count = 0;
        await foreach (var _ in _smsSubscribers.QueryAsync<SmsSubscriberEntity>(e => !e.IsVerified && e.CreatedAt < cutoff))
            count++;
        return count;
    }

    public async Task<List<SmsSubscriber>> GetSmsSubscribersAsync()
    {
        var list = new List<SmsSubscriber>();
        await foreach (var e in _smsSubscribers.QueryAsync<SmsSubscriberEntity>())
            list.Add(ToSmsSubscriber(e));
        return list;
    }

    public async Task<List<SmsSubscriber>> GetVerifiedSmsSubscribersAsync()
    {
        var list = new List<SmsSubscriber>();
        await foreach (var e in _smsSubscribers.QueryAsync<SmsSubscriberEntity>(e => e.IsVerified))
            list.Add(ToSmsSubscriber(e));
        return list;
    }

    public async Task<SmsSubscriber?> GetSmsSubscriberByIdAsync(int id)
    {
        try
        {
            var e = await _smsSubscribers.GetEntityAsync<SmsSubscriberEntity>("S", id.ToString());
            return ToSmsSubscriber(e.Value);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task UpdateSmsSubscriberAsync(SmsSubscriber subscriber)
    {
        var entity = new SmsSubscriberEntity
        {
            PartitionKey = "S",
            RowKey = subscriber.Id.ToString(),
            Id = subscriber.Id,
            PhoneNumber = subscriber.PhoneNumber,
            IsVerified = subscriber.IsVerified,
            VerificationToken = subscriber.VerificationToken,
            UnsubscribeToken = subscriber.UnsubscribeToken,
            CreatedAt = subscriber.CreatedAt
        };
        await _smsSubscribers.UpsertEntityAsync(entity, TableUpdateMode.Replace);
    }

    public Task RemoveSmsSubscriberAsync(SmsSubscriber subscriber) =>
        _smsSubscribers.DeleteEntityAsync("S", subscriber.Id.ToString());

    // Sent notifications
    public async Task<bool> SentNotificationExistsAsync(string type, string key)
    {
        try
        {
            await _sentNotifications.GetEntityAsync<SentNotificationEntity>(type, key);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
    }

    public async Task AddSentNotificationAsync(SentNotification notification)
    {
        var entity = new SentNotificationEntity
        {
            PartitionKey = notification.Type,
            RowKey = notification.Key,
            SentAt = notification.SentAt
        };
        await _sentNotifications.UpsertEntityAsync(entity);
    }

    private class SubscriberEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "E";
        public string RowKey { get; set; } = string.Empty;
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
        public string VerificationToken { get; set; } = string.Empty;
        public string UnsubscribeToken { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }

    private class SmsSubscriberEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "S";
        public string RowKey { get; set; } = string.Empty;
        public int Id { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
        public string VerificationToken { get; set; } = string.Empty;
        public string UnsubscribeToken { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }

    private class SentNotificationEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
