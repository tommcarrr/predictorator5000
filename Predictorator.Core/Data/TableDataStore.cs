using Azure;
using Azure.Data.Tables;
using Predictorator.Models;
using System.Linq.Expressions;
using System.Security.Cryptography;

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

    private static int GenerateId() => RandomNumberGenerator.GetInt32(1, int.MaxValue);

    private async Task<bool> ExistsAsync<TEntity>(TableClient table, Expression<Func<TEntity, bool>> filter)
        where TEntity : class, ITableEntity, new()
    {
        await foreach (var _ in table.QueryAsync(filter))
            return true;
        return false;
    }

    private async Task<TModel?> SingleOrDefaultAsync<TEntity, TModel>(TableClient table, Expression<Func<TEntity, bool>> filter, Func<TEntity, TModel> map)
        where TEntity : class, ITableEntity, new()
    {
        await foreach (var e in table.QueryAsync(filter))
            return map(e);
        return default;
    }

    private async Task<int> CountAsync<TEntity>(TableClient table, Expression<Func<TEntity, bool>> filter)
        where TEntity : class, ITableEntity, new()
    {
        var count = 0;
        await foreach (var _ in table.QueryAsync(filter))
            count++;
        return count;
    }

    private async Task<List<TModel>> ListAsync<TEntity, TModel>(TableClient table, Expression<Func<TEntity, bool>>? filter, Func<TEntity, TModel> map)
        where TEntity : class, ITableEntity, new()
    {
        var list = new List<TModel>();
        var query = filter is null ? table.QueryAsync<TEntity>() : table.QueryAsync(filter);
        await foreach (var e in query)
            list.Add(map(e));
        return list;
    }

    private async Task<TEntity?> GetEntityByIdAsync<TEntity>(TableClient table, string partitionKey, int id)
        where TEntity : class, ITableEntity, new()
    {
        try
        {
            var e = await table.GetEntityAsync<TEntity>(partitionKey, id.ToString());
            return e.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    private async Task AddSubscriberAsync<TModel, TEntity>(TableClient table, TModel subscriber, string partitionKey, Func<TModel, TEntity> map)
        where TModel : ISubscriber
        where TEntity : SubscriberEntityBase, new()
    {
        var entity = map(subscriber);
        while (true)
        {
            var id = GenerateId();
            entity.Id = id;
            entity.PartitionKey = partitionKey;
            entity.RowKey = id.ToString();
            try
            {
                await table.AddEntityAsync(entity);
                subscriber.Id = id;
                return;
            }
            catch (RequestFailedException ex) when (ex.Status == 409)
            {
                // collision, retry
            }
        }
    }

    private Task UpdateSubscriberAsync<TModel, TEntity>(TableClient table, TModel subscriber, string partitionKey, Func<TModel, TEntity> map)
        where TModel : ISubscriber
        where TEntity : SubscriberEntityBase, new()
    {
        var entity = map(subscriber);
        entity.PartitionKey = partitionKey;
        entity.RowKey = subscriber.Id.ToString();
        return table.UpsertEntityAsync(entity, TableUpdateMode.Replace);
    }

    private Task RemoveSubscriberAsync(TableClient table, string partitionKey, int id)
        => table.DeleteEntityAsync(partitionKey, id.ToString());

    // Email subscribers
    public Task<bool> EmailSubscriberExistsAsync(string email) =>
        ExistsAsync<SubscriberEntity>(_emailSubscribers, e => e.Email == email);

    public Task AddEmailSubscriberAsync(Subscriber subscriber) =>
        AddSubscriberAsync(_emailSubscribers, subscriber, "E", s => new SubscriberEntity
        {
            Email = s.Email,
            IsVerified = s.IsVerified,
            VerificationToken = s.VerificationToken,
            UnsubscribeToken = s.UnsubscribeToken,
            CreatedAt = s.CreatedAt
        });

    public Task<Subscriber?> GetEmailSubscriberByVerificationTokenAsync(string token) =>
        SingleOrDefaultAsync<SubscriberEntity, Subscriber>(_emailSubscribers, e => e.VerificationToken == token, ToSubscriber);

    public Task<Subscriber?> GetEmailSubscriberByUnsubscribeTokenAsync(string token) =>
        SingleOrDefaultAsync<SubscriberEntity, Subscriber>(_emailSubscribers, e => e.UnsubscribeToken == token, ToSubscriber);

    public Task<int> CountExpiredEmailSubscribersAsync(DateTime cutoff) =>
        CountAsync<SubscriberEntity>(_emailSubscribers, e => !e.IsVerified && e.CreatedAt < cutoff);

    public Task<Subscriber?> GetEmailSubscriberByEmailAsync(string normalizedEmail) =>
        SingleOrDefaultAsync<SubscriberEntity, Subscriber>(_emailSubscribers, e => e.Email.ToLower() == normalizedEmail, ToSubscriber);

    public Task<List<Subscriber>> GetEmailSubscribersAsync() =>
        ListAsync<SubscriberEntity, Subscriber>(_emailSubscribers, null, ToSubscriber);

    public Task<List<Subscriber>> GetVerifiedEmailSubscribersAsync() =>
        ListAsync<SubscriberEntity, Subscriber>(_emailSubscribers, e => e.IsVerified, ToSubscriber);

    public async Task<Subscriber?> GetEmailSubscriberByIdAsync(int id)
    {
        var entity = await GetEntityByIdAsync<SubscriberEntity>(_emailSubscribers, "E", id);
        return entity == null ? null : ToSubscriber(entity);
    }

    public Task UpdateEmailSubscriberAsync(Subscriber subscriber) =>
        UpdateSubscriberAsync(_emailSubscribers, subscriber, "E", s => new SubscriberEntity
        {
            Id = s.Id,
            Email = s.Email,
            IsVerified = s.IsVerified,
            VerificationToken = s.VerificationToken,
            UnsubscribeToken = s.UnsubscribeToken,
            CreatedAt = s.CreatedAt
        });

    public Task RemoveEmailSubscriberAsync(Subscriber subscriber) =>
        RemoveSubscriberAsync(_emailSubscribers, "E", subscriber.Id);

    // SMS subscribers
    public Task<bool> SmsSubscriberExistsAsync(string phoneNumber) =>
        ExistsAsync<SmsSubscriberEntity>(_smsSubscribers, e => e.PhoneNumber == phoneNumber);

    public Task AddSmsSubscriberAsync(SmsSubscriber subscriber) =>
        AddSubscriberAsync(_smsSubscribers, subscriber, "S", s => new SmsSubscriberEntity
        {
            PhoneNumber = s.PhoneNumber,
            IsVerified = s.IsVerified,
            VerificationToken = s.VerificationToken,
            UnsubscribeToken = s.UnsubscribeToken,
            CreatedAt = s.CreatedAt
        });

    public Task<SmsSubscriber?> GetSmsSubscriberByVerificationTokenAsync(string token) =>
        SingleOrDefaultAsync<SmsSubscriberEntity, SmsSubscriber>(_smsSubscribers, e => e.VerificationToken == token, ToSmsSubscriber);

    public Task<SmsSubscriber?> GetSmsSubscriberByUnsubscribeTokenAsync(string token) =>
        SingleOrDefaultAsync<SmsSubscriberEntity, SmsSubscriber>(_smsSubscribers, e => e.UnsubscribeToken == token, ToSmsSubscriber);

    public Task<int> CountExpiredSmsSubscribersAsync(DateTime cutoff) =>
        CountAsync<SmsSubscriberEntity>(_smsSubscribers, e => !e.IsVerified && e.CreatedAt < cutoff);

    public Task<List<SmsSubscriber>> GetSmsSubscribersAsync() =>
        ListAsync<SmsSubscriberEntity, SmsSubscriber>(_smsSubscribers, null, ToSmsSubscriber);

    public Task<List<SmsSubscriber>> GetVerifiedSmsSubscribersAsync() =>
        ListAsync<SmsSubscriberEntity, SmsSubscriber>(_smsSubscribers, e => e.IsVerified, ToSmsSubscriber);

    public async Task<SmsSubscriber?> GetSmsSubscriberByIdAsync(int id)
    {
        var entity = await GetEntityByIdAsync<SmsSubscriberEntity>(_smsSubscribers, "S", id);
        return entity == null ? null : ToSmsSubscriber(entity);
    }

    public Task UpdateSmsSubscriberAsync(SmsSubscriber subscriber) =>
        UpdateSubscriberAsync(_smsSubscribers, subscriber, "S", s => new SmsSubscriberEntity
        {
            Id = s.Id,
            PhoneNumber = s.PhoneNumber,
            IsVerified = s.IsVerified,
            VerificationToken = s.VerificationToken,
            UnsubscribeToken = s.UnsubscribeToken,
            CreatedAt = s.CreatedAt
        });

    public Task RemoveSmsSubscriberAsync(SmsSubscriber subscriber) =>
        RemoveSubscriberAsync(_smsSubscribers, "S", subscriber.Id);

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

    private abstract class SubscriberEntityBase : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public int Id { get; set; }
        public bool IsVerified { get; set; }
        public string VerificationToken { get; set; } = string.Empty;
        public string UnsubscribeToken { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }

    private class SubscriberEntity : SubscriberEntityBase
    {
        public string Email { get; set; } = string.Empty;
    }

    private class SmsSubscriberEntity : SubscriberEntityBase
    {
        public string PhoneNumber { get; set; } = string.Empty;
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
