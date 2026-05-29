using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;

namespace CryptoScanner.Commands;

public class CommandCleanOrphanCandleFiles : CommandBase
{
    public override void Execute(object? parameter)
    {
        // Execute runs on the UI thread when the menu fires. Both cleanup routines start
        // with an "await Semaphore.WaitAsync()" that almost always completes synchronously,
        // followed by largely synchronous SQLite / filesystem work. Without Task.Run the
        // whole chain would unwind on the UI thread before the await suspends — the menu
        // would stay open and the application would freeze. Task.Run hops the chain onto
        // the threadpool so Execute returns immediately, the menu can close, and the work
        // continues in the background.
        _ = Task.Run(() => ExecuteAsync(parameter));
    }

    public async Task ExecuteAsync(object? parameter)
    {
        if (GlobalData.ActiveExchange == null)
            return;

        try
        {
            // First the DB-level cleanup (deletes orphan candle rows + incremental_vacuum)
            await CandleDatabase.CleanCandlesAsync();

            // Then the filesystem sweep (exchange/quote folders + legacy Pivots folder)
            await DataStore.CleanOrphanCandleFilesAsync();
        }
        catch (Exception err)
        {
            ScannerLog.Logger.Error(err, "orphan cleanup command failed");
            GlobalData.AddTextToLogTab($"orphan cleanup failed: {err.Message}");
        }
    }
}