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

        // An impulse followed by a box of bodies, in against the impulse when the box breaks the
        // other way or a break with the impulse fails. Added 21-11-2026 in Release on request to
        // measure it quickly; back to the #if DEBUG block when the emulator is disappointing.
        // Unregistered on 24-11-2026 (the code stays). Measured in the emulator (Session2, runs
        // 2108-2111, 15m, stop 4, take profit 7.5): the long side earns (long only +2845.61 USDT over
        // 1584 trades), the short fake break loses (-1663.14). See ImpulseBox/ImpulseBox.md.
        //PluginManager.Register(new ImpulseBox.ImpulseBoxPlugin());

        // Change of character into a fair value gap on 1m: a limit on the middle of the gap inside
        // its fib zone, stop behind the gap candle, fixed risk/reward. Added 21-11-2026 in Release on
        // request to measure it quickly; to the #if DEBUG block when the emulator is disappointing.
        // Unregistered on 24-11-2026 (the code stays): runs 2124-2126 all lost (-74.61, -1052.60 and
        // -28.67 USDT), the win rate stays below what the risk/reward needs.
        //PluginManager.Register(new ChochFvg.ChochFvgPlugin());

        // The support/resistance flip (open point 46): a broken level retested from the other side
        // Moved to the #if DEBUG block on 05-11-2026: it did not perform well.
        //PluginManager.Register(new SrFlip.SrFlipPlugin());

        PluginManager.Register(new Dlz.DlzPlugin());
        PluginManager.Register(new Fvg.FvgPlugin());

        // The FVG & DCA strategy for BTC (and the coins of its list): buy limits on gap tops, DCA's,
        // take profit above break-even, no stop loss, several positions on one coin at once.
        // Paper trading and the emulator only. The rules are in FvgLadder/FvgLadder.md.
        PluginManager.Register(new FvgLadder.FvgLadderPlugin());

        PluginManager.Register(new Jump.JumpPlugin());

        PluginManager.Register(new Sbm.SbmPlugin());
        PluginManager.Register(new Smc.SmcPlugin());
        PluginManager.Register(new Stobb.StobbPlugin());
        PluginManager.Register(new Storsi.StorsiPlugin());

        // A band strategy that looked promising, but emulator was negative
        PluginManager.Register(new Vbs.VbsPlugin());

        // Experimental strategies (not yet fully tested or documented)
#if DEBUG || EXPERIMENTAL_STRATEGIES
        // ATR based bands, it does not perform well
        PluginManager.Register(new AtrRb.AtrRbPlugin());

        // From the Malysian trader Oma Ally, not much signals but performs well (no profits yet)
        //PluginManager.Register(new Bbma.BbmaPlugin());
        // Back on 06-10-2026 (still inside #if DEBUG), to be measured again.
        PluginManager.Register(new Bbma.BbmaPlugin());

        PluginManager.Register(new BbRsiEngulfing.BbRsiEngulfingPlugin());

        // These look interesting (specially the squeeze ones)
        PluginManager.Register(new BbSqueeze.BbSqueezePlugin());

        // Very disapointing, expected more of this strategy
        PluginManager.Register(new Choch.ChochPlugin());

        // Lots of noise, there is alway's some sort of dtd to be found

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

        // The support/resistance flip (open point 46), it did not perform well
        PluginManager.Register(new SrFlip.SrFlipPlugin());

        PluginManager.Register(new SuperTrendBreakout.SuperTrendBreakoutPlugin());

        // VWAP mean reversion: a touch of a band around the day VWAP with a reversal candle, out
        // when a candle closes back at the VWAP. Measured on 18/19-10-2026: the VWAP exit loses on
        // every interval, only 8h with a far fixed take profit made a small profit. Stays in Debug
        // as a reference; the numbers are in VwapMr/VwapMr.md.
        PluginManager.Register(new VwapMr.VwapMrPlugin());

        // Cross-sectional momentum: a weekly ranking of all coins of a quote on their return, long
        // the strongest and short the weakest, closed when they drop out. Measured offline on
        // 24-11-2026 (positive), not in the emulator yet. The rules are in XsMom/XsMom.md.
        PluginManager.Register(new XsMom.XsMomPlugin());


        // Very disapointing, expected more of this strategy
        PluginManager.Register(new Trend.TrendPlugin());

        // The Heikin Ashi trend: in on the colour change of the Heikin Ashi candle with the higher
        // interval (1w) on the same colour, out on the first candle of the other colour or with a
        // wick on the other side. Measured offline on 1d (09-11-2026), not in the emulator yet.
        // Moved to the #if DEBUG block on 20-11-2026.
        PluginManager.Register(new HeikinAshi.HeikinAshiPlugin());
#endif

        // A trending breakout: a cloud of four moving averages for the trend, with three entries
        // inside it - the break of a pivot level, the crossing of the two EMAs, or a pullback to
        // the fast line. Not measured yet.
        PluginManager.Register(new Mac.MacPlugin());

        // The parabolic SAR flip: in when the SAR flips to the other side of the price, out on the
        // flip back. Not measured yet.
        PluginManager.Register(new PsarCross.PsarCrossPlugin());

        // The Heikin Ashi trend: in on the colour change of the Heikin Ashi candle with the higher
        // interval (1w) on the same colour, out on the first candle of the other colour or with a
        // wick on the other side. Measured offline on 1d (09-11-2026), not in the emulator yet.
        // Moved to the #if DEBUG block on 20-11-2026.
        //PluginManager.Register(new HeikinAshi.HeikinAshiPlugin());
    }
}
