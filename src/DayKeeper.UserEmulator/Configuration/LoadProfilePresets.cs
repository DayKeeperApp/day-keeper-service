namespace DayKeeper.UserEmulator.Configuration;

public static class LoadProfilePresets
{
    public static ProfileConfig Get(LoadProfile profile) => profile switch
    {
        LoadProfile.Light => Light,
        LoadProfile.Medium => Medium,
        LoadProfile.Heavy => Heavy,
        _ => Medium,
    };

    public static readonly ProfileConfig Light = new(
        TotalUsers: 5,
        SoloUsers: 2,
        GroupUsers: 3,
        SharedSpaceCount: 1,
        MinMembersPerSpace: 2,
        MaxMembersPerSpace: 3,
        DurationMinutes: 5,
        BrowserIntervalMinMs: 2000,
        BrowserIntervalMaxMs: 3000,
        ActivePlannerIntervalMinMs: 3000,
        ActivePlannerIntervalMaxMs: 5000,
        RapidMutatorIntervalMinMs: 500,
        RapidMutatorIntervalMaxMs: 1000,
        BulkCreatorIntervalMinMs: 8000,
        BulkCreatorIntervalMaxMs: 12000,
        SpikeDormantMinMs: 45000,
        SpikeDormantMaxMs: 90000,
        SpikeBurstMinSize: 10,
        SpikeBurstMaxSize: 15,
        BurstChance: 0.03,
        ScenarioIntervalSeconds: 40,
        MaxConcurrentScenarios: 1);

    public static readonly ProfileConfig Medium = new(
        TotalUsers: 15,
        SoloUsers: 5,
        GroupUsers: 10,
        SharedSpaceCount: 3,
        MinMembersPerSpace: 3,
        MaxMembersPerSpace: 5,
        DurationMinutes: 7,
        BrowserIntervalMinMs: 2000,
        BrowserIntervalMaxMs: 3000,
        ActivePlannerIntervalMinMs: 2000,
        ActivePlannerIntervalMaxMs: 5000,
        RapidMutatorIntervalMinMs: 300,
        RapidMutatorIntervalMaxMs: 800,
        BulkCreatorIntervalMinMs: 5000,
        BulkCreatorIntervalMaxMs: 10000,
        SpikeDormantMinMs: 30000,
        SpikeDormantMaxMs: 60000,
        SpikeBurstMinSize: 20,
        SpikeBurstMaxSize: 30,
        BurstChance: 0.05,
        ScenarioIntervalSeconds: 25,
        MaxConcurrentScenarios: 2);

    public static readonly ProfileConfig Heavy = new(
        TotalUsers: 30,
        SoloUsers: 10,
        GroupUsers: 20,
        SharedSpaceCount: 6,
        MinMembersPerSpace: 4,
        MaxMembersPerSpace: 8,
        DurationMinutes: 10,
        BrowserIntervalMinMs: 1000,
        BrowserIntervalMaxMs: 2000,
        ActivePlannerIntervalMinMs: 1000,
        ActivePlannerIntervalMaxMs: 3000,
        RapidMutatorIntervalMinMs: 100,
        RapidMutatorIntervalMaxMs: 500,
        BulkCreatorIntervalMinMs: 3000,
        BulkCreatorIntervalMaxMs: 8000,
        SpikeDormantMinMs: 15000,
        SpikeDormantMaxMs: 30000,
        SpikeBurstMinSize: 30,
        SpikeBurstMaxSize: 50,
        BurstChance: 0.10,
        ScenarioIntervalSeconds: 15,
        MaxConcurrentScenarios: 4);
}
