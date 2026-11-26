using Binance.Net.Clients;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Exchange.Binance.Perpetual;

/// <summary>
/// The funding history of a USD-M perpetual (fapi/v1/fundingRate): at most 1000 settlements per call,
/// oldest first. The endpoint has a limit of its own of 500 calls per 5 minutes per IP address, shared
/// with fundingInfo, so the calls are spaced at least <see cref="MinimumSpacing"/> apart on top of the
/// general weight limiter.
/// </summary>
public class Funding : IFunding
{
    private const int PageSize = 1000;

    // 500 calls per 5 minutes is one call per 600 ms; a little slower leaves room for other traffic
    private static readonly TimeSpan MinimumSpacing = TimeSpan.FromMilliseconds(700);
    private static readonly SemaphoreSlim Spacing = new(1);
    private static DateTime LastCall = DateTime.MinValue;


    public async Task<List<CryptoFundingRate>> GetFundingRatesAsync(CryptoSymbol symbol, DateTime fromTime, DateTime toTime, CancellationToken cancellationToken)
    {
        List<CryptoFundingRate> result = [];
        using BinanceRestClient client = new();
        string prefix = $"{ExchangeBase.ExchangeOptions.ExchangeName} {symbol.Name} funding";

        DateTime start = fromTime.AddMilliseconds(1);
        while (start <= toTime && !cancellationToken.IsCancellationRequested)
        {
            await WaitForSpacingAsync(cancellationToken);
            LimitRate.WaitForFairWeight(1);

            var response = await client.UsdFuturesApi.ExchangeData.GetFundingRatesAsync(symbol.ExchangeName,
                startTime: start, endTime: toTime, limit: PageSize, ct: cancellationToken);
            if (!response.Success || response.Data == null)
            {
                GlobalData.AddTextToLogTab($"{prefix} error {response.Error}");
                break;
            }

            int count = 0;
            DateTime last = start;
            foreach (var item in response.Data.OrderBy(x => x.FundingTime))
            {
                count++;
                last = item.FundingTime;
                if (item.FundingTime <= fromTime || item.FundingTime > toTime)
                    continue;
                result.Add(new CryptoFundingRate(DateTime.SpecifyKind(item.FundingTime, DateTimeKind.Utc), item.FundingRate, item.MarkPrice ?? 0m));
            }

            if (count < PageSize)
                break;
            start = last.AddMilliseconds(1);
        }
        return result;
    }


    private static async Task WaitForSpacingAsync(CancellationToken cancellationToken)
    {
        await Spacing.WaitAsync(cancellationToken);
        try
        {
            TimeSpan wait = LastCall + MinimumSpacing - DateTime.UtcNow;
            if (wait > TimeSpan.Zero)
                await Task.Delay(wait, cancellationToken);
            LastCall = DateTime.UtcNow;
        }
        finally
        {
            Spacing.Release();
        }
    }
}
