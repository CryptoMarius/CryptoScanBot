using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Trader;

/// <summary>
/// Writes the daily history the <see cref="CryptoAssetSnapshot"/> table holds, and reads it back for
/// the dashboards.
///
/// <para>
/// The rule: one snapshot per calendar day, stamped on midnight of that day and holding the balances
/// as they were at the moment it was taken. It is taken the first time the application notices that
/// the day has no snapshot yet - so at 00:00 for a scanner that runs around the clock, and otherwise
/// at the first moment it runs that day. The emulator forces one extra at the start of a run (the
/// start capital) and one at the end, and that last one overwrites the snapshot of its own day, so
/// the final point of a run is the result of that run.
/// </para>
/// <para>
/// The clock is <see cref="GlobalData.Clock"/>, so a replay is stamped with emulator time and a run
/// produces the same dates every time it is repeated.
/// </para>
/// </summary>
public static class AssetSnapshotTools
{
    /// <summary>
    /// The coin every value is expressed in. Also stored per row, so old snapshots stay readable
    /// when this ever changes.
    /// </summary>
    public const string ReferenceCoin = "USDT";

    private static readonly object captureLock = new();

    // The day of the newest snapshot within the current series, so the check that runs every minute
    // (and, in the emulator, every replayed minute) costs nothing.
    private static DateTime? lastSnapshotDay;
    private static bool lastSnapshotDayKnown;
    private static int? lastSnapshotRunId;


    /// <summary>
    /// Forget what was captured before. The emulator calls this at the start of a run, together with
    /// the rest of the transient state, so the previous run's day does not suppress the first
    /// snapshot of this one.
    /// </summary>
    public static void Reset()
    {
        lock (captureLock)
        {
            lastSnapshotDay = null;
            lastSnapshotDayKnown = false;
            lastSnapshotRunId = null;
        }
    }


    /// <summary>
    /// Take a snapshot when the day of <see cref="GlobalData.Clock"/> has no snapshot yet. Cheap
    /// enough to call every minute: normally this is one comparison of two dates.
    /// </summary>
    public static void CaptureIfDue(Model.CryptoExchange activeExchange)
    {
        DateTime day = GlobalData.Clock.UtcNow.Date;
        int? runId = GlobalData.CurrentEmulatorRunId;

        lock (captureLock)
        {
            // A different run (or the switch between a run and the live scanner) is a different
            // series of snapshots, so the day of the previous one says nothing about this one.
            if (!lastSnapshotDayKnown || lastSnapshotRunId != runId)
            {
                lastSnapshotDay = ReadLastSnapshotDay(runId);
                lastSnapshotRunId = runId;
                lastSnapshotDayKnown = true;
            }

            if (lastSnapshotDay == day)
                return;
        }

        Capture(activeExchange, day);
    }


    /// <summary>
    /// Take a snapshot for <paramref name="day"/>, replacing the one that day already had.
    /// <para>
    /// The day is handed in instead of read from the clock because the emulator parks its clock on
    /// the END of the replay window before a run starts (see ReplayRunner), so the start capital has
    /// to be stamped with the run's own start date.
    /// </para>
    /// </summary>
    public static void Capture(Model.CryptoExchange activeExchange, DateTime day)
    {
        day = day.Date;

        try
        {
            List<CryptoAssetSnapshot> rows = BuildRows(activeExchange, day);

            lock (captureLock)
            {
                // Even without balances the day counts as done, otherwise CaptureIfDue would keep
                // trying every minute. Real trading has no balances at all - reading them from the
                // exchange is not implemented, see AssetTools.FetchAssets.
                lastSnapshotDay = day;
                lastSnapshotRunId = GlobalData.CurrentEmulatorRunId;
                lastSnapshotDayKnown = true;

                if (rows.Count == 0)
                    return;

                using CryptoDatabase database = new();
                database.Open();
                using var transaction = database.BeginTransaction();

                database.Connection.Execute(
                    "delete from AssetSnapshot where SnapshotDate = @day and EmulatorRunId is @runId",
                    new { day, runId = rows[0].EmulatorRunId }, transaction);

                foreach (CryptoAssetSnapshot row in rows)
                    database.Connection.Insert(row, transaction);

                transaction.Commit();
            }
        }
        catch (Exception error)
        {
            // A missing snapshot costs a point in a graph, it may never cost a trade
            ScannerLog.Logger.Error(error, "AssetSnapshotTools.Capture");
        }
    }


