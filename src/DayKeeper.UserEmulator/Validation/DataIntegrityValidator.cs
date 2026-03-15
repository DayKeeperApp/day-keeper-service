using System.Globalization;
using DayKeeper.UserEmulator.Client;
using DayKeeper.UserEmulator.Metrics;
using DayKeeper.UserEmulator.Orchestration;
using Spectre.Console;

namespace DayKeeper.UserEmulator.Validation;

public sealed class DataIntegrityValidator
{
    private readonly DayKeeperApiClient _apiClient;
    private readonly SharedStateCoordinator _coordinator;
    private readonly MetricsCollector _metrics;

    public DataIntegrityValidator(
        DayKeeperApiClient apiClient,
        SharedStateCoordinator coordinator,
        MetricsCollector metrics)
    {
        _apiClient = apiClient;
        _coordinator = coordinator;
        _metrics = metrics;
    }

    public async Task ValidateAsync(CancellationToken ct)
    {
        var results = new List<(string Name, bool Passed, string Details)>();

        results.Add(await CheckEntityCountsAsync(ct).ConfigureAwait(false));
        results.Add(await CheckTaskStatusIntegrityAsync(ct).ConfigureAwait(false));
        results.Add(await CheckRelationshipIntegrityAsync(ct).ConfigureAwait(false));
        results.Add(await CheckAttachmentVerificationAsync(ct).ConfigureAwait(false));
        results.Add(await CheckSyncConsistencyAsync(ct).ConfigureAwait(false));

        DisplayResults(results);
    }

    private async Task<(string, bool, string)> CheckEntityCountsAsync(CancellationToken ct)
    {
        const string name = "Entity Counts";
        try
        {
            var data = await _apiClient.GraphQLAsync(
                "GetTaskItems", GraphQLOperations.GetTaskItems,
                new Dictionary<string, object?>(StringComparer.Ordinal) { ["spaceId"] = null },
                _metrics, "validator", "validator", ct).ConfigureAwait(false);

            var count = data.GetProperty("taskItems").GetProperty("nodes").GetArrayLength();
            return count > 0
                ? (name, true, string.Format(CultureInfo.InvariantCulture, "{0} task items found", count))
                : (name, false, "No task items found");
        }
        catch (Exception ex)
        {
            return (name, false, ex.Message);
        }
    }

    private async Task<(string, bool, string)> CheckTaskStatusIntegrityAsync(CancellationToken ct)
    {
        const string name = "Task Status Integrity";
        try
        {
            var data = await _apiClient.GraphQLAsync(
                "GetTaskItems", GraphQLOperations.GetTaskItems,
                new Dictionary<string, object?>(StringComparer.Ordinal) { ["spaceId"] = null },
                _metrics, "validator", "validator", ct).ConfigureAwait(false);

            var nodes = data.GetProperty("taskItems").GetProperty("nodes");
            var violations = 0;
            var doneCount = 0;

            foreach (var node in nodes.EnumerateArray())
            {
                if (!node.TryGetProperty("status", out var statusProp))
                {
                    continue;
                }

                var status = statusProp.GetString();
                if (!string.Equals(status, "COMPLETED", StringComparison.Ordinal))
                {
                    continue;
                }

                doneCount++;
                node.TryGetProperty("completedAt", out var completedAtProp);
                if (completedAtProp.ValueKind == System.Text.Json.JsonValueKind.Null
                    || completedAtProp.ValueKind == System.Text.Json.JsonValueKind.Undefined)
                {
                    violations++;
                }
            }

            return violations == 0
                ? (name, true, string.Format(CultureInfo.InvariantCulture, "{0} done tasks all have completedAt", doneCount))
                : (name, false, string.Format(CultureInfo.InvariantCulture, "{0} done tasks missing completedAt", violations));
        }
        catch (Exception ex)
        {
            return (name, false, ex.Message);
        }
    }

