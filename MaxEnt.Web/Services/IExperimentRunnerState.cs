namespace MaxEnt.Web.Services;

/// <summary>
/// Singleton that tracks whether an experiment is currently running and which one.
/// Enforces the single-experiment-at-a-time constraint across the app.
/// </summary>
public interface IExperimentRunnerState
{
    bool IsRunning { get; }

    /// <summary>"CartPole" | "AntMaxEnt" — null when not running.</summary>
    string? ActiveSource { get; }

    void Start(string source);
    void Stop();
}

public sealed class ExperimentRunnerState : IExperimentRunnerState
{
    public bool IsRunning { get; private set; }
    public string? ActiveSource { get; private set; }

    public void Start(string source)
    {
        IsRunning = true;
        ActiveSource = source;
    }

    public void Stop()
    {
        IsRunning = false;
        ActiveSource = null;
    }
}
