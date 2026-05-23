using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using System.Text;
using MaxEnt.Web.Helpers;

namespace MaxEnt.Web.Pages;

/// <summary>
/// Shared base for all experiment pages. Handles run orchestration, log/CTS lifecycle,
/// navigation guard, timeout selection, and common helpers.
/// Subclasses implement <see cref="RunEpisodesAsync"/> and <see cref="SaveGraphsAsync"/>.
/// </summary>
public abstract class ExperimentPageBase : ComponentBase, IDisposable
{
    [Inject] protected IExperimentLogRepository LogRepo { get; set; } = default!;
    [Inject] protected IExperimentGraphRepository GraphRepo { get; set; } = default!;
    [Inject] protected IExperimentRunnerState RunnerState { get; set; } = default!;
    [Inject] protected NavigationManager Nav { get; set; } = default!;
    [Inject] protected IJSRuntime JS { get; set; } = default!;

    /// <summary>The source identifier used in log/graph records, e.g. "CartPole".</summary>
    protected abstract string Source { get; }

    public static readonly int[] TimeoutOptions = [2, 4, 8, 16, 32];

    // ── Run parameter option arrays ──────────────────────────────────────────
    public static readonly int[] EpisodeOptions =
        [100, 200, 300, 400, 500, 1_000, 2_000, 3_000, 4_000, 5_000, 10_000];

    public static readonly int[] MaxStepsOptions =
        [100, 200, 300, 400, 500, 1_000, 2_000];

    /// <summary>
    /// How often (in episodes) to yield to the UI during a run.
    /// Scales so that the UI updates roughly every ~10 seconds worth of work.
    /// </summary>
    protected static int YieldInterval(int totalEpisodes) =>
        totalEpisodes switch
        {
            <= 200  => 10,
            <= 500  => 20,
            <= 1000 => 50,
            <= 3000 => 100,
            _       => 200
        };

    protected bool isRunning;
    protected bool IsBlockedByOther => RunnerState.IsRunning && RunnerState.ActiveSource != Source;
    protected string outputLog = "";
    protected StringBuilder logBuilder = new();
    protected int timeoutHours = 2;
    protected DateTime experimentStartedAt = DateTime.MinValue;
    protected DateTime experimentFinishedAt = DateTime.MinValue;
    protected TimeSpan experimentElapsed = TimeSpan.Zero;

    private CancellationTokenSource? cts;
    private IDisposable? _navHandler;
    private Guid? _currentLogId;

    private static long _globalSequence;
    private const string CsvHeader = "ExperimentRunId,ExperimentRowId,Time,Ticks,Sequence,EpisodeCount,EpisodeTotal,Steps,Reward,Entropy";

    /// <summary>Column names for the live output display.</summary>
    protected static string OutputColumns => CsvHeader;

    // Browsers (Spectre mitigations) clamp performance.now() to 1 ms, so Stopwatch
    // inside WebAssembly has the same 1 ms resolution as DateTime.UtcNow.
    // We synthesise sub-millisecond uniqueness by tracking the last observed
    // millisecond and incrementing a within-ms counter each call.
    private static long _lastTimestampMs;
    private static int _withinMsCounter;

    private static string HighResTimestamp()
    {
        // Real-wall ms is all the browser gives us.
        long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        int subMs;
        lock (typeof(ExperimentPageBase))
        {
            if (nowMs != _lastTimestampMs)
            {
                _lastTimestampMs = nowMs;
                _withinMsCounter = 0;
            }
            subMs = _withinMsCounter++;
        }

        // Re-construct a DateTime that includes the within-ms counter in its
        // low-order ticks (1 tick = 100 ns; 10 ticks = 1 µs; 10 000 ticks = 1 ms).
        // subMs counts events within the same millisecond and is stored in ticks
        // 0-9 999 (the sub-ms part of the 7-digit fractional field).
        var baseDto = DateTimeOffset.FromUnixTimeMilliseconds(nowMs);
        var augmented = baseDto.UtcDateTime.AddTicks(subMs % 10_000);
        return augmented.ToString("o"); // yyyy-MM-ddTHH:mm:ss.fffffffZ
    }

    protected override void OnInitialized()
    {
        _navHandler = Nav.RegisterLocationChangingHandler(OnLocationChangingAsync);
    }

