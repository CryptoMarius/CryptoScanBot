namespace CryptoScanner.Core.Core;

/// <summary>
/// Abstraction over wall-clock time. Production code uses <see cref="SystemClock"/> which
/// delegates to <see cref="DateTime.UtcNow"/>. The emulator swaps in <see cref="EmulatorClock"/>
/// and advances it manually as candles are replayed, making signal/position lifecycle
/// timestamps deterministic and reproducible.
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
}

/// <summary>
/// Default implementation — delegates to the operating system clock.
/// </summary>
public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

/// <summary>
/// Manually-advanced clock used by the emulator. The TickRunner sets <see cref="UtcNow"/>
/// to the close-time of the candle currently being replayed before invoking the analysis
/// pipeline.
/// </summary>
public sealed class EmulatorClock : IClock
{
    // The replay-wide time, advanced per base candle by the TickRunner.
    private DateTime baseTime;

    // Per-flow override, used while the replay descends to minute resolution inside one base
    // candle. AsyncLocal rather than a plain field because every symbol runs its own descent:
    // with RunParallel on they are on different minutes at the same moment, and a shared field
    // would let one symbol's minute leak into another's order handling.
    private static readonly AsyncLocal<DateTime?> flowTime = new();

    public DateTime UtcNow
    {
        get => flowTime.Value ?? baseTime;
        set => baseTime = value;
    }

    /// <summary>
    /// Overrides the clock for the current async flow until the returned scope is disposed.
    /// A no-op for any other <see cref="IClock"/> implementation, so calling it outside an
    /// emulator run is harmless.
    /// </summary>
    public static FlowScope Scoped(DateTime value) => new(value);

    public readonly struct FlowScope : IDisposable
    {
        private readonly DateTime? previous;

        internal FlowScope(DateTime value)
        {
            previous = flowTime.Value;
            flowTime.Value = value;
        }

        public void Dispose() => flowTime.Value = previous;
    }
}
