using CryptoScanner.Analyzers.Baba;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;

using System.Text.Json;

namespace CryptoScanner.CoreTests.Signal.Baba;

/// <summary>
/// Parity tests for the scanner's BABA band construction (<see cref="BabaBandsHelper.ComputeBands"/>)
/// against the bands served by the TradingBuddy backend (scanner3.tradingbuddy.io
/// /api/v1/baba/bands). The fixtures under Signal/Baba/Fixtures hold, per symbol, the exact
/// candles (/api/v1/candles) and the exact bands TradingBuddy returned for the same timestamps,
/// pre-joined on timestamp.
///
/// What was reverse-engineered from that data (BTC/ETH/SOL/RUNE/XRP/DOGE, 1h, 500 bars each):
///   basis  = VWMA(hlc3, 50)                                  -> matches to machine precision
///   band   = basis +/- 2.5 * vwStdev(hlc3, 50)               -> population volume-weighted stdev
///   vwStdev = sqrt(E_w[hlc3^2] - E_w[hlc3]^2)
/// The median of (halfBand / vwStdev_population) is exactly 2.5000 on every symbol, which pins the
/// multiplier to 2.5 and the stdev to the POPULATION form (not sample) — exactly the scanner default
/// (Length=50, Mult=2.5, AtrMult=0). A minority of bars on volatile days are up to ~16% wider on the
/// server side (a backend implementation detail we cannot reproduce from outside), so the band
/// assertions are on the robust MEDIAN error, not the max.
/// </summary>
[TestClass]
public class BabaBandsParityTests
{
    private const string Exchange = "TestExchange";

    // Relative tolerance for "bit-for-bit" agreement on a single band value (allows only
    // floating-point/tick rounding — 1e-6 is ~sub-cent on a 60000 price).
    private const double BitExactTol = 1e-6;

