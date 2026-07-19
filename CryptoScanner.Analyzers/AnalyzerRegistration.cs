using CryptoScanner.Core.Contracts;

namespace CryptoScanner.Analyzers;

/// <summary>
/// Entry point for the Analyzers project. Call <see cref="RegisterAll"/> once
/// at startup to register all analyzer strategies into the PluginManager.
/// </summary>
public static class AnalyzerRegistration
{
    public static void RegisterAll()
    {
        //PluginManager.Register(new AtrRb.AtrRbPlugin());
        PluginManager.Register(new Baba.BabaPlugin());
        PluginManager.Register(new Bre.BrePlugin());

        PluginManager.Register(new Storsi.StoRsiPlugin());

#if DEBUG
        // BBMA is DEBUG-only (the signal classes are guarded with #if DEBUG).
        PluginManager.Register(new IChimokuKumoBreakout.IChimokuKumoBreakoutPlugin());
        PluginManager.Register(new Bbma.BbmaPlugin());
#endif

        // Stand-alone overlay (not a strategy): TradingBuddy's own served BABA bands, so they can be
        // toggled independently and compared with the scanner's "Baba Bands" overlay.
        PluginManager.RegisterOverlay(new Baba.Chart.TradingBuddyBabaOverlay());
    }
}
