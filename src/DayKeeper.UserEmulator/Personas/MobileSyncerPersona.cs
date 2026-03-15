using System.Text.Json;
using DayKeeper.UserEmulator.Client;

namespace DayKeeper.UserEmulator.Personas;

public sealed class MobileSyncerPersona : IPersona
{
    private static readonly string[] ContentTypes =
    [
        "image/jpeg", "image/png", "application/pdf", "text/plain", "image/gif",
    ];

    private static readonly string[] FileExtensions =
    [
        "jpg", "png", "pdf", "txt", "gif",
    ];

    // ChangeLogEntityType integer values matching the server enum
    private static readonly int[] SyncEntityTypes =
    [
        8,  // TaskItem
        5,  // CalendarEvent
        16, // ShoppingList
        17, // ListItem
        12, // Person
    ];

    // ChangeOperation integer values matching the server enum
    private static readonly int[] SyncOperations =
    [
        0, // Created
        1, // Updated
        2, // Deleted
    ];

    public string Name => "MobileSyncer";

    public async Task SeedAsync(PersonaContext ctx, CancellationToken ct)
    {
        var deviceCount = ctx.DataFactory.RandomInt(1, 2);
        for (var i = 0; i < deviceCount; i++)
        {
            await CreateDeviceAsync(ctx, ct).ConfigureAwait(false);
        }
    }

    public async Task RunIterationAsync(PersonaContext ctx, CancellationToken ct)
    {
        try
        {
            await DispatchOperationAsync(ctx, ctx.DataFactory.RandomInt(0, 99), ct).ConfigureAwait(false);
        }
        catch (GraphQLException)
        {
            // error already recorded in metrics
        }
        catch (HttpRequestException)
        {
            // error already recorded in metrics
        }
        catch (InvalidOperationException)
        {
            // sync/attachment errors already recorded in metrics
        }
    }

    private static async Task DispatchOperationAsync(PersonaContext ctx, int roll, CancellationToken ct)
    {
        if (roll < 20)
        {
            await SyncPullAsync(ctx, ct).ConfigureAwait(false);
        }
        else if (roll < 35)
        {
            await SyncPushAsync(ctx, ct).ConfigureAwait(false);
        }
        else if (roll < 50)
        {
            await AttachmentUploadAsync(ctx, ct).ConfigureAwait(false);
        }
        else if (roll < 60)
        {
            await AttachmentDownloadAsync(ctx, ct).ConfigureAwait(false);
        }
        else if (roll < 68)
        {
            await AttachmentMetadataAsync(ctx, ct).ConfigureAwait(false);
        }
        else if (roll < 73)
        {
            await CreateDeviceAsync(ctx, ct).ConfigureAwait(false);
        }
        else if (roll < 80)
        {
            await UpdateDeviceNotificationPreferenceAsync(ctx, ct).ConfigureAwait(false);
        }
        else if (roll < 85)
        {
            await UpdateDeviceAsync(ctx, ct).ConfigureAwait(false);
        }
        else if (roll < 90)
        {
            await AttachmentDeleteAsync(ctx, ct).ConfigureAwait(false);
        }
        else if (roll < 95)
        {
            await GetDevicesAsync(ctx, ct).ConfigureAwait(false);
        }
        else
        {
            await DeleteDeviceAsync(ctx, ct).ConfigureAwait(false);
        }
    }

    private static async Task<Guid> CreateDeviceAsync(PersonaContext ctx, CancellationToken ct)
    {
        try
        {
            var (deviceName, platform, fcmToken) = ctx.DataFactory.GenerateDevice();
            var result = await ctx.ApiClient.GraphQLAsync(
                "CreateDevice",
                GraphQLOperations.CreateDevice,
                new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { userId = ctx.UserId, deviceName, platform, fcmToken } },
                ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
            var id = result.GetProperty("createDevice").GetProperty("device").GetProperty("id").GetGuid();
            ctx.DeviceIds.Add(id);
            return id;
        }
        catch (GraphQLException)
        {
            return Guid.Empty;
        }
        catch (HttpRequestException)
        {
            return Guid.Empty;
        }
    }