    /// <summary>
    /// The balances of right now as snapshot rows, one per coin, valued in <see cref="ReferenceCoin"/>.
    /// </summary>
    private static List<CryptoAssetSnapshot> BuildRows(Model.CryptoExchange activeExchange, DateTime day)
    {
        List<CryptoAssetSnapshot> rows = [];
        int? runId = GlobalData.CurrentEmulatorRunId;

        // Collected before the semaphore is taken: this walks the positions, and the asset semaphore
        // has nothing to do with those.
        Dictionary<string, decimal> shortPerBase = CollectShortQuantities(activeExchange, out Dictionary<string, decimal> priceHints);

        activeExchange.Data.AssetListSemaphore.Wait();
        try
        {
            // Locked is derived from the orders that are open right now, so refresh it before reading
            // it - same reason as in AssetTools.GetAsset.
            PaperAssets.RecalculateLocked(activeExchange);

            foreach (CryptoAsset asset in activeExchange.Data.AssetList.Values)
            {
                shortPerBase.Remove(asset.Name, out decimal shortQuantity);
                rows.Add(CreateRow(activeExchange, day, runId, asset.Name,
                    asset.Total, asset.Free, asset.Locked, shortQuantity, priceHints));
            }
        }
        finally
        {
            activeExchange.Data.AssetListSemaphore.Release();
        }

        // What is left are shorts on a coin without a balance of its own, which is the normal case:
        // a short never touches the base asset. Booking them anyway is the whole point of
        // ShortQuantity - without these rows the sale proceeds would count as capital and the debt
        // behind them would not.
        foreach (var (name, shortQuantity) in shortPerBase)
            rows.Add(CreateRow(activeExchange, day, runId, name, 0, 0, 0, shortQuantity, priceHints));

        return rows;
    }


    private static CryptoAssetSnapshot CreateRow(Model.CryptoExchange activeExchange, DateTime day, int? runId,
        string name, decimal total, decimal free, decimal locked, decimal shortQuantity,
        Dictionary<string, decimal> priceHints)
    {
        decimal price = ResolvePrice(activeExchange, name, priceHints);
        return new CryptoAssetSnapshot
        {
            EmulatorRunId = runId,
            SnapshotDate = day,
            Name = name,
            Total = total,
            Free = free,
            Locked = locked,
            ShortQuantity = shortQuantity,
            ReferenceCoin = ReferenceCoin,
            Price = price,
            Value = (total - shortQuantity) * price,
        };
    }


    /// <summary>
    /// The base quantity every coin still owes on open short positions, plus the last price of the
    /// symbols those positions are on (<paramref name="priceHints"/>) - the price of a coin that is
    /// actually being traded is best taken from the symbol that is trading it.
    /// </summary>
    private static Dictionary<string, decimal> CollectShortQuantities(Model.CryptoExchange activeExchange,
        out Dictionary<string, decimal> priceHints)
    {
        Dictionary<string, decimal> shortPerBase = [];
        priceHints = [];

        foreach (CryptoPosition position in activeExchange.Data.PositionList.Values)
        {
            CryptoSymbol symbol = position.Symbol;
            if (symbol.LastPrice.HasValue && symbol.Quote == ReferenceCoin)
                priceHints[symbol.Base] = symbol.LastPrice.Value;

            // Ready means the position is closed and bought back, so nothing is owed any more.
            if (position.Side != CryptoTradeSide.Short || position.Status == CryptoPositionStatus.Ready)
                continue;

            // Quantity is what was sold minus what was bought back again, so exactly the debt
            // (see TradeTools.CalculateProfitAndBreakEvenPrice).
            if (position.Quantity <= 0)
                continue;

            shortPerBase[symbol.Base] = shortPerBase.TryGetValue(symbol.Base, out decimal current)
                ? current + position.Quantity
                : position.Quantity;
        }

        return shortPerBase;
    }


