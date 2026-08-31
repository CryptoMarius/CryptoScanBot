using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Model;

/// <summary>
/// One barometer measurement, stored next to the run that produced it.
/// <para>
/// A signal and a position already carry the barometer per interval, but only its AVERAGE - the
/// five Barometer15m..Barometer1d columns on <see cref="CryptoData"/>. The rest of the same
/// measurement (median, breadth, spread, movement, bitcoin against the market) is what tells "the
/// whole market drifts up a little" apart from "three coins explode and the rest sinks", and that
/// used to live only in the candles of the barometer symbols - which the emulator does not write
/// and which the live scanner ages out after hours. So it was measured and thrown away.
/// </para>
/// <para>
/// Two kinds of row, both written by the emulator: one per created position (PositionId filled),
/// which is what an analysis joins on to separate profit from loss, and a heartbeat every
/// <see cref="Const.Constants.BarometerHeartbeatMinutes"/> replayed minutes (PositionId null), which
/// gives the market context of the whole run - without it there is no denominator to say how often
/// a falling market even occurred.
/// </para>
/// </summary>
[Table("BarometerSnapshot")]
public class CryptoBarometerSnapshot
{
    [Key]
    public int Id { get; set; }

    // FK to EmulatorRun, so the rows of a run stay separated from those of other runs - same as
    // Position/Signal/AssetSnapshot. Null would mean the live scanner, which does not write here.
    public int? EmulatorRunId { get; set; }

    /// <summary>
    /// The position this measurement belongs to, or null for a heartbeat row. Deliberately NOT a
    /// foreign key: a position can be deleted (see GlobalData.PositionDeleted) and a measurement
    /// that was taken stays true regardless.
    /// </summary>
    public int? PositionId { get; set; }

    // The moment measured, taken from the emulator clock: the close of the last complete minute
    // that took part in the measurement.
    public DateTime MeasureDate { get; set; }

    // The quote coin this barometer is for (USDT, BTC, ...). A barometer is per quote coin.
    public string Quote { get; set; } = "";

    // Interval NAME ("15m", "1h", "1d") instead of the enum number, because these rows exist to be
    // read back by hand and by an analysis script.
    public string Interval { get; set; } = "";

    // The nine figures of BarometerResult. Average is the barometer as every filter knows it.
    public decimal Average { get; set; }
    public decimal Median { get; set; }
    public decimal PercentageRising { get; set; }
    public decimal Spread { get; set; }
    public decimal Movement { get; set; }
    public decimal? BitcoinVersusMarket { get; set; }
    public int SymbolCount { get; set; }
    public int OutlierCount { get; set; }
}
