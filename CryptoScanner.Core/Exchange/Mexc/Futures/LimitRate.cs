using CryptoScanner.Core.Core;

namespace CryptoScanner.Core.Exchange.Mexc.Futures;

internal class MexcWeight
{
    public long Time { get; set; }
    public long Weight { get; set; }

}
/*
- are there limits for Mexc Futures? Yes, they are called rate limits
  https://mexcdevelop.github.io/apidocs/contract_v1_en/
  The futures side counts requests per endpoint, not weight per endpoint like the spot side does
*/

/// <summary>
/// Delays the caller when too many requests are made.
///
/// The futures documentation states a limit per endpoint, expressed in calls instead of weight:
///   GET api/v1/contract/kline/{symbol}   20 times / 2 seconds  (Candle.GetCandlesForInterval)
///   GET api/v1/contract/ticker           20 times / 2 seconds  (Symbol.GetSymbolsAsync)
///   GET api/v1/contract/detail            1 time  / 5 seconds  (Symbol.GetSymbolsAsync)
///
/// Fetching candles is the only thing that happens in bulk, so the window follows that endpoint:
/// 2 seconds, and 15 of the 20 allowed calls to leave room for the requests that are already on
/// their way. The contract list is asked for once per symbol refresh, far apart enough for its own
/// much stricter limit, so it books its calls in the same counter.
/// </summary>
public static class LimitRate
{
    public static long CurrentWeight { get; set; }
    static private List<MexcWeight> List { get; } = new List<MexcWeight>();

    public static void WaitForFairWeight(long newWeight)
    {

        Monitor.Enter(List);
        try
        {
            // Remove the registrations older than the measuring window
            while (true)
            {
                // Current time.
                DateTimeOffset dateTimeOffset = DateTime.UtcNow;
                long unix = dateTimeOffset.ToUnixTimeSeconds();

                // A moment 2 seconds ago (the window Mexc measures over)
                long removeBeforeDate = unix - 2;

                while (List.Count > 0)
                {
                    MexcWeight item = List[0];
                    if (item.Time <= removeBeforeDate)
                    {
                        CurrentWeight -= item.Weight;
                        List.RemoveAt(0);
                    }
                    else break;
                }

                if (CurrentWeight > 15)
                {
                    GlobalData.AddTextToLogTab($"{ExchangeBase.ExchangeOptions.ExchangeName} delay needed for weight: {CurrentWeight} (rate limits)");
                    // Release the lock while waiting. Sleeping inside Monitor.Enter(List) queues
                    // every other fetch thread behind this one, after which they each sleep in
                    // turn instead of re-testing the (by then lowered) weight.
                    Monitor.Exit(List);
                    try
                    {
                        Thread.Sleep(2500);
                    }
                    finally
                    {
                        Monitor.Enter(List);
                    }
                }
                else
                {
                    CurrentWeight += newWeight;

                    // And add a new registration
                    MexcWeight item = new();
                    DateTimeOffset dateTimeOffset2 = DateTime.UtcNow;
                    item.Time = dateTimeOffset2.ToUnixTimeSeconds();
                    item.Weight = newWeight;
                    List.Add(item);

                    break;
                }

            }
        }
        finally
        {
            Monitor.Exit(List);
        }
    }
}
