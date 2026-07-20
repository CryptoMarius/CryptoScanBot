using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.Analyzers.Nwe.Signal;

/// <summary>
/// Shared computation for the NWE × BB crossover signals.
/// Builds a time-ordered history of non-repainting NWE and BB values.
/// </summary>
public abstract class SignalNweBbBase : SignalCreateBase
{
    private const int Lookback = 60;

    protected readonly struct NweBbBar
    {
        public readonly CandleTime OpenTime;
        public readonly decimal Close;
        public readonly decimal NweUpper;
        public readonly decimal NweLower;
        public readonly decimal BbUpper;
        public readonly decimal BbLower;

        public NweBbBar(CandleTime openTime, decimal close,
            decimal nweUpper, decimal nweLower,
            decimal bbUpper, decimal bbLower)
        {
            OpenTime = openTime;
            Close = close;
            NweUpper = nweUpper;
            NweLower = nweLower;
            BbUpper = bbUpper;
            BbLower = bbLower;
        }
    }

    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
           || data.Candle.OpenTime == 0
           || data.CandleData == null
           || data.CandleData.Sma20 == null
           || data.CandleData.BollingerBandsDeviation == null
           )
            return false;

        return true;
    }

    /// <summary>
    /// Returns the last <see cref="Lookback"/> bars (oldest-first) with matched
    /// non-repainting NWE and BB values. Returns false when there is insufficient history.
    /// </summary>
    protected bool TryBuildHistory(out NweBbBar[] bars)
    {
        bars = [];

        // Repainting NWE computed on the full candle list (matches the chart NWE display)
        var nweIndicator = new NweIndicator(
            bandwidth: NwePlugin.Settings.BandWidth,
            multiplier: NwePlugin.Settings.Multiplication,
            smoothRepainting: true);
        var nweResults = nweIndicator.Calculate(SymbolInterval.CandleList);

        // Lookup by OpenTime, only bars that have valid upper/lower
        var nweByTime = nweResults
            .Where(r => r.Upper.HasValue && r.Lower.HasValue)
            .ToDictionary(r => r.OpenTime);

        // Walk back from the current candle collecting matched NWE + BB data
        var collected = new List<NweBbBar>(Lookback);
        MyData? cur = CandleLast;

        for (int i = 0; i < Lookback && cur != null; i++)
        {
            var cd = cur.CandleData;
            if (cd?.Sma20 != null && cd.BollingerBandsDeviation != null
                && nweByTime.TryGetValue(cur.Candle.OpenTime, out var nwe))
            {
                decimal bbUpper = (decimal)(cd.Sma20.Value + cd.BollingerBandsDeviation.Value);
                decimal bbLower = (decimal)(cd.Sma20.Value - cd.BollingerBandsDeviation.Value);
                collected.Add(new NweBbBar(
                    cur.Candle.OpenTime,
                    cur.Candle.Close,
                    nwe.Upper!.Value,
                    nwe.Lower!.Value,
                    bbUpper,
                    bbLower));
            }

            if (!GetPrevCandle(cur, out cur))
                break;
        }

        collected.Reverse(); // oldest first
        bars = [.. collected];
        return bars.Length >= 5;
    }
}
