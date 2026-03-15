using System.Collections.Concurrent;
using DayKeeper.UserEmulator.Client;
using DayKeeper.UserEmulator.DataGeneration;
using DayKeeper.UserEmulator.Metrics;
using DayKeeper.UserEmulator.Orchestration;

namespace DayKeeper.UserEmulator.Personas;

public sealed class PersonaContext
{
    public required Guid UserId { get; init; }
    public required Guid PersonalSpaceId { get; init; }
    public required string DisplayName { get; init; }
    public required SharedStateCoordinator Coordinator { get; init; }
    public required DayKeeperApiClient ApiClient { get; init; }
    public required SyncClient SyncClient { get; init; }
    public required AttachmentClient AttachmentClient { get; init; }
    public required FakeDataFactory DataFactory { get; init; }
    public required MetricsCollector Metrics { get; init; }
    public required string PersonaName { get; init; }
    public required string ArchetypeName { get; init; }
    public required IList<Guid> SharedSpaceIds { get; init; }
    public required bool IsSoloUser { get; init; }

    public ConcurrentBag<Guid> ProjectIds { get; } = [];
    public ConcurrentBag<Guid> TaskItemIds { get; } = [];
    public ConcurrentBag<Guid> CalendarIds { get; } = [];
    public ConcurrentBag<Guid> CalendarEventIds { get; } = [];
    public ConcurrentBag<Guid> ShoppingListIds { get; } = [];
    public ConcurrentBag<Guid> ListItemIds { get; } = [];
    public ConcurrentBag<Guid> PersonIds { get; } = [];
    public ConcurrentBag<Guid> ContactMethodIds { get; } = [];
    public ConcurrentBag<Guid> AddressIds { get; } = [];
    public ConcurrentBag<Guid> DeviceIds { get; } = [];
    public ConcurrentBag<Guid> AttachmentIds { get; } = [];

    public Guid GetWorkingSpaceId()
    {
        if (IsSoloUser || SharedSpaceIds.Count == 0 || DataFactory.RandomBool(0.6f))
        {
            return PersonalSpaceId;
        }

        return DataFactory.PickRandom(SharedSpaceIds);
    }

    public bool IsWorkingInSharedSpace(Guid spaceId) => SharedSpaceIds.Contains(spaceId);
}
