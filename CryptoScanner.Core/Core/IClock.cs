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
    public DateTime UtcNow { get; set; }
}
