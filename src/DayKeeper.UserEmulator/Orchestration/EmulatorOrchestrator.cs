using System.Diagnostics;
using System.Globalization;
using DayKeeper.UserEmulator.Client;
using DayKeeper.UserEmulator.Configuration;
using DayKeeper.UserEmulator.DataGeneration;
using DayKeeper.UserEmulator.Metrics;
using DayKeeper.UserEmulator.Personas;
using DayKeeper.UserEmulator.Reporting;
using DayKeeper.UserEmulator.Validation;
using Humanizer;
using Spectre.Console;

namespace DayKeeper.UserEmulator.Orchestration;

public sealed class EmulatorOrchestrator
{
    private static readonly string[] PersonaNames =
    [
        "TaskManager", "CalendarPowerUser", "ListMaker",
        "ContactManager", "Collaborator", "MobileSyncer",
    ];

    private static readonly (BehaviorArchetype Archetype, double Weight)[] ArchetypeWeights =
    [
        (BehaviorArchetype.Browser, 0.25),
        (BehaviorArchetype.ActivePlanner, 0.30),
        (BehaviorArchetype.RapidMutator, 0.20),
        (BehaviorArchetype.BulkCreator, 0.10),
        (BehaviorArchetype.SpikeUser, 0.15),
    ];

    private readonly EmulatorSettings _settings;
    private readonly ProfileConfig _config;
    private readonly MetricsCollector _metrics;
    private readonly SharedStateCoordinator _coordinator;
    private readonly FakeDataFactory _dataFactory;
    private readonly List<(IPersona Persona, PersonaContext Context, BehaviorArchetype Archetype)> _users = [];

    private TimeSpan _seedDuration = TimeSpan.Zero;
    private int _seedEntityCount;

    public EmulatorOrchestrator(EmulatorSettings settings)
    {
        _settings = settings;
        _config = settings.GetEffectiveConfig();
        _metrics = new MetricsCollector();
        _coordinator = new SharedStateCoordinator();
        _dataFactory = new FakeDataFactory(settings.Seed);
    }

    public async Task<int> RunAsync(CancellationToken ct)
    {
        DisplayBanner();
        DisplayConfig();

        await SetupAsync(ct).ConfigureAwait(false);

        if (!_settings.NoSeed)
        {
            await SeedAsync(ct).ConfigureAwait(false);
        }

        await RunMainPhaseAsync(ct).ConfigureAwait(false);

        if (!_settings.NoValidate)
        {
            await ValidateAsync(ct).ConfigureAwait(false);
        }

        DisplayFinalReport();

        return 0;
    }

    private static void DisplayBanner()
    {
        AnsiConsole.Write(new FigletText("DayKeeper").Color(Color.Cyan1));
        AnsiConsole.Write(new FigletText("UserEmulator").Color(Color.Green));
    }

    private void DisplayConfig()
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[grey]Setting[/]")
            .AddColumn("[white]Value[/]");

        table.AddRow("Profile", _settings.Profile.ToString());
        table.AddRow("Duration", string.Format(CultureInfo.InvariantCulture, "{0} min", _config.DurationMinutes));
        table.AddRow("Users", string.Format(CultureInfo.InvariantCulture, "{0} solo + {1} group", _config.SoloUsers, _config.GroupUsers));
        table.AddRow("Shared Spaces", _config.SharedSpaceCount.ToString(CultureInfo.InvariantCulture));
        table.AddRow("API URL", _settings.Url);

        if (_settings.Seed.HasValue)
        {
            table.AddRow("Seed", _settings.Seed.Value.ToString(CultureInfo.InvariantCulture));
        }

