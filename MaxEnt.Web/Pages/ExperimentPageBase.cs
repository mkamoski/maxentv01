using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using System.Text;

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

    protected bool isRunning;
    protected bool IsBlockedByOther => RunnerState.IsRunning && RunnerState.ActiveSource != Source;
    protected string outputLog = "";
    protected StringBuilder logBuilder = new();
    protected int timeoutHours = 2;

    private CancellationTokenSource? cts;
    private IDisposable? _navHandler;
    private Guid? _currentLogId;

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
        RunnerState.Start(Source);
        cts = new CancellationTokenSource(TimeSpan.FromHours(timeoutHours));
        _currentLogId = null;

        var runLog = ExperimentLog.Create(Source, "");
        _currentLogId = runLog.Id;
        await LogRepo.AddAsync(runLog);

        bool completedCleanly = false;
        try
        {
            await RunEpisodesAsync(cts.Token);
            completedCleanly = !cts.Token.IsCancellationRequested;
            Log(completedCleanly ? "Training complete." : "Stopped.");
        }
        catch (OperationCanceledException)
        {
            Log("Stopped.");
        }
        finally
        {
            if (_currentLogId.HasValue)
            {
                await LogRepo.UpdateContentAsync(_currentLogId.Value, logBuilder.ToString());
                if (completedCleanly)
                    await LogRepo.MarkCompletedAsync(_currentLogId.Value);
            }

            await SaveGraphsAsync();

            RunnerState.Stop();
            isRunning = false;
            cts?.Dispose();
            cts = null;
            StateHasChanged();
        }
    }

    protected void Stop() => cts?.Cancel();

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

    protected void Log(string msg)
    {
        logBuilder.AppendLine(msg);
        outputLog = logBuilder.ToString();
    }
}
