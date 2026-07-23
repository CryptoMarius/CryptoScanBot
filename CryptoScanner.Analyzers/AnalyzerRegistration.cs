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
        // ATR based bands, but it does not perform well enough
        //PluginManager.Register(new AtrRb.AtrRbPlugin()); 

        // A band strategy which does rather well
        PluginManager.Register(new Dbr.DbrPlugin());

        PluginManager.Register(new Dlz.DlzPlugin());
        PluginManager.Register(new Fvg.FvgPlugin());
        PluginManager.Register(new Jump.JumpPlugin());
        PluginManager.Register(new Sbm.SbmPlugin());
        PluginManager.Register(new Smc.SmcPlugin());
        PluginManager.Register(new Stobb.StobbPlugin());
        PluginManager.Register(new Storsi.StorsiPlugin());
        PluginManager.Register(new Vbs.VbsPlugin());

#if DEBUG
        // Experimental strategies (not yet fully tested or documented)
        PluginManager.Register(new Bbma.BbmaPlugin());
        PluginManager.Register(new BbRsiEngulfing.BbRsiEngulfingPlugin());
        PluginManager.Register(new Choch.ChochPlugin());
        PluginManager.Register(new DoubleTopBottom.DoubleTopBottomPlugin());
        PluginManager.Register(new IChimokuKumoBreakout.IChimokuKumoBreakoutPlugin());
        PluginManager.Register(new Nwe.NwePlugin());
        PluginManager.Register(new Trend.TrendPlugin());
#endif
    }
}
