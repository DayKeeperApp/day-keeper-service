using System.ComponentModel;
using Spectre.Console.Cli;

namespace DayKeeper.UserEmulator.Configuration;

public sealed class EmulatorSettings : CommandSettings
{
    [CommandOption("--url <URL>")]
    [Description("API base URL")]
    [DefaultValue("https://localhost:5101")]
    public string Url { get; init; } = "https://localhost:5101";

    [CommandOption("--profile <PROFILE>")]
    [Description("Load profile: Light, Medium, or Heavy")]
    [DefaultValue(LoadProfile.Medium)]
    public LoadProfile Profile { get; init; } = LoadProfile.Medium;

    [CommandOption("--duration <MINUTES>")]
    [Description("Run duration in minutes (overrides profile default)")]
    public int? Duration { get; init; }

    [CommandOption("--users <COUNT>")]
    [Description("Override user count")]
    public int? Users { get; init; }

    [CommandOption("--seed <SEED>")]
    [Description("Random seed for reproducible runs")]
    public int? Seed { get; init; }

    [CommandOption("--no-validate")]
    [Description("Skip post-run validation phase")]
    [DefaultValue(false)]
    public bool NoValidate { get; init; }

    [CommandOption("--no-cleanup")]
    [Description("Keep emulator tenant data after run")]
    [DefaultValue(false)]
    public bool NoCleanup { get; init; }

    [CommandOption("--no-seed")]
    [Description("Skip seeding phase")]
    [DefaultValue(false)]
    public bool NoSeed { get; init; }

    [CommandOption("--verbose")]
    [Description("Log individual requests")]
    [DefaultValue(false)]
    public bool Verbose { get; init; }

    public ProfileConfig GetEffectiveConfig()
    {
        var baseConfig = LoadProfilePresets.Get(Profile);

        if (Duration is null && Users is null)
        {
            return baseConfig;
        }

        var totalUsers = Users ?? baseConfig.TotalUsers;
        var soloUsers = (int)Math.Ceiling(totalUsers * (baseConfig.SoloUsers / (double)baseConfig.TotalUsers));
        var groupUsers = totalUsers - soloUsers;

        return baseConfig with
        {
            TotalUsers = totalUsers,
            SoloUsers = soloUsers,
            GroupUsers = groupUsers,
            DurationMinutes = Duration ?? baseConfig.DurationMinutes,
        };
    }
}
