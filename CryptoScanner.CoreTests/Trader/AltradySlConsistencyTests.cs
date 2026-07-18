using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Trader;

using static CryptoScanner.Core.Trader.StopLossCalculator;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// Verifies that both the papertrading <see cref="StopLossCalculator"/> and the Altrady
/// webhook produce consistent SL/DCA/TP placement.
///
/// Key invariant: when a strategy provides a signal SL percentage, DCAs beyond that SL
/// must NOT be placed (they would never fill). The SL is measured from the entry price.
/// </summary>
[TestClass]
public class AltradySlConsistencyTests
{
    // ═══════════════════════════════════════════════════════════════════════
    //  Helpers: simulate Altrady webhook DCA/SL logic
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Filters a DCA list the same way the Altrady webhook does: skip levels at or beyond
    /// the signal SL percentage. Returns the surviving DCA entries.
    /// </summary>
    private static List<CryptoDcaEntry> FilterDcasForSignalSl(
        List<CryptoDcaEntry> dcaList, decimal? signalSlPercentage)
    {
        if (!signalSlPercentage.HasValue)
            return dcaList;

        return dcaList.Where(d => d.Percentage < signalSlPercentage.Value).ToList();
    }

    /// <summary>
    /// Reproduces the Altrady webhook's stop_loss_percentage calculation
    /// (after DCA filtering).
    /// </summary>
    private static decimal? ComputeAltradySlPercentage(
        decimal? signalSlPercentage,
        List<CryptoDcaEntry> filteredDcaList,
        decimal globalStopLossPercentage)
    {
        decimal deepestDcaPct = 0;
        foreach (var dca in filteredDcaList)
        {
            if (dca.Percentage > deepestDcaPct)
                deepestDcaPct = dca.Percentage;
        }

        if (signalSlPercentage.HasValue)
            return signalSlPercentage.Value;

        if (globalStopLossPercentage > 0)
            return deepestDcaPct + globalStopLossPercentage;

        return null;
    }

    private static decimal LongPrice(decimal entry, decimal pct) => entry * (1 - pct / 100m);
    private static decimal ShortPrice(decimal entry, decimal pct) => entry * (1 + pct / 100m);


    // ═══════════════════════════════════════════════════════════════════════
    //  DCA filtering: DCAs beyond signal SL are not placed
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void DcaFilter_TightSl_SkipsDcasBeyondSl()
    {
        var dcaList = new List<CryptoDcaEntry>
        {
            new() { Factor = 200, Percentage = 1.5m },
            new() { Factor = 400, Percentage = 4.5m },
        };

        var filtered = FilterDcasForSignalSl(dcaList, 2.5m);

        Assert.AreEqual(1, filtered.Count, "Only DCA1 (1.5%) should survive; DCA2 (4.5%) is beyond SL (2.5%)");
        Assert.AreEqual(1.5m, filtered[0].Percentage);
    }

    [TestMethod]
    public void DcaFilter_VeryTightSl_SkipsAllDcas()
    {
        var dcaList = new List<CryptoDcaEntry>
        {
            new() { Factor = 200, Percentage = 1.5m },
            new() { Factor = 400, Percentage = 4.5m },
        };

        var filtered = FilterDcasForSignalSl(dcaList, 1.0m);

        Assert.AreEqual(0, filtered.Count, "All DCAs are beyond the 1.0% SL");
    }

    [TestMethod]
    public void DcaFilter_WideSl_KeepsAllDcas()
    {
        var dcaList = new List<CryptoDcaEntry>
        {
            new() { Factor = 200, Percentage = 1.5m },
            new() { Factor = 400, Percentage = 4.5m },
        };

        var filtered = FilterDcasForSignalSl(dcaList, 6.0m);

        Assert.AreEqual(2, filtered.Count, "Both DCAs are within the 6.0% SL");
    }

    [TestMethod]
    public void DcaFilter_NoSignalSl_KeepsAllDcas()
    {
        var dcaList = new List<CryptoDcaEntry>
        {
            new() { Factor = 200, Percentage = 1.5m },
            new() { Factor = 400, Percentage = 4.5m },
        };

        var filtered = FilterDcasForSignalSl(dcaList, null);

        Assert.AreEqual(2, filtered.Count, "Without signal SL, all DCAs should be kept (global SL handles it)");
    }

