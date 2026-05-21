namespace MaxEnt.Web.Helpers;

/// <summary>
/// Provides monotonically unique <see cref="DateTime.Ticks"/> values.
/// A brief spin ensures each call returns a value strictly greater than the last,
/// even when called faster than the system clock advances.
/// </summary>
public static class TicksHelper
{
    private static long _lastTicks;

    /// <summary>
    /// Returns a unique <c>DateTime.Now.Ticks</c> value.
    /// Spins with a 0 ms <see cref="Task.Delay"/> yield between checks so that
    /// the caller never blocks a real thread but each returned value is unique.
    /// </summary>
    public static long NextTicks()
    {
        // A tiny spin loop; on fast hardware we almost never loop more than once.
        while (true)
        {
            long candidate = DateTime.Now.Ticks;
            long prev = System.Threading.Interlocked.CompareExchange(ref _lastTicks, candidate, 0);

            // Atomically set if candidate > _lastTicks
            long snapshot;
            do
            {
                snapshot = System.Threading.Volatile.Read(ref _lastTicks);
                if (candidate <= snapshot)
                {
                    // Clock hasn't advanced; nudge the candidate one tick past the last seen value.
                    candidate = snapshot + 1;
                }
            }
            while (System.Threading.Interlocked.CompareExchange(ref _lastTicks, candidate, snapshot) != snapshot);

            return candidate;
        }
    }
}
