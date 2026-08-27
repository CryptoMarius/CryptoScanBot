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
        // tijdelijk terug..
        PluginManager.Register(new AtrRb.AtrRbPlugin());

        // A band strategy which does rather well
        PluginManager.Register(new Dbr.DbrPlugin());

        PluginManager.Register(new Dlz.DlzPlugin());
        PluginManager.Register(new Fvg.FvgPlugin());
        PluginManager.Register(new Jump.JumpPlugin());

        // Large breakous with large profits and large losses
        // tijdelijk terug..
        PluginManager.Register(new Nwe.NwePlugin());

        PluginManager.Register(new Sbm.SbmPlugin());
        PluginManager.Register(new Smc.SmcPlugin());
        PluginManager.Register(new Stobb.StobbPlugin());
        PluginManager.Register(new Storsi.StorsiPlugin());

        // A new band stratgy which is still being tested, but looks promising
        PluginManager.Register(new Vbs.VbsPlugin());

        // Experimental strategies (not yet fully tested or documented)
#if DEBUG
        // These look interesting (specially the squeeze ones)
        PluginManager.Register(new BbSqueeze.BbSqueezePlugin());
        PluginManager.Register(new IChimokuKumoBreakout.IChimokuKumoBreakoutPlugin());
        PluginManager.Register(new KumoSqueeze.KumoSqueezePlugin());

        // From the Malysian trader Oma Ally, not much signals but performs well (no profits yet)
        PluginManager.Register(new Bbma.BbmaPlugin());
        PluginManager.Register(new BbRsiEngulfing.BbRsiEngulfingPlugin());
        // Very disapointing, expected more of this strategy
        PluginManager.Register(new Choch.ChochPlugin());
        // Lots of noise, there is alway's some sort of dtd to be found
        PluginManager.Register(new DoubleTopBottom.DoubleTopBottomPlugin());
        PluginManager.Register(new SuperTrendBreakout.SuperTrendBreakoutPlugin());
        // Very disapointing, expected more of this strategy
        PluginManager.Register(new Trend.TrendPlugin());
#endif
    }
}