    // Symbols that have a fixture under Signal/Baba/Fixtures.
    private static readonly string[] Symbols =
        ["BTCUSDT", "ETHUSDT", "SOLUSDT", "RUNEUSDT", "XRPUSDT", "DOGEUSDT"];

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        GlobalData.Settings ??= new SettingsBasic();
        // Use the shipped defaults (Length=50, Mult=2.5, AtrLength=14, AtrMult=0.0 = pure VWAP bands).
        // CreateSettings() replaces the plugin's static Settings with a fresh BabaSettings.
        BabaPlugin.CreateSettings();
    }

    // ── Fixture model ────────────────────────────────────────────────────

    private sealed record Fixture(
        string Symbol, string Timeframe, string Exchange, string Source,
        int TickDecimals, int CandleCount, int BandCount, Row[] Rows);

    private sealed record Row(
        long T, decimal O, decimal H, decimal L, decimal C, decimal V,
        double? Basis, double? Upper, double? Lower);

    private static Fixture LoadFixture(string symbol)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Signal", "Baba", "Fixtures", $"{symbol}-1h.json");
        Assert.IsTrue(File.Exists(path), $"Fixture not found: {path}");
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<Fixture>(File.ReadAllText(path), opts)!;
    }

    private static List<CryptoCandle> BuildCandles(Fixture fx)
    {
        var list = new List<CryptoCandle>(fx.Rows.Length);
        byte tickDec = (byte)fx.TickDecimals;
        for (int i = 0; i < fx.Rows.Length; i++)
        {
            Row r = fx.Rows[i];
            // Exchange candles are already tick-aligned, so storing at the symbol's own decimals is
            // lossless. OpenTime only needs to be strictly increasing for the indicator engine; the
            // fixture is already sorted ascending, so an hourly step is enough.
            list.Add(new CryptoCandle
            {
                TickDecimals = tickDec,
                OpenTime = new CandleTime((uint)((i + 1) * 60)),
                Open = r.O,
                High = r.H,
                Low = r.L,
                Close = r.C,
                Volume = r.V,
            });
        }
        return list;
    }

    private static double Median(List<double> values)
    {
        if (values.Count == 0)
            return 0;
        values.Sort();
        int mid = values.Count / 2;
        return values.Count % 2 == 1 ? values[mid] : (values[mid - 1] + values[mid]) / 2;
    }

    // ── The main parity test, one run per symbol ─────────────────────────

    [TestMethod]
    [DataRow("BTCUSDT")]
    [DataRow("ETHUSDT")]
    [DataRow("SOLUSDT")]
    [DataRow("RUNEUSDT")]
    [DataRow("XRPUSDT")]
    [DataRow("DOGEUSDT")]
    public void ScannerBands_MatchTradingBuddy(string symbol)
    {
        Fixture fx = LoadFixture(symbol);
        List<CryptoCandle> candles = BuildCandles(fx);

        BabaBandsHelper.BandValue[] bands = BabaBandsHelper.ComputeBands(candles);
        Assert.AreEqual(candles.Count, bands.Length, "Result must be index-aligned with the candles.");

        double basisMaxRel = 0;
        var upperRel = new List<double>();
        var lowerRel = new List<double>();
        int within1pct = 0, compared = 0;

        for (int i = 0; i < fx.Rows.Length; i++)
        {
            Row r = fx.Rows[i];
            if (r.Basis is not double eBasis || r.Upper is not double eUpper || r.Lower is not double eLower)
                continue; // warm-up bar without a served band
            if (!bands[i].HasValue)
                continue;

            basisMaxRel = Math.Max(basisMaxRel, Math.Abs(bands[i].Basis - eBasis) / eBasis);

            double uRel = Math.Abs(bands[i].Upper - eUpper) / eUpper;
            double lRel = Math.Abs(bands[i].Lower - eLower) / eLower;
            upperRel.Add(uRel);
            lowerRel.Add(lRel);

            // Band half-width agreement (symmetric), the number that actually matters for a break.
            double eHalf = eUpper - eBasis;
            double cHalf = bands[i].Upper - bands[i].Basis;
            if (Math.Abs(cHalf - eHalf) / eHalf < 0.01)
                within1pct++;
            compared++;
        }

        Assert.IsTrue(compared > 300, $"{symbol}: too few comparable bars ({compared}).");

        double medUpper = Median(upperRel);
        double medLower = Median(lowerRel);
        double maxUpper = upperRel.Count > 0 ? upperRel.Max() : 0;
        double bitExactBars = 100.0 * upperRel.Count(e => e < BitExactTol) / upperRel.Count;
        double pctWithin1 = 100.0 * within1pct / compared;

        TestContext.WriteLine($"{symbol}: bars={compared}  basisMaxRelErr={basisMaxRel:E2}  " +
            $"bandBitExact={bitExactBars:F1}%  medianBandErr={medUpper * 100:F3}%  " +
            $"maxBandErr={maxUpper * 100:F2}%  halfWidthWithin1%={pctWithin1:F1}%");

        // The midline (VWMA(hlc3,50)) must reproduce to machine precision — this part IS bit-for-bit.
        Assert.IsTrue(basisMaxRel < 1e-4,
            $"{symbol}: basis (VWMA hlc3 50) should match TradingBuddy to ~machine precision, was {basisMaxRel:E2}.");

        // BIT-FOR-BIT goal: every band must match TradingBuddy, not just the median.
        // This currently FAILS on a minority of volatile-day bars whose server-side widening
        // could not be reverse-engineered from the candle+volume data (see class summary).
        // The assertion is intentionally strict so the suite reports the real gap instead of hiding it.
        Assert.IsTrue(maxUpper < BitExactTol,
            $"{symbol}: bands are NOT bit-for-bit. Only {bitExactBars:F1}% of bars match exactly; " +
            $"worst deviation {maxUpper * 100:F2}%. The scanner reproduces the midline exactly and " +
            $"~{bitExactBars:F0}% of the band values, but the volatile-bar tail is unresolved.");
    }

    // ── Formula sanity tests (independent of tolerance choices) ───────────

    [TestMethod]
    public void Basis_EqualsVolumeWeightedVwmaOfHlc3()
    {
        Fixture fx = LoadFixture("BTCUSDT");
        List<CryptoCandle> candles = BuildCandles(fx);
        int len = BabaPlugin.Settings.Length;

        BabaBandsHelper.BandValue[] bands = BabaBandsHelper.ComputeBands(candles);

        // Independent reference VWMA(hlc3, len) computed straight from the candles.
        int chec1 = 0;
        for (int i = len - 1; i < candles.Count; i++)
        {
            if (!bands[i].HasValue)
                continue;
            double sumPV = 0, sumV = 0;
            for (int k = i - len + 1; k <= i; k++)
            {
                double hlc3 = (double)(candles[k].High + candles[k].Low + candles[k].Close) / 3.0;
                double vol = (double)candles[k].Volume;
                sumPV += hlc3 * vol;
                sumV += vol;
            }
            double refVwma = sumPV / sumV;
            Assert.AreEqual(refVwma, bands[i].Basis, Math.Abs(refVwma) * 1e-6 + 1e-6,
                $"Basis at {i} must equal VWMA(hlc3,{len}).");
            chec1++;
        }
        Assert.IsTrue(chec1 > 400, "Expected many verified bars.");
    }

    [TestMethod]
    public void Bands_AreSymmetricAroundBasis_AndUseMultTimesVwStdev()
    {
        Fixture fx = LoadFixture("SOLUSDT");
        List<CryptoCandle> candles = BuildCandles(fx);
        double mult = BabaPlugin.Settings.Mult;

        BabaBandsHelper.BandValue[] bands = BabaBandsHelper.ComputeBands(candles);

        foreach (BabaBandsHelper.BandValue b in bands)
        {
            if (!b.HasValue)
                continue;
            double up = b.Upper - b.Basis;
            double dn = b.Basis - b.Lower;
            // The pad is exactly Mult * vwStdev and the band is symmetric.
            Assert.AreEqual(up, dn, Math.Abs(up) * 1e-9 + 1e-9, "Band must be symmetric around basis.");
            Assert.AreEqual(mult * b.VwStdev, up, Math.Abs(up) * 1e-9 + 1e-9,
                "Half-width must equal Mult * vwStdev.");
        }
    }

    [TestMethod]
    public void DefaultSettings_ArePureVwapBands()
    {
        var s = new BabaSettings();
        Assert.AreEqual(50, s.Length);
        Assert.AreEqual(2.5, s.Mult, 1e-12);
    }
}