    [TestMethod]
    public void DcaFilter_DcaAtExactSl_IsSkipped()
    {
        var dcaList = new List<CryptoDcaEntry>
        {
            new() { Factor = 200, Percentage = 2.5m },
        };

        var filtered = FilterDcasForSignalSl(dcaList, 2.5m);

        Assert.AreEqual(0, filtered.Count, "DCA at exactly the SL percentage should be skipped");
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  Altrady SL percentage: signal SL is sent as-is (from entry)
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Altrady_SignalSl_UsesRawPercentage()
    {
        var filtered = FilterDcasForSignalSl(new List<CryptoDcaEntry>
        {
            new() { Factor = 200, Percentage = 1.5m },
            new() { Factor = 400, Percentage = 4.5m },
        }, 2.5m);

        decimal? pct = ComputeAltradySlPercentage(2.5m, filtered, 5m);

        Assert.AreEqual(2.5m, pct, "Signal SL is sent directly as stop_loss_percentage");
    }

    [TestMethod]
    public void Altrady_GlobalSl_AddsDeepestDcaPct()
    {
        var dcaList = new List<CryptoDcaEntry>
        {
            new() { Factor = 200, Percentage = 1.5m },
            new() { Factor = 400, Percentage = 4.5m },
        };

        decimal? pct = ComputeAltradySlPercentage(null, dcaList, 5m);

        Assert.AreEqual(9.5m, pct, "Global SL = deepest DCA (4.5%) + global SL (5%) = 9.5%");
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  Consistency: Altrady vs StopLossCalculator (papertrading)
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Consistency_Long_SignalSl_BothAnchorOnEntry()
    {
        decimal entry = 100m;
        decimal signalSlPct = 2.5m;

        // Papertrading: signal SL anchors on entry (not on DCA)
        var ptResult = Calculate(new SlInput
        {
            Side = CryptoTradeSide.Long,
            SlPercentage = signalSlPct,
            EntryPrice = entry,
            ExtremeDcaPrice = LongPrice(entry, 1.5m),  // DCA within range
            GlobalStopLossPercentage = 5m,
            GlobalStopLossLimitPercentage = 6m,
        });

        // Altrady: signal SL percentage from entry
        decimal altradySlPrice = LongPrice(entry, signalSlPct);

        Assert.AreEqual(altradySlPrice, ptResult.Stop,
            "Signal SL: both systems must anchor on entry and produce the same stop price");
    }

    [TestMethod]
    public void Consistency_Short_SignalSl_BothAnchorOnEntry()
    {
        decimal entry = 100m;
        decimal signalSlPct = 2.5m;

        var ptResult = Calculate(new SlInput
        {
            Side = CryptoTradeSide.Short,
            SlPercentage = signalSlPct,
            EntryPrice = entry,
            ExtremeDcaPrice = ShortPrice(entry, 1.5m),
            GlobalStopLossPercentage = 5m,
            GlobalStopLossLimitPercentage = 6m,
        });

        decimal altradySlPrice = ShortPrice(entry, signalSlPct);

        Assert.AreEqual(altradySlPrice, ptResult.Stop,
            "Signal SL: both systems must produce the same stop price");
    }

    [TestMethod]
    public void Consistency_Long_NoDcas_IdenticalPlacement()
    {
        decimal entry = 100m;
        decimal signalSlPct = 3.0m;

        var ptResult = Calculate(new SlInput
        {
            Side = CryptoTradeSide.Long,
            SlPercentage = signalSlPct,
            EntryPrice = entry,
            ExtremeDcaPrice = null,
            GlobalStopLossPercentage = 5m,
            GlobalStopLossLimitPercentage = 6m,
        });

        decimal altradySlPrice = LongPrice(entry, signalSlPct);

        Assert.AreEqual(altradySlPrice, ptResult.Stop);
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  BRE-typical: tight band-width SL with standard DCAs
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Bre_Long_TightSl_DcasBeyondSlNotPlaced()
    {
        decimal entry = 150m;
        decimal breSl = 2.0m;
        var dcaList = new List<CryptoDcaEntry>
        {
            new() { Factor = 200, Percentage = 1.5m },
            new() { Factor = 400, Percentage = 4.5m },
        };

        var filtered = FilterDcasForSignalSl(dcaList, breSl);
        decimal? altradyPct = ComputeAltradySlPercentage(breSl, filtered, 5m);
        decimal slPrice = LongPrice(entry, altradyPct!.Value);

        Assert.AreEqual(1, filtered.Count, "DCA2 at 4.5% is beyond SL at 2.0%");
        Assert.AreEqual(1.5m, filtered[0].Percentage);

        decimal dca1Price = LongPrice(entry, 1.5m);
        Assert.IsTrue(slPrice < dca1Price,
            $"SL ({slPrice}) must be below the remaining DCA ({dca1Price})");

        Console.WriteLine($"BRE Long: entry={entry}, SL={breSl}%");
        Console.WriteLine($"  DCA1: {dca1Price} (1.5%) - placed");
        Console.WriteLine($"  DCA2: {LongPrice(entry, 4.5m)} (4.5%) - NOT placed (beyond SL)");
        Console.WriteLine($"  SL:   {slPrice} ({altradyPct}%)");
    }

    [TestMethod]
    public void Bre_Short_TightSl_DcasBeyondSlNotPlaced()
    {
        decimal entry = 150m;
        decimal breSl = 2.0m;
        var dcaList = new List<CryptoDcaEntry>
        {
            new() { Factor = 200, Percentage = 1.5m },
            new() { Factor = 400, Percentage = 4.5m },
        };

        var filtered = FilterDcasForSignalSl(dcaList, breSl);
        decimal? altradyPct = ComputeAltradySlPercentage(breSl, filtered, 5m);
        decimal slPrice = ShortPrice(entry, altradyPct!.Value);

        Assert.AreEqual(1, filtered.Count);

        decimal dca1Price = ShortPrice(entry, 1.5m);
        Assert.IsTrue(slPrice > dca1Price,
            $"Short SL ({slPrice}) must be above the remaining DCA ({dca1Price})");
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  Full stack ordering: SL < DCA < Entry < TP (long)
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Long_FullStack_CorrectOrdering()
    {
        decimal entry = 100m;
        decimal breSl = 2.5m;
        var dcaList = new List<CryptoDcaEntry>
        {
            new() { Factor = 200, Percentage = 1.5m },
            new() { Factor = 400, Percentage = 4.5m },
        };
        var tpList = new List<CryptoTpEntry>
        {
            new() { Factor = 50, Percentage = 0.75m },
            new() { Factor = 100, Percentage = 1.5m },
        };

        var filteredDcas = FilterDcasForSignalSl(dcaList, breSl);
        decimal slPrice = LongPrice(entry, breSl);
        decimal tp1 = entry * (1 + tpList[0].Percentage / 100m);
        decimal tp2 = entry * (1 + tpList[1].Percentage / 100m);

        // Only DCA1 survives (1.5% < 2.5% SL)
        Assert.AreEqual(1, filteredDcas.Count);
        decimal dca1 = LongPrice(entry, filteredDcas[0].Percentage);

        // Expected order: SL < DCA1 < Entry < TP1 < TP2
        Assert.IsTrue(slPrice < dca1, $"SL ({slPrice}) must be < DCA1 ({dca1})");
        Assert.IsTrue(dca1 < entry, $"DCA1 ({dca1}) must be < Entry ({entry})");
        Assert.IsTrue(entry < tp1, $"Entry ({entry}) must be < TP1 ({tp1})");
        Assert.IsTrue(tp1 < tp2, $"TP1 ({tp1}) must be < TP2 ({tp2})");

        Console.WriteLine("Long full stack (SL < DCA1 < Entry < TP1 < TP2):");
        Console.WriteLine($"  SL:    {slPrice:N4} ({breSl}%)");
        Console.WriteLine($"  DCA1:  {dca1:N4} (1.5%)");
        Console.WriteLine($"  Entry: {entry:N4}");
        Console.WriteLine($"  TP1:   {tp1:N4} ({tpList[0].Percentage}%)");
        Console.WriteLine($"  TP2:   {tp2:N4} ({tpList[1].Percentage}%)");
        Console.WriteLine($"  DCA2 (4.5%): NOT placed — beyond SL");
    }

    [TestMethod]
    public void Short_FullStack_CorrectOrdering()
    {
        decimal entry = 100m;
        decimal breSl = 2.5m;
        var dcaList = new List<CryptoDcaEntry>
        {
            new() { Factor = 200, Percentage = 1.5m },
            new() { Factor = 400, Percentage = 4.5m },
        };
        var tpList = new List<CryptoTpEntry>
        {
            new() { Factor = 50, Percentage = 0.75m },
            new() { Factor = 100, Percentage = 1.5m },
        };

        var filteredDcas = FilterDcasForSignalSl(dcaList, breSl);
        decimal slPrice = ShortPrice(entry, breSl);
        decimal tp1 = entry * (1 - tpList[0].Percentage / 100m);
        decimal tp2 = entry * (1 - tpList[1].Percentage / 100m);

        Assert.AreEqual(1, filteredDcas.Count);
        decimal dca1 = ShortPrice(entry, filteredDcas[0].Percentage);

        // Expected order: TP2 < TP1 < Entry < DCA1 < SL
        Assert.IsTrue(tp2 < tp1, $"TP2 ({tp2}) must be < TP1 ({tp1})");
        Assert.IsTrue(tp1 < entry, $"TP1 ({tp1}) must be < Entry ({entry})");
        Assert.IsTrue(entry < dca1, $"Entry ({entry}) must be < DCA1 ({dca1})");
        Assert.IsTrue(dca1 < slPrice, $"DCA1 ({dca1}) must be < SL ({slPrice})");

        Console.WriteLine("Short full stack (TP2 < TP1 < Entry < DCA1 < SL):");
        Console.WriteLine($"  TP2:   {tp2:N4} ({tpList[1].Percentage}%)");
        Console.WriteLine($"  TP1:   {tp1:N4} ({tpList[0].Percentage}%)");
        Console.WriteLine($"  Entry: {entry:N4}");
        Console.WriteLine($"  DCA1:  {dca1:N4} (1.5%)");
        Console.WriteLine($"  SL:    {slPrice:N4} ({breSl}%)");
        Console.WriteLine($"  DCA2 (4.5%): NOT placed — beyond SL");
    }
}
