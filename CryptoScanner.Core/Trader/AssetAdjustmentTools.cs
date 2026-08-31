using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Trader;

/// <summary>
/// The ledger of money that went into or out of the account without a trade behind it - see
/// <see cref="CryptoAssetAdjustment"/> for why it is kept.
///
/// <para>
/// Recorded at the three places in <see cref="PaperAssets"/> that change a balance outside the
/// trader: handing out the start capital, a correction from the paper-assets screen, and "start
/// over". Everything else that moves a balance is a fill, and a fill is a result, not a deposit.
/// </para>
/// </summary>
public static class AssetAdjustmentTools
{
    /// <summary>
    /// Write down that <paramref name="name"/> went from <paramref name="oldTotal"/> to
    /// <paramref name="newTotal"/> by hand. Nothing is written when the amount did not actually
    /// change.
    /// <para>
    /// Never lets a failure through: a missing ledger line makes a graph harder to read, it may not
    /// stop the balance from being corrected.
    /// </para>
    /// </summary>
    public static void Record(Model.CryptoExchange activeExchange, string name,
        decimal oldTotal, decimal newTotal, CryptoAssetAdjustmentReason reason)
    {
        if (oldTotal == newTotal)
            return;

        try
        {
            decimal price = AssetSnapshotTools.ResolvePrice(activeExchange, name);
            CryptoAssetAdjustment adjustment = new()
            {
                EmulatorRunId = GlobalData.CurrentEmulatorRunId,
                EventTime = GlobalData.Clock.UtcNow,
                Name = name,
                Reason = reason,
                OldTotal = oldTotal,
                NewTotal = newTotal,
                Quantity = newTotal - oldTotal,
                ReferenceCoin = AssetSnapshotTools.ReferenceCoin,
                Price = price,
                Value = (newTotal - oldTotal) * price,
            };

            using CryptoDatabase database = new();
            database.Open();
            database.Connection.Insert(adjustment);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "AssetAdjustmentTools.Record");
        }
    }


    /// <summary>
    /// Everything that was booked in or out on one day, in
    /// <see cref="AssetSnapshotTools.ReferenceCoin"/>, for the live scanner
    /// (<paramref name="emulatorRunId"/> null) or one run.
    /// </summary>
    public static List<AdjustmentDay> LoadDailyTotals(int? emulatorRunId)
    {
        try
        {
            using CryptoDatabase database = new();
            database.Open();

            // Value is stored as TEXT (like every other decimal in this database), so it has to be
            // CAST before it can be summed. date() strips the time, because the capital line only
            // knows days.
            return [.. database.Connection.Query<AdjustmentDay>(
                "select date(EventTime) as Date, sum(cast(Value as REAL)) as Value " +
                "from AssetAdjustment where EmulatorRunId is @runId " +
                "group by date(EventTime) order by date(EventTime)", new { runId = emulatorRunId })];
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "AssetAdjustmentTools.LoadDailyTotals");
            return [];
        }
    }


    /// <summary>One day of manual bookings: the date and the net amount, in the reference coin.</summary>
    public class AdjustmentDay
    {
        public DateTime Date { get; set; }
        public decimal Value { get; set; }
    }
}