    private async ValueTask OnLocationChangingAsync(LocationChangingContext ctx)
    {
        if (!isRunning) return;
        var confirmed = await JS.InvokeAsync<bool>(
            "confirm",
            "Navigating away will stop this experiment and mark the log as incomplete. Continue?");
        if (confirmed)
            cts?.Cancel();
        else
            ctx.PreventNavigation();
    }

    public void Dispose() => _navHandler?.Dispose();

    protected async Task Run()
    {
        if (isRunning || IsBlockedByOther) return;
        isRunning = true;
        experimentStartedAt = DateTime.Now;
        experimentFinishedAt = DateTime.MinValue;
        experimentElapsed = TimeSpan.Zero;
        RunnerState.Start(Source);
        cts = new CancellationTokenSource(TimeSpan.FromHours(timeoutHours));
        _currentLogId = null;

        logBuilder.AppendLine(CsvHeader);
        outputLog = logBuilder.ToString();

        var runLog = ExperimentLog.Create(Source, "");
        _currentLogId = runLog.Id;
        await LogRepo.AddAsync(runLog);

        await AcquireWakeLockAsync();
        bool completedCleanly = false;
        try
        {
            await RunEpisodesAsync(cts.Token);
            completedCleanly = !cts.Token.IsCancellationRequested;
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            await ReleaseWakeLockAsync();
            if (_currentLogId.HasValue)
            {
                await LogRepo.UpdateContentAsync(_currentLogId.Value, logBuilder.ToString());
                if (completedCleanly)
                    await LogRepo.MarkCompletedAsync(_currentLogId.Value);
            }

            await SaveGraphsAsync();

            RunnerState.Stop();
            isRunning = false;
            experimentFinishedAt = DateTime.Now;
            experimentElapsed = experimentFinishedAt - experimentStartedAt;
            cts?.Dispose();
            cts = null;
            StateHasChanged();
        }
    }

    protected void Stop() => cts?.Cancel();

    private async Task AcquireWakeLockAsync()
    {
        try
        {
            var m = await JS.InvokeAsync<IJSObjectReference>("import", "./file-download.js");
            await m.InvokeVoidAsync("acquireWakeLock");
        }
        catch { /* wake lock not supported or blocked – ignore */ }
    }

    private async Task ReleaseWakeLockAsync()
    {
        try
        {
            var m = await JS.InvokeAsync<IJSObjectReference>("import", "./file-download.js");
            await m.InvokeVoidAsync("releaseWakeLock");
        }
        catch { }
    }

    /// <summary>Clears all run timing stats and output. Call from subclass Reset().</summary>
    protected void ClearRunStats()
    {
        logBuilder.Clear();
        outputLog = "";
        experimentStartedAt = DateTime.MinValue;
        experimentFinishedAt = DateTime.MinValue;
        experimentElapsed = TimeSpan.Zero;
    }

    /// <summary>Runs the experiment's episode loop. Implementors should respect the token.</summary>
    protected abstract Task RunEpisodesAsync(CancellationToken ct);

    /// <summary>Persists graphs produced by the last run.</summary>
    protected abstract Task SaveGraphsAsync();

    /// <summary>Discretises a continuous value into a bin index.</summary>
    protected static int StateBin(double value, double min, double max, int bins)
    {
        int b = (int)((value - min) / (max - min) * bins);
        return Math.Clamp(b, 0, bins - 1);
    }

    /// <summary>
    /// Appends a CSV row to the log. All string values are quoted to handle commas safely.
    /// </summary>
    protected void LogCsv(int episodeCount, int episodeTotal, int steps, double reward, double entropy)
    {
        long ticks = TicksHelper.NextTicks();
        long seq = System.Threading.Interlocked.Increment(ref _globalSequence);
        string time = HighResTimestamp();
        string id = Guid.NewGuid().ToString();
        string runId = (_currentLogId ?? Guid.Empty).ToString();
        logBuilder.AppendLine($"{runId},{id},{time},{ticks},{seq},{episodeCount},{episodeTotal},{steps},{reward},{entropy}");
        outputLog = logBuilder.ToString();
    }

    /// <summary>
    /// While running, shows only the last 5 log lines to keep the output area compact.
    /// After the run completes, returns the full log.
    /// </summary>
    protected string LiveLog
    {
        get
        {
            if (!isRunning) return outputLog;
            var lines = outputLog.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            return lines.Length <= 5
                ? outputLog
                : string.Join('\n', lines[^5..]);
        }
    }
}
