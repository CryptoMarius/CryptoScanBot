using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

using Dapper.Contrib.Extensions;

using System.Collections.Concurrent;

namespace CryptoScanBot.Core.Intern;

// The database Sqlite is not the best when working with heavy multithreaded applications.
// There is basicly only one 1 write transaction allowed, that is not sufficient.
// (in effect (&currently) this goes only wrong for inserting signals in parrallel)
// In effect this is just a stupid workaround the database limitations
// Simple and effective but still kind of stupid..
// Extendable to other objects who dont require an immediatie id

public class ThreadSaveObjects
{

    private readonly BlockingCollection<CryptoSignal> Queue = [];
    private readonly CancellationTokenSource cancellationToken = new();


    public void Stop()
    {
        cancellationToken.Cancel();
        GlobalData.AddTextToLogTab(string.Format("Stop monitor candle"));
    }


    public void AddToQueue(CryptoSignal signal)
    {
        Queue.Add((signal));
    }


    public void Execute()
    {
        GlobalData.AddTextToLogTab("Starting task for saving objects");
        try
        {
            foreach (CryptoSignal signal in Queue.GetConsumingEnumerable(cancellationToken.Token))
            {
                CryptoDatabase databaseThread = new();
                try
                {
                    databaseThread.Open();
                    var transaction = databaseThread.BeginTransaction();
                    try
                    {
                        if (signal.Id == 0)
                            databaseThread.Connection.Insert(signal, transaction);
                        else
                            databaseThread.Connection.Update(signal, transaction);
                        transaction.Commit();
                    }
                    catch (Exception error)
                    {
                        transaction.Rollback();
                        ScannerLog.Logger.Error(error, "");
                    }
                }
                finally
                {
                    databaseThread.Close();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // niets..
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab($"ThreadSaveObjects ERROR {error.Message}");
        }

        GlobalData.AddTextToLogTab("ThreadSaveObjects thread exit");
    }
}