    private static async Task SyncPullAsync(PersonaContext ctx, CancellationToken ct)
    {
        var cursor = ctx.Coordinator.GetSyncCursor(ctx.UserId);
        var spaceId = ctx.GetWorkingSpaceId();
        var response = await ctx.SyncClient.PullAsync(cursor, spaceId, limit: 100, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
        if (response.Cursor > cursor)
        {
            ctx.Coordinator.UpdateSyncCursor(ctx.UserId, response.Cursor);
        }
    }

    private static async Task SyncPushAsync(PersonaContext ctx, CancellationToken ct)
    {
        var count = ctx.DataFactory.RandomInt(1, 5);
        var entries = BuildSyncPushEntries(ctx, count);
        await ctx.SyncClient.PushAsync(entries, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }

    private static List<SyncPushEntry> BuildSyncPushEntries(PersonaContext ctx, int count)
    {
        var entries = new List<SyncPushEntry>(count);
        for (var i = 0; i < count; i++)
        {
            entries.Add(BuildSingleSyncEntry(ctx));
        }

        return entries;
    }

    private static SyncPushEntry BuildSingleSyncEntry(PersonaContext ctx)
    {
        var entityType = ctx.DataFactory.PickRandom(SyncEntityTypes);
        var operation = ctx.DataFactory.PickRandom(SyncOperations);
        var entityId = ResolveEntityIdForSync(ctx, entityType);
        return new SyncPushEntry(entityType, entityId, operation, DateTime.UtcNow, (JsonElement?)null);
    }

    private static Guid ResolveEntityIdForSync(PersonaContext ctx, int entityType)
    {
        return entityType switch
        {
            8 when !ctx.TaskItemIds.IsEmpty => ctx.DataFactory.PickRandom([.. ctx.TaskItemIds]),      // TaskItem
            5 when !ctx.CalendarEventIds.IsEmpty => ctx.DataFactory.PickRandom([.. ctx.CalendarEventIds]), // CalendarEvent
            16 when !ctx.ShoppingListIds.IsEmpty => ctx.DataFactory.PickRandom([.. ctx.ShoppingListIds]), // ShoppingList
            17 when !ctx.ListItemIds.IsEmpty => ctx.DataFactory.PickRandom([.. ctx.ListItemIds]),         // ListItem
            12 when !ctx.PersonIds.IsEmpty => ctx.DataFactory.PickRandom([.. ctx.PersonIds]),             // Person
            _ => Guid.NewGuid(),
        };
    }

    private static async Task AttachmentUploadAsync(PersonaContext ctx, CancellationToken ct)
    {
        var fileContent = ctx.DataFactory.RandomBytes(1024, 102400);
        var extIndex = ctx.DataFactory.RandomInt(0, FileExtensions.Length - 1);
        var fileName = $"attachment_{Guid.NewGuid():N}.{FileExtensions[extIndex]}";
        var contentType = ContentTypes[extIndex];
        var taskItemId = ctx.TaskItemIds.IsEmpty ? (Guid?)null : ctx.DataFactory.PickRandom([.. ctx.TaskItemIds]);
        var response = await ctx.AttachmentClient.UploadAsync(fileContent, fileName, contentType, taskItemId, null, null, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
        ctx.AttachmentIds.Add(response.Id);
        ctx.Coordinator.AddAttachmentId(response.Id);
    }

    private static async Task AttachmentDownloadAsync(PersonaContext ctx, CancellationToken ct)
    {
        var id = ctx.Coordinator.GetRandomAttachmentId();
        if (id is null)
        {
            return;
        }

        await ctx.AttachmentClient.DownloadAsync(id.Value, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }

    private static async Task AttachmentMetadataAsync(PersonaContext ctx, CancellationToken ct)
    {
        var id = ctx.Coordinator.GetRandomAttachmentId();
        if (id is null)
        {
            return;
        }

        await ctx.AttachmentClient.GetMetadataAsync(id.Value, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }

    private static async Task AttachmentDeleteAsync(PersonaContext ctx, CancellationToken ct)
    {
        if (ctx.AttachmentIds.IsEmpty)
        {
            return;
        }

        var id = ctx.DataFactory.PickRandom([.. ctx.AttachmentIds]);
        await ctx.AttachmentClient.DeleteAsync(id, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
        ctx.Coordinator.AddDeletedAttachmentId(id);
    }

    private static async Task UpdateDeviceNotificationPreferenceAsync(PersonaContext ctx, CancellationToken ct)
    {
        if (ctx.DeviceIds.IsEmpty)
        {
            return;
        }

        var deviceId = ctx.DataFactory.PickRandom([.. ctx.DeviceIds]);
        var dndEnabled = ctx.DataFactory.RandomBool(0.3f);
        var notifyEvents = ctx.DataFactory.RandomBool();
        var notifyTasks = ctx.DataFactory.RandomBool();
        var notifyLists = ctx.DataFactory.RandomBool();
        var notifyPeople = ctx.DataFactory.RandomBool(0.3f);
        await ctx.ApiClient.GraphQLAsync(
            "UpdateDeviceNotificationPreference",
            GraphQLOperations.UpdateDeviceNotificationPreference,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { deviceId, dndEnabled, notifyEvents, notifyTasks, notifyLists, notifyPeople } },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }

    private static async Task UpdateDeviceAsync(PersonaContext ctx, CancellationToken ct)
    {
        if (ctx.DeviceIds.IsEmpty)
        {
            return;
        }

        var id = ctx.DataFactory.PickRandom([.. ctx.DeviceIds]);
        var (deviceName, _, fcmToken) = ctx.DataFactory.GenerateDevice();
        await ctx.ApiClient.GraphQLAsync(
            "UpdateDevice",
            GraphQLOperations.UpdateDevice,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { id, deviceName, fcmToken } },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }

    private static async Task GetDevicesAsync(PersonaContext ctx, CancellationToken ct)
    {
        await ctx.ApiClient.GraphQLAsync(
            "GetDevices",
            GraphQLOperations.GetDevices,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["userId"] = ctx.UserId },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }

    private static async Task DeleteDeviceAsync(PersonaContext ctx, CancellationToken ct)
    {
        if (ctx.DeviceIds.IsEmpty)
        {
            return;
        }

        var id = ctx.DataFactory.PickRandom([.. ctx.DeviceIds]);
        await ctx.ApiClient.GraphQLAsync(
            "DeleteDevice",
            GraphQLOperations.DeleteDevice,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { id } },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }
}
