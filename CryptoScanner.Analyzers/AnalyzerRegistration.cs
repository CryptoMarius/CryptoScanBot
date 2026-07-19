using CryptoScanner.Analyzers.AtrRb;
using CryptoScanner.Analyzers.Baba;
using CryptoScanner.Analyzers.Baba.Chart;
#if DEBUG
using CryptoScanner.Analyzers.Bbma;
#endif
using CryptoScanner.Analyzers.Bre;
using CryptoScanner.Analyzers.Storsi;
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
        //PluginManager.Register(new AtrRbPlugin());
        PluginManager.Register(new BabaPlugin());
        PluginManager.Register(new BrePlugin());

        PluginManager.Register(new StoRsiPlugin());

#if DEBUG
        // BBMA is DEBUG-only (the signal classes are guarded with #if DEBUG).
        PluginManager.Register(new BbmaPlugin());
#endif

        // Stand-alone overlay (not a strategy): TradingBuddy's own served BABA bands, so they can be
        // toggled independently and compared with the scanner's "Baba Bands" overlay.
        PluginManager.RegisterOverlay(new TradingBuddyBabaOverlay());
    }
}
