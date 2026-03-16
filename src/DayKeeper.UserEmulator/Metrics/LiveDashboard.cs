using System.Globalization;
using Humanizer;
using Spectre.Console;

namespace DayKeeper.UserEmulator.Metrics;

public sealed class LiveDashboard : IDisposable
{
    private readonly MetricsCollector _metrics;
    private readonly IReadOnlyList<(string DisplayName, string PersonaName, string ArchetypeName)> _userInfo;
    private CancellationTokenSource? _cts;
    private Task? _renderTask;

    public LiveDashboard(
        MetricsCollector metrics,
        IReadOnlyList<(string DisplayName, string PersonaName, string ArchetypeName)> userInfo)
    {
        _metrics = metrics;
        _userInfo = userInfo;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _renderTask = Task.Run(() => RenderLoopAsync(_cts.Token));
    }

    public async Task StopAsync()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync().ConfigureAwait(false);
            if (_renderTask is not null)
            {
                try
                {
                    await _renderTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
            }
        }
    }

    public void Dispose()
    {
        _cts?.Dispose();
    }

    private async Task RenderLoopAsync(CancellationToken ct)
    {
        await AnsiConsole.Live(BuildTable())
            .AutoClear(false)
            .StartAsync(async liveCtx =>
            {
                while (!ct.IsCancellationRequested)
                {
                    liveCtx.UpdateTarget(BuildTable());

                    try
                    {
                        await Task.Delay(1000, ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }).ConfigureAwait(false);
    }

    private Table BuildTable()
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold cyan]DayKeeper UserEmulator - Live Stats[/]")
            .AddColumn(new TableColumn("[grey]Metric[/]").Width(20))
            .AddColumn(new TableColumn("[white]Value[/]").Width(20));

        AddCountRows(table);
        AddRpsRow(table);
        AddLatencyRows(table);
        AddApiSplitRows(table);

        return table;
    }

    private void AddCountRows(Table table)
    {
        var total = _metrics.TotalRequests;
        var errors = _metrics.TotalErrors;
        var successRate = total == 0 ? 100.0 : (total - errors) / (double)total * 100.0;

        table.AddRow("Total Requests", ((int)Math.Min(total, int.MaxValue)).ToMetric(decimals: 1));
        table.AddRow("Errors", string.Format(
            CultureInfo.InvariantCulture,
            "[{0}]{1}[/]",
            errors > 0 ? "red" : "grey",
            ((int)Math.Min(errors, int.MaxValue)).ToMetric(decimals: 1)));
        table.AddRow("Success Rate", string.Format(CultureInfo.InvariantCulture, "{0:F1}%", successRate));
        table.AddRow("Users", _userInfo.Count.ToString(CultureInfo.InvariantCulture));
    }

    private void AddRpsRow(Table table)
    {
        var rps = _metrics.GetRequestsPerSecond();
        table.AddRow("RPS", string.Format(CultureInfo.InvariantCulture, "{0:F1} req/s", rps));
    }

    private void AddLatencyRows(Table table)
    {
        table.AddRow("[grey]---[/]", "[grey]Latency[/]");
        table.AddRow("p50", FormatLatency(_metrics.GetLatencyPercentile(50)));
        table.AddRow("p95", FormatLatency(_metrics.GetLatencyPercentile(95)));
        table.AddRow("p99", FormatLatency(_metrics.GetLatencyPercentile(99)));
    }

    private void AddApiSplitRows(Table table)
    {
        var total = _metrics.TotalRequests;
        if (total == 0)
        {
            return;
        }

        table.AddRow("[grey]---[/]", "[grey]API Split[/]");
        table.AddRow("GraphQL", FormatPercent(_metrics.GraphQLRequests, total));
        table.AddRow("REST Sync", FormatPercent(_metrics.RestSyncRequests, total));
        table.AddRow("REST Attach", FormatPercent(_metrics.RestAttachmentRequests, total));
    }

    private static string FormatLatency(long ms) =>
        string.Format(CultureInfo.InvariantCulture, "{0} ms", ms);

    private static string FormatPercent(long part, long total) =>
        string.Format(CultureInfo.InvariantCulture, "{0:F1}%", part / (double)total * 100.0);
}
