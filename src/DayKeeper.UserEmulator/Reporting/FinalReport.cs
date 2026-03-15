using System.Globalization;
using DayKeeper.UserEmulator.Configuration;
using DayKeeper.UserEmulator.Metrics;
using Humanizer;
using Spectre.Console;

namespace DayKeeper.UserEmulator.Reporting;

public sealed class FinalReport
{
    private readonly MetricsCollector _metrics;
    private readonly ProfileConfig _config;
    private readonly TimeSpan _totalDuration;
    private readonly TimeSpan _seedDuration;
    private readonly int _seedEntityCount;

    public FinalReport(
        MetricsCollector metrics,
        ProfileConfig config,
        TimeSpan totalDuration,
        TimeSpan seedDuration,
        int seedEntityCount)
    {
        _metrics = metrics;
        _config = config;
        _totalDuration = totalDuration;
        _seedDuration = seedDuration;
        _seedEntityCount = seedEntityCount;
    }

    public void Display()
    {
        AnsiConsole.WriteLine();
        DisplaySummaryPanel();
        DisplayLatencyTable();
        DisplayEndpointBreakdown();
        DisplayEntityChart();
        DisplayErrorSummary();
    }

    private void DisplaySummaryPanel()
    {
        var total = _metrics.TotalRequests;
        var errors = _metrics.TotalErrors;
        var successRate = total == 0 ? 100.0 : (total - errors) / (double)total * 100.0;
        var rps = _metrics.GetRequestsPerSecond();

        var content = BuildSummaryContent(total, successRate, rps);
        var panel = new Panel(content)
            .Border(BoxBorder.Rounded)
            .Header("[bold cyan] Run Summary [/]");

        AnsiConsole.Write(panel);
    }

    private string BuildSummaryContent(long total, double successRate, double rps)
    {
        var graphPct = GetApiSplitPercent(_metrics.GraphQLRequests, total);
        var syncPct = GetApiSplitPercent(_metrics.RestSyncRequests, total);
        var attachPct = GetApiSplitPercent(_metrics.RestAttachmentRequests, total);

        return string.Format(
            CultureInfo.InvariantCulture,
            "Profile: [cyan]{0}[/]  Duration: [cyan]{1}[/]  Users: [cyan]{2}[/]\n"
            + "Total Requests: [white]{3:N0}[/]  Success: [green]{4:F1}%[/]  Avg RPS: [white]{5:F1}[/]\n"
            + "API Split: [cyan]{6:F0}%[/] GraphQL / [yellow]{7:F0}%[/] REST Sync / [grey]{8:F0}%[/] REST Attach\n"
            + "Seed: [grey]{9:N0} entities in {10:F1}s[/]",
            _config.DurationMinutes,
            _totalDuration.Humanize(),
            _config.TotalUsers,
            total,
            successRate,
            rps,
            graphPct,
            syncPct,
            attachPct,
            _seedEntityCount,
            _seedDuration.TotalSeconds);
    }

    private void DisplayLatencyTable()
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold]Latency Percentiles[/]")
            .AddColumn("[grey]Percentile[/]")
            .AddColumn("[grey]Latency (ms)[/]");

        AddLatencyRow(table, "p50", 50);
        AddLatencyRow(table, "p75", 75);
        AddLatencyRow(table, "p90", 90);
        AddLatencyRow(table, "p95", 95);
        AddLatencyRow(table, "p99", 99);
        AddLatencyRow(table, "p99.9", 99.9);

        AnsiConsole.Write(table);
    }

    private void AddLatencyRow(Table table, string label, double percentile)
    {
        var ms = _metrics.GetLatencyPercentile(percentile);
        var color = ms < 100 ? "green" : ms < 500 ? "yellow" : "red";
        table.AddRow(label, string.Format(CultureInfo.InvariantCulture, "[{0}]{1} ms[/]", color, ms));
    }

    private void DisplayEndpointBreakdown()
    {
        var records = _metrics.GetRecords();
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold]Endpoint Breakdown[/]")
            .AddColumn("[grey]Category[/]")
            .AddColumn("[grey]Count[/]")
            .AddColumn("[grey]Avg Latency[/]");

        AddEndpointRow(table, "GraphQL Mutations",
            records.Where(r => string.Equals(r.Method, "GraphQL", StringComparison.Ordinal)
                && r.Endpoint.StartsWith("Create", StringComparison.OrdinalIgnoreCase)));

        AddEndpointRow(table, "GraphQL Queries",
            records.Where(r => string.Equals(r.Method, "GraphQL", StringComparison.Ordinal)
                && r.Endpoint.StartsWith("Get", StringComparison.OrdinalIgnoreCase)));

        AddEndpointRow(table, "REST Sync",
            records.Where(r => string.Equals(r.Method, "REST", StringComparison.Ordinal)
                && r.Endpoint.StartsWith("sync/", StringComparison.OrdinalIgnoreCase)));

        AddEndpointRow(table, "REST Attachments",
            records.Where(r => string.Equals(r.Method, "REST", StringComparison.Ordinal)
                && r.Endpoint.StartsWith("attachments/", StringComparison.OrdinalIgnoreCase)));

        AnsiConsole.Write(table);
    }

    private static void AddEndpointRow(Table table, string label, IEnumerable<RequestRecord> records)
    {
        var list = records.ToList();
        var count = list.Count;
        var avgLatency = count == 0 ? 0 : (long)list.Average(r => r.LatencyMs);
        table.AddRow(
            label,
            count.ToString("N0", CultureInfo.InvariantCulture),
            string.Format(CultureInfo.InvariantCulture, "{0} ms", avgLatency));
    }

    private void DisplayEntityChart()
    {
        var records = _metrics.GetRecords();
        var createCounts = records
            .Where(r => r.Endpoint.StartsWith("Create", StringComparison.OrdinalIgnoreCase)
                && !r.IsError)
            .GroupBy(r => r.Endpoint, StringComparer.OrdinalIgnoreCase)
            .Select(g => (g.Key, g.Count()))
            .OrderByDescending(x => x.Item2)
            .Take(10)
            .ToList();

        if (createCounts.Count == 0)
        {
            return;
        }

        var chart = new BarChart()
            .Width(60)
            .Label("[bold]Entity Creation Counts[/]");

        foreach (var (endpoint, count) in createCounts)
        {
            chart.AddItem(endpoint, count, Color.Cyan1);
        }

        AnsiConsole.Write(chart);
    }

    private void DisplayErrorSummary()
    {
        var errors = _metrics.GetRecords().Where(r => r.IsError).ToList();
        if (errors.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]No errors recorded.[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold red]Error Summary[/]")
            .AddColumn("[grey]Status Code[/]")
            .AddColumn("[grey]Count[/]")
            .AddColumn("[grey]Sample Endpoint[/]");

        var grouped = errors
            .GroupBy(r => r.StatusCode)
            .OrderByDescending(g => g.Count());

        foreach (var group in grouped)
        {
            table.AddRow(
                group.Key.ToString(CultureInfo.InvariantCulture),
                group.Count().ToString("N0", CultureInfo.InvariantCulture),
                group.First().Endpoint);
        }

        AnsiConsole.Write(table);
    }

    private static double GetApiSplitPercent(long part, long total) =>
        total == 0 ? 0.0 : part / (double)total * 100.0;
}
