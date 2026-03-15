using System.Collections.Concurrent;

namespace DayKeeper.UserEmulator.Orchestration;

public sealed class SharedStateCoordinator
{
    private readonly ConcurrentBag<Guid> _userIds = [];
    private readonly ConcurrentDictionary<Guid, SharedSpaceInfo> _sharedSpaces = new();
    private readonly ConcurrentBag<Guid> _sharedTaskItemIds = [];
    private readonly ConcurrentBag<Guid> _sharedCalendarEventIds = [];
    private readonly ConcurrentBag<Guid> _sharedShoppingListIds = [];
    private readonly ConcurrentBag<Guid> _sharedListItemIds = [];
    private readonly ConcurrentBag<Guid> _sharedPersonIds = [];
    private readonly ConcurrentBag<Guid> _attachmentIds = [];
    private readonly ConcurrentBag<Guid> _deletedAttachmentIds = [];
    private readonly ConcurrentBag<Guid> _categoryIds = [];
    private readonly ConcurrentDictionary<Guid, long> _syncCursors = new();

    public Guid TenantId { get; set; }

    public void AddUserId(Guid id) => _userIds.Add(id);

    public IReadOnlyList<Guid> GetUserIds() => [.. _userIds];

    public void AddSharedSpace(Guid spaceId, string name, Guid ownerId) =>
        _sharedSpaces[spaceId] = new SharedSpaceInfo(name, ownerId, []);

    public IReadOnlyDictionary<Guid, SharedSpaceInfo> GetSharedSpaces() => _sharedSpaces;

    public void AddSharedTaskItemId(Guid id) => _sharedTaskItemIds.Add(id);

    public IReadOnlyList<Guid> GetAllSharedTaskItemIds() => [.. _sharedTaskItemIds];

    public Guid? GetRandomSharedTaskItemId() => GetRandomFromBag(_sharedTaskItemIds);

    public void AddSharedCalendarEventId(Guid id) => _sharedCalendarEventIds.Add(id);

    public IReadOnlyList<Guid> GetAllSharedCalendarEventIds() => [.. _sharedCalendarEventIds];

    public Guid? GetRandomSharedCalendarEventId() => GetRandomFromBag(_sharedCalendarEventIds);

    public void AddSharedShoppingListId(Guid id) => _sharedShoppingListIds.Add(id);

    public IReadOnlyList<Guid> GetAllSharedShoppingListIds() => [.. _sharedShoppingListIds];

    public Guid? GetRandomSharedShoppingListId() => GetRandomFromBag(_sharedShoppingListIds);

    public void AddSharedListItemId(Guid id) => _sharedListItemIds.Add(id);

    public IReadOnlyList<Guid> GetAllSharedListItemIds() => [.. _sharedListItemIds];

    public Guid? GetRandomSharedListItemId() => GetRandomFromBag(_sharedListItemIds);

    public void AddSharedPersonId(Guid id) => _sharedPersonIds.Add(id);

    public IReadOnlyList<Guid> GetAllSharedPersonIds() => [.. _sharedPersonIds];

    public Guid? GetRandomSharedPersonId() => GetRandomFromBag(_sharedPersonIds);

    public void AddAttachmentId(Guid id) => _attachmentIds.Add(id);

    public IReadOnlyList<Guid> GetAllAttachmentIds() => [.. _attachmentIds];

    public Guid? GetRandomAttachmentId() => GetRandomFromBag(_attachmentIds);

    public void AddDeletedAttachmentId(Guid id) => _deletedAttachmentIds.Add(id);

    public IReadOnlyList<Guid> GetAllDeletedAttachmentIds() => [.. _deletedAttachmentIds];

    public Guid? GetRandomDeletedAttachmentId() => GetRandomFromBag(_deletedAttachmentIds);

    public void AddCategoryId(Guid id) => _categoryIds.Add(id);

    public IReadOnlyList<Guid> GetAllCategoryIds() => [.. _categoryIds];

    public Guid? GetRandomCategoryId() => GetRandomFromBag(_categoryIds);

    public void UpdateSyncCursor(Guid userId, long cursor) => _syncCursors[userId] = cursor;

    public long GetSyncCursor(Guid userId) => _syncCursors.GetValueOrDefault(userId, 0);

    private static Guid? GetRandomFromBag(ConcurrentBag<Guid> bag)
    {
        var snapshot = bag.ToArray();
        if (snapshot.Length == 0)
        {
            return null;
        }

        return snapshot[Random.Shared.Next(snapshot.Length)];
    }
}
