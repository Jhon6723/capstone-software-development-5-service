using MongoDB.Driver;
using PixPro.Services.Notifications.Domain.Entities;

namespace PixPro.Services.Notifications.Infrastructure.Persistence;

public class NotificationDbContext
{
    private readonly IMongoDatabase _database;

    public NotificationDbContext(IMongoClient mongoClient, string databaseName)
    {
        _database = mongoClient.GetDatabase(databaseName);
    }

    public IMongoCollection<Notification> Notifications => 
        _database.GetCollection<Notification>("notifications");

    public async Task CreateIndexesAsync()
    {
        var indexKeysDefinition = Builders<Notification>.IndexKeys
            .Ascending(n => n.UserId)
            .Descending(n => n.CreatedAt);

        await Notifications.Indexes.CreateOneAsync(
            new CreateIndexModel<Notification>(indexKeysDefinition)
        );

        var isReadIndexDefinition = Builders<Notification>.IndexKeys
            .Ascending(n => n.UserId)
            .Ascending(n => n.IsRead);

        await Notifications.Indexes.CreateOneAsync(
            new CreateIndexModel<Notification>(isReadIndexDefinition)
        );
    }
}
