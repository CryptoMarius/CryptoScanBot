namespace CryptoScanner.Core.Messages;

/// <summary>
/// Another figure or interval was picked for the dashboard barometer graph. The chart listens so its
/// barometer line (see Barometer.BarometerChartOverlay) follows the dashboard at once instead of at
/// the next redraw.
/// </summary>
public class BarometerGraphChangedMessage
{
}
