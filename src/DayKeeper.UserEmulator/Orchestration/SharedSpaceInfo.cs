using System.Collections.Concurrent;

namespace DayKeeper.UserEmulator.Orchestration;

public sealed record SharedSpaceInfo(string Name, Guid OwnerId, ConcurrentBag<Guid> MemberUserIds);