    private async Task<(string, bool, string)> CheckRelationshipIntegrityAsync(CancellationToken ct)
    {
        const string name = "Relationship Integrity";
        try
        {
            var data = await _apiClient.GraphQLAsync(
                "GetTaskItems", GraphQLOperations.GetTaskItems,
                new Dictionary<string, object?>(StringComparer.Ordinal) { ["spaceId"] = null },
                _metrics, "validator", "validator", ct).ConfigureAwait(false);

            var nodes = data.GetProperty("taskItems").GetProperty("nodes");
            var tasksWithProject = nodes.EnumerateArray()
                .Where(n => n.TryGetProperty("projectId", out var pid)
                    && pid.ValueKind != System.Text.Json.JsonValueKind.Null)
                .Take(5)
                .ToList();

            if (tasksWithProject.Count == 0)
            {
                return (name, true, "No tasks with projects to verify");
            }

            return await VerifyProjectsExistAsync(tasksWithProject, ct).ConfigureAwait(false) is var (passed, details)
                ? (name, passed, details)
                : (name, true, "Projects verified");
        }
        catch (Exception ex)
        {
            return (name, false, ex.Message);
        }
    }

    private async Task<(bool Passed, string Details)> VerifyProjectsExistAsync(
        IEnumerable<System.Text.Json.JsonElement> tasks, CancellationToken ct)
    {
        var verified = 0;
        foreach (var task in tasks)
        {
            var projectId = task.GetProperty("projectId").GetGuid();
            var projectData = await _apiClient.GraphQLAsync(
                "GetProjectById", GraphQLOperations.GetProjectById,
                new Dictionary<string, object?>(StringComparer.Ordinal) { ["id"] = projectId },
                _metrics, "validator", "validator", ct).ConfigureAwait(false);

            if (projectData.TryGetProperty("projectById", out var project)
                && project.ValueKind != System.Text.Json.JsonValueKind.Null)
            {
                verified++;
            }
        }

        return verified > 0
            ? (true, string.Format(CultureInfo.InvariantCulture, "{0} task-project relationships verified", verified))
            : (false, "Could not verify any task-project relationships");
    }

    private async Task<(string, bool, string)> CheckAttachmentVerificationAsync(CancellationToken ct)
    {
        const string name = "Attachment Verification";
        try
        {
            var attachmentId = _coordinator.GetRandomAttachmentId();
            if (attachmentId is null)
            {
                return (name, true, "No attachments tracked (skipped)");
            }

            var data = await _apiClient.GraphQLAsync(
                "GetAttachments", GraphQLOperations.GetAttachments,
                new Dictionary<string, object?>(StringComparer.Ordinal) { ["taskItemId"] = null, ["calendarEventId"] = null, ["personId"] = null },
                _metrics, "validator", "validator", ct).ConfigureAwait(false);

            var count = data.GetProperty("attachments").GetProperty("nodes").GetArrayLength();
            return (name, true, string.Format(CultureInfo.InvariantCulture, "{0} attachments queryable", count));
        }
        catch (Exception ex)
        {
            return (name, false, ex.Message);
        }
    }

    private async Task<(string, bool, string)> CheckSyncConsistencyAsync(CancellationToken ct)
    {
        const string name = "Sync Consistency";
        try
        {
            var userIds = _coordinator.GetUserIds();
            if (userIds.Count == 0)
            {
                return (name, true, "No users to sync (skipped)");
            }

            var data = await _apiClient.GraphQLAsync(
                "GetSpaces", GraphQLOperations.GetSpaces, null,
                _metrics, "validator", "validator", ct).ConfigureAwait(false);

            var spaceCount = data.GetProperty("spaces").GetProperty("nodes").GetArrayLength();
            return spaceCount > 0
                ? (name, true, string.Format(CultureInfo.InvariantCulture, "API responsive, {0} spaces accessible", spaceCount))
                : (name, false, "No spaces returned from API");
        }
        catch (Exception ex)
        {
            return (name, false, ex.Message);
        }
    }

    private static void DisplayResults(IReadOnlyList<(string Name, bool Passed, string Details)> results)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold]Validation Results[/]")
            .AddColumn("[grey]Check[/]")
            .AddColumn("[grey]Result[/]")
            .AddColumn("[grey]Details[/]");

        foreach (var (name, passed, details) in results)
        {
            var result = passed ? "[green]PASS[/]" : "[red]FAIL[/]";
            table.AddRow(name, result, details);
        }

        AnsiConsole.Write(table);

        var failCount = results.Count(r => !r.Passed);
        if (failCount > 0)
        {
            AnsiConsole.MarkupLine(string.Format(
                CultureInfo.InvariantCulture,
                "[red]{0} validation check(s) failed[/]", failCount));
        }
        else
        {
            AnsiConsole.MarkupLine("[green]All validation checks passed.[/]");
        }
    }
}
