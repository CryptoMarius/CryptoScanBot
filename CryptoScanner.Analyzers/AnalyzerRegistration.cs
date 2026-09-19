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
        // The classic reversal shapes
        PluginManager.Register(new CandlePattern.CandlePatternPlugin());

        // A band strategy which does rather well
        PluginManager.Register(new Dbr.DbrPlugin());

        // The break that did not hold
        PluginManager.Register(new FailedBreakout.FailedBreakoutPlugin());

        PluginManager.Register(new Dlz.DlzPlugin());
        PluginManager.Register(new Fvg.FvgPlugin());
        PluginManager.Register(new Jump.JumpPlugin());

        PluginManager.Register(new Sbm.SbmPlugin());
        PluginManager.Register(new Smc.SmcPlugin());
        PluginManager.Register(new Stobb.StobbPlugin());
        PluginManager.Register(new Storsi.StorsiPlugin());

        // A band strategy that looked promising, but emulator was negative
        PluginManager.Register(new Vbs.VbsPlugin());

        // Experimental strategies (not yet fully tested or documented)
#if DEBUG
        // ATR based bands, it does not perform well
        PluginManager.Register(new AtrRb.AtrRbPlugin());

        // From the Malysian trader Oma Ally, not much signals but performs well (no profits yet)
        //PluginManager.Register(new Bbma.BbmaPlugin());

        PluginManager.Register(new BbRsiEngulfing.BbRsiEngulfingPlugin());

        // These look interesting (specially the squeeze ones)
        PluginManager.Register(new BbSqueeze.BbSqueezePlugin());

        // Very disapointing, expected more of this strategy
        PluginManager.Register(new Choch.ChochPlugin());

        // Lots of noise, there is alway's some sort of dtd to be found
        PluginManager.Register(new DoubleTopBottom.DoubleTopBottomPlugin());

        PluginManager.Register(new IChimokuKumoBreakout.IChimokuKumoBreakoutPlugin());
        PluginManager.Register(new KumoSqueeze.KumoSqueezePlugin());

        // The MACD crossover with an exit rule of its own: in on the cross, out on the cross back.
        // Being measured. The first strategy to use SignalCreateBase.IsExitSignal.
        PluginManager.Register(new MacdCross.MacdCrossPlugin());

        // The same crossover, but only when the price was at a band of Vbs, AtrRb or Dbr in the
        // last few candles. Meant as an attention filter: a cross on its own fires often, a cross
        // right after a band break is the rarer situation that is worth opening the chart for.
        PluginManager.Register(new MacdCrossBand.MacdCrossBandPlugin());

        // Large breakous with large profits and large losses
        PluginManager.Register(new Nwe.NwePlugin());

        PluginManager.Register(new SuperTrendBreakout.SuperTrendBreakoutPlugin());


        // Very disapointing, expected more of this strategy
        PluginManager.Register(new Trend.TrendPlugin());
#endif

        // A trending breakout: a cloud of four moving averages for the trend, with three entries
        // inside it - the break of a pivot level, the crossing of the two EMAs, or a pullback to
        // the fast line. Not measured yet.
        PluginManager.Register(new Mac.MacPlugin());
    }
}
