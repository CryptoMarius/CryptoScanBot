namespace CryptoScanner.Core.Messages;

/// <summary>
/// Shutdown has begun. The host cancels the actual window close and does its cleanup (saving
/// candles, closing the exchange connections) in the background, so without this the window simply
/// sat there unchanged for several seconds and looked frozen.
/// </summary>
public class ShutdownStartedMessage
{
}