    /// <summary>
    /// What one coin of <paramref name="name"/> is worth in <see cref="ReferenceCoin"/>. Zero when
    /// there is no pair to read a price from - the row is written anyway, it just does not count
    /// towards the total of that day.
    /// </summary>
    public static decimal ResolvePrice(Model.CryptoExchange activeExchange, string name)
    {
        if (name == ReferenceCoin)
            return 1m;

        if (activeExchange.TryGetSymbolByPair(name + ReferenceCoin, out CryptoSymbol? symbol) && symbol.LastPrice.HasValue)
            return symbol.LastPrice.Value;

        return 0m;
    }


    /// <summary>
    /// Same, but the price of a coin that is being traded right now is taken from the symbol trading
    /// it - during a replay only the symbols of the run have a price at all.
    /// </summary>
    private static decimal ResolvePrice(Model.CryptoExchange activeExchange, string name, Dictionary<string, decimal> priceHints)
    {
        if (name != ReferenceCoin && priceHints.TryGetValue(name, out decimal hinted))
            return hinted;

        return ResolvePrice(activeExchange, name);
    }


    /// <summary>
    /// The day of the newest snapshot of one series, or null when the series is still empty.
    /// </summary>
    private static DateTime? ReadLastSnapshotDay(int? runId)
    {
        try
        {
            using CryptoDatabase database = new();
            database.Open();
            return database.Connection.ExecuteScalar<DateTime?>(
                "select max(SnapshotDate) from AssetSnapshot where EmulatorRunId is @runId", new { runId });
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "AssetSnapshotTools.ReadLastSnapshotDay");
            return null;
        }
    }


    /// <summary>
    /// One day of capital: the date and the total of every coin, in <see cref="ReferenceCoin"/>.
    /// </summary>
    public class AssetSnapshotDay
    {
        public DateTime Date { get; set; }

        /// <summary>What was in the account on this day.</summary>
        public decimal Value { get; set; }

        /// <summary>
        /// Everything booked in or out by hand since the first day of the series, up to and including
        /// this one - deposits positive, withdrawals and deleted coins negative. Measured from the
        /// first day on purpose: whatever happened before the series started is part of its starting
        /// point, not of its course.
        /// </summary>
        public decimal Adjustment { get; set; }

        /// <summary>
        /// Where the capital would have been without those bookings, so the part that was really
        /// traded. Equal to <see cref="Value"/> until the first correction.
        /// </summary>
        public decimal ValueWithoutAdjustments => Value - Adjustment;
    }


    /// <summary>
    /// Adds up the manual bookings per day and puts the running total on every snapshot day, measured
    /// from the first day of the series.
    /// <para>
    /// Both lists are in date order. A booking made on a day WITHOUT a snapshot (the scanner was not
    /// running) counts towards the first snapshot day that follows it, and a booking made before the
    /// series starts lands entirely in the starting point and cancels itself out.
    /// </para>
    /// </summary>
    internal static void ApplyAdjustments(List<AssetSnapshotDay> days, List<AssetAdjustmentTools.AdjustmentDay> adjustments)
    {
        if (days.Count == 0)
            return;

        decimal running = 0;
        decimal atFirstDay = 0;
        int index = 0;

        for (int i = 0; i < days.Count; i++)
        {
            while (index < adjustments.Count && adjustments[index].Date.Date <= days[i].Date.Date)
            {
                running += adjustments[index].Value;
                index++;
            }

            if (i == 0)
                atFirstDay = running;
            days[i].Adjustment = running - atFirstDay;
        }
    }


    /// <summary>
    /// The capital per day of one series - the live scanner (<paramref name="emulatorRunId"/> null)
    /// or one emulator run - in date order.
    /// </summary>
    public static List<AssetSnapshotDay> LoadDailyTotals(int? emulatorRunId)
    {
        try
        {
            using CryptoDatabase database = new();
            database.Open();

            // Value is stored as TEXT (like every other decimal in this database), so it has to be
            // CAST before it can be summed.
            List<AssetSnapshotDay> days = [.. database.Connection.Query<AssetSnapshotDay>(
                "select SnapshotDate as Date, sum(cast(Value as REAL)) as Value " +
                "from AssetSnapshot where EmulatorRunId is @runId " +
                "group by SnapshotDate order by SnapshotDate", new { runId = emulatorRunId })];

            ApplyAdjustments(days, AssetAdjustmentTools.LoadDailyTotals(emulatorRunId));
            return days;
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "AssetSnapshotTools.LoadDailyTotals");
            return [];
        }
    }
}