        AnsiConsole.Write(table);
    }

    private async Task SetupAsync(CancellationToken ct)
    {
        AnsiConsole.MarkupLine("[cyan]Setting up emulator environment...[/]");

        var tenantId = await CreateTenantAsync(ct).ConfigureAwait(false);
        _coordinator.TenantId = tenantId;

        var userRecords = await CreateUsersAsync(tenantId, ct).ConfigureAwait(false);
        var personalSpaces = await CreatePersonalSpacesAsync(tenantId, userRecords, ct).ConfigureAwait(false);
        var sharedSpaceIds = await CreateSharedSpacesAsync(tenantId, userRecords, ct).ConfigureAwait(false);

        BuildUserEntries(tenantId, userRecords, personalSpaces, sharedSpaceIds);
        DisplaySetupSummary(sharedSpaceIds);
    }

    private async Task<Guid> CreateTenantAsync(CancellationToken ct)
    {
        var setupClient = new DayKeeperApiClient(_settings.Url, Guid.NewGuid());
        var tenantName = string.Format(CultureInfo.InvariantCulture, "emulator-{0:N}", Guid.NewGuid());
        var variables = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["input"] = new { name = tenantName, slug = tenantName },
        };

        var result = await setupClient.GraphQLAsync(
            "CreateTenant", GraphQLOperations.CreateTenant, variables,
            _metrics, "setup", "setup", ct).ConfigureAwait(false);

        return result.GetProperty("createTenant").GetProperty("tenant").GetProperty("id").GetGuid();
    }

    private async Task<IReadOnlyList<(Guid UserId, string DisplayName, string Email)>> CreateUsersAsync(
        Guid tenantId, CancellationToken ct)
    {
        var setupClient = new DayKeeperApiClient(_settings.Url, tenantId);
        var results = new List<(Guid, string, string)>();

        for (var i = 0; i < _config.TotalUsers; i++)
        {
            const int maxRetries = 3;
            for (var attempt = 0; attempt < maxRetries; attempt++)
            {
                var (displayName, email, timezone, weekStart, locale) = _dataFactory.GenerateUser();
                var variables = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["input"] = new { tenantId, displayName, email, timezone, weekStart, locale },
                };

                try
                {
                    var result = await setupClient.GraphQLAsync(
                        "CreateUser", GraphQLOperations.CreateUser, variables,
                        _metrics, "setup", "setup", ct).ConfigureAwait(false);

                    var userId = result.GetProperty("createUser").GetProperty("user").GetProperty("id").GetGuid();
                    _coordinator.AddUserId(userId);
                    results.Add((userId, displayName, email));
                    break;
                }
                catch (GraphQLException) when (attempt < maxRetries - 1)
                {
                    // Likely duplicate email — retry with a new generated user
                }
            }
        }

        return results;
    }

    private async Task<IReadOnlyList<Guid>> CreatePersonalSpacesAsync(
        Guid tenantId,
        IReadOnlyList<(Guid UserId, string DisplayName, string Email)> userRecords,
        CancellationToken ct)
    {
        var setupClient = new DayKeeperApiClient(_settings.Url, tenantId);
        var spaceIds = new List<Guid>();

        foreach (var (userId, displayName, _) in userRecords)
        {
            var variables = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["input"] = new { tenantId, name = string.Format(CultureInfo.InvariantCulture, "{0}'s Space", displayName), spaceType = "PERSONAL", createdByUserId = userId },
            };

            var result = await setupClient.GraphQLAsync(
                "CreateSpace", GraphQLOperations.CreateSpace, variables,
                _metrics, "setup", "setup", ct).ConfigureAwait(false);

            spaceIds.Add(result.GetProperty("createSpace").GetProperty("space").GetProperty("id").GetGuid());
        }

        return spaceIds;
    }

    private async Task<IReadOnlyList<Guid>> CreateSharedSpacesAsync(
        Guid tenantId,
        IReadOnlyList<(Guid UserId, string DisplayName, string Email)> userRecords,
        CancellationToken ct)
    {
        var setupClient = new DayKeeperApiClient(_settings.Url, tenantId);
        var sharedSpaceIds = new List<Guid>();

        var usedNames = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < _config.SharedSpaceCount; i++)
        {
            var ownerId = userRecords[i % userRecords.Count].UserId;
            string spaceName;
            do
            {
                spaceName = _dataFactory.GenerateSpaceName();
            } while (!usedNames.Add(spaceName));

            var spaceId = await CreateOneSharedSpaceAsync(setupClient, tenantId, spaceName, ownerId, ct).ConfigureAwait(false);

            _coordinator.AddSharedSpace(spaceId, spaceName, ownerId);
            sharedSpaceIds.Add(spaceId);

            await AddMembersToSharedSpaceAsync(setupClient, spaceId, ownerId, userRecords, ct).ConfigureAwait(false);
        }

        return sharedSpaceIds;
    }

    private async Task<Guid> CreateOneSharedSpaceAsync(
        DayKeeperApiClient client, Guid tenantId, string name, Guid ownerId, CancellationToken ct)
    {
        var variables = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["input"] = new { tenantId, name, spaceType = "SHARED", createdByUserId = ownerId },
        };

        var result = await client.GraphQLAsync(
            "CreateSpace", GraphQLOperations.CreateSpace, variables,
            _metrics, "setup", "setup", ct).ConfigureAwait(false);

        return result.GetProperty("createSpace").GetProperty("space").GetProperty("id").GetGuid();
    }

    private async Task AddMembersToSharedSpaceAsync(
        DayKeeperApiClient client,
        Guid spaceId,
        Guid ownerId,
        IReadOnlyList<(Guid UserId, string DisplayName, string Email)> userRecords,
        CancellationToken ct)
    {
        var memberCount = _dataFactory.RandomInt(_config.MinMembersPerSpace, _config.MaxMembersPerSpace);
        var candidates = userRecords.Where(u => u.UserId != ownerId).ToList();
        var selected = candidates.Take(Math.Min(memberCount, candidates.Count)).ToList();

        foreach (var (userId, _, _) in selected)
        {
            try
            {
                var role = _dataFactory.RandomBool(0.3f) ? "EDITOR" : "VIEWER";
                var variables = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["input"] = new { spaceId, userId, role },
                };

                await client.GraphQLAsync(
                    "AddSpaceMember", GraphQLOperations.AddSpaceMember, variables,
                    _metrics, "setup", "setup", ct).ConfigureAwait(false);

                _coordinator.GetSharedSpaces()[spaceId].MemberUserIds.Add(userId);
            }
            catch (Exception ex) when (ex is GraphQLException or HttpRequestException or InvalidOperationException)
            {
                // Duplicate membership or other non-fatal error — skip this member
            }
        }
    }

    private void BuildUserEntries(
        Guid tenantId,
        IReadOnlyList<(Guid UserId, string DisplayName, string Email)> userRecords,
        IReadOnlyList<Guid> personalSpaces,
        IReadOnlyList<Guid> sharedSpaceIds)
    {
        var personaTypes = CreatePersonaInstances();
        var userSharedSpaces = BuildUserSharedSpaceMapping(userRecords, sharedSpaceIds);

        for (var i = 0; i < userRecords.Count; i++)
        {
            var (userId, displayName, _) = userRecords[i];
            var isSolo = i < _config.SoloUsers;
            var persona = AssignPersona(personaTypes, i);
            var archetype = AssignArchetype(i);
            var jitter = CreateJitterPolicy(archetype);
            var userSpaces = isSolo ? new List<Guid>() : userSharedSpaces[userId];

            var apiClient = new DayKeeperApiClient(_settings.Url, tenantId);
            var syncHttpClient = CreateHttpClient(_settings.Url);
            syncHttpClient.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId.ToString());
            syncHttpClient.DefaultRequestHeaders.Add("X-User-Id", userId.ToString());
            var syncClient = new SyncClient(syncHttpClient, _metrics);

            var attachHttpClient = CreateHttpClient(_settings.Url);
            attachHttpClient.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId.ToString());
            attachHttpClient.DefaultRequestHeaders.Add("X-User-Id", userId.ToString());
            var attachmentClient = new AttachmentClient(attachHttpClient, _metrics);

            var context = new PersonaContext
            {
                UserId = userId,
                PersonalSpaceId = personalSpaces[i],
                DisplayName = displayName,
                Coordinator = _coordinator,
                ApiClient = apiClient,
                SyncClient = syncClient,
                AttachmentClient = attachmentClient,
                DataFactory = new FakeDataFactory(_settings.Seed.HasValue ? _settings.Seed.Value + i : null),
                Metrics = _metrics,
                PersonaName = persona.Name,
                ArchetypeName = archetype.ToString(),
                SharedSpaceIds = userSpaces,
                IsSoloUser = isSolo,
            };

            _users.Add((persona, context, archetype));
        }
    }

    private static IPersona[] CreatePersonaInstances() =>
    [
        new Personas.TaskManagerPersona(),
        new Personas.CalendarPowerUserPersona(),
        new Personas.ListMakerPersona(),
        new Personas.ContactManagerPersona(),
        new Personas.CollaboratorPersona(),
        new Personas.MobileSyncerPersona(),
    ];

    private static IPersona AssignPersona(IPersona[] personaTypes, int userIndex) =>
        personaTypes[userIndex % personaTypes.Length];

    private BehaviorArchetype AssignArchetype(int userIndex)
    {
        if (userIndex < ArchetypeWeights.Length)
        {
            return ArchetypeWeights[userIndex].Archetype;
        }

        var roll = _dataFactory.RandomInt(0, 99) / 100.0;
        var cumulative = 0.0;
        foreach (var (archetype, weight) in ArchetypeWeights)
        {
            cumulative += weight;
            if (roll < cumulative)
            {
                return archetype;
            }
        }

        return BehaviorArchetype.ActivePlanner;
    }

    private JitterPolicy CreateJitterPolicy(BehaviorArchetype archetype) => archetype switch
    {
        BehaviorArchetype.Browser => new JitterPolicy(_config.BrowserIntervalMinMs, _config.BrowserIntervalMaxMs, _config.BurstChance),
        BehaviorArchetype.ActivePlanner => new JitterPolicy(_config.ActivePlannerIntervalMinMs, _config.ActivePlannerIntervalMaxMs, _config.BurstChance),
        BehaviorArchetype.RapidMutator => new JitterPolicy(_config.RapidMutatorIntervalMinMs, _config.RapidMutatorIntervalMaxMs, _config.BurstChance),
        BehaviorArchetype.BulkCreator => new JitterPolicy(_config.BulkCreatorIntervalMinMs, _config.BulkCreatorIntervalMaxMs, _config.BurstChance),
        BehaviorArchetype.SpikeUser => new JitterPolicy(0, 0, 0),
        _ => new JitterPolicy(_config.ActivePlannerIntervalMinMs, _config.ActivePlannerIntervalMaxMs, _config.BurstChance),
    };

    private Dictionary<Guid, List<Guid>> BuildUserSharedSpaceMapping(
        IReadOnlyList<(Guid UserId, string DisplayName, string Email)> userRecords,
        IReadOnlyList<Guid> sharedSpaceIds)
    {
        var mapping = userRecords.ToDictionary(u => u.UserId, _ => new List<Guid>());
        var sharedSpaces = _coordinator.GetSharedSpaces();

        foreach (var (spaceId, info) in sharedSpaces)
        {
            mapping[info.OwnerId].Add(spaceId);
            foreach (var memberId in info.MemberUserIds)
            {
                if (mapping.TryGetValue(memberId, out var memberSpaces))
                {
                    memberSpaces.Add(spaceId);
                }
            }
        }

        return mapping;
    }

    private void DisplaySetupSummary(IReadOnlyList<Guid> sharedSpaceIds)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[grey]Component[/]")
            .AddColumn("[green]Count[/]");

        table.AddRow("Users created", _config.TotalUsers.ToString(CultureInfo.InvariantCulture));
        table.AddRow("Personal spaces", _config.TotalUsers.ToString(CultureInfo.InvariantCulture));
        table.AddRow("Shared spaces", sharedSpaceIds.Count.ToString(CultureInfo.InvariantCulture));

        AnsiConsole.MarkupLine("[green]Setup complete.[/]");
        AnsiConsole.Write(table);
    }

    private async Task SeedAsync(CancellationToken ct)
    {
        AnsiConsole.MarkupLine("[cyan]Seeding data...[/]");
        var stopwatch = Stopwatch.StartNew();

        var userPairs = _users.Select(u => (u.Persona, u.Context)).ToList();
        await DataSeeder.SeedAsync(userPairs, ct).ConfigureAwait(false);

        stopwatch.Stop();
        _seedDuration = stopwatch.Elapsed;

        _seedEntityCount = _users
            .Sum(u => u.Context.ProjectIds.Count
                + u.Context.TaskItemIds.Count
                + u.Context.CalendarIds.Count
                + u.Context.CalendarEventIds.Count
                + u.Context.ShoppingListIds.Count
                + u.Context.ListItemIds.Count
                + u.Context.PersonIds.Count);

        AnsiConsole.MarkupLine(string.Format(
            CultureInfo.InvariantCulture,
            "[green]Seed complete: {0} entities in {1:F1}s[/]",
            _seedEntityCount,
            _seedDuration.TotalSeconds));
    }

    private async Task RunMainPhaseAsync(CancellationToken ct)
    {
        AnsiConsole.MarkupLine(string.Format(
            CultureInfo.InvariantCulture,
            "[cyan]Starting main phase ({0} minutes)...[/]",
            _config.DurationMinutes));

        using var phaseCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        phaseCts.CancelAfter(TimeSpan.FromMinutes(_config.DurationMinutes));

        var userInfo = _users
            .Select(u => (u.Context.DisplayName, u.Persona.Name, u.Archetype.ToString()))
            .ToList();

        var dashboard = new LiveDashboard(_metrics, userInfo);
        dashboard.Start();

        var simulators = _users.Select(u => new UserSimulator(u.Persona, u.Context, CreateJitterPolicy(u.Archetype), u.Archetype, _config));
        var runTasks = simulators.Select(s => s.RunAsync(phaseCts.Token)).ToList();

        try
        {
            await Task.WhenAll(runTasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { /* expected on timeout */ }

        await dashboard.StopAsync().ConfigureAwait(false);
        AnsiConsole.MarkupLine("[green]Main phase complete.[/]");
    }

    private async Task ValidateAsync(CancellationToken ct)
    {
        AnsiConsole.MarkupLine("[cyan]Running validation...[/]");

        if (_users.Count == 0)
        {
            return;
        }

        var firstUser = _users[0];
        var validator = new DataIntegrityValidator(firstUser.Context.ApiClient, _coordinator, _metrics);
        await validator.ValidateAsync(ct).ConfigureAwait(false);
    }

    private void DisplayFinalReport()
    {
        var totalDuration = TimeSpan.FromMinutes(_config.DurationMinutes);
        var report = new FinalReport(_metrics, _config, totalDuration, _seedDuration, _seedEntityCount);
        report.Display();
    }

    private static HttpClient CreateHttpClient(string baseUrl)
    {
        var handler = new HttpClientHandler();

        if (baseUrl.StartsWith("https://localhost", StringComparison.OrdinalIgnoreCase)
            || baseUrl.StartsWith("https://127.0.0.1", StringComparison.OrdinalIgnoreCase))
        {
#pragma warning disable MA0039 // Local dev tool — accept dev certs for localhost only
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
#pragma warning restore MA0039
        }

        return new HttpClient(handler)
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
        };
    }
}
