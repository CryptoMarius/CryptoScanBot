using CryptoScanner.Core.Context;
using CryptoScanner.Core.Model;

using Dapper.Contrib.Extensions;

using Microsoft.Data.Sqlite;

using System.Collections.Concurrent;

namespace CryptoScanner.Core.Core;

// The database Sqlite is not the best when working with heavy multithreaded applications.
// There is basicly only one 1 write transaction allowed, that is not sufficient.
// (in effect (&currently) this goes only wrong for inserting signals in parrallel)
// In effect this is just a stupid workaround the database limitations
// Simple and effective but still kind of stupid..
// Extendable to other objects who dont require an immediatie id

public class ThreadSaveObjects
{

    private readonly BlockingCollection<object> Queue = [];
    private readonly CancellationTokenSource cancellationToken = new();


    public void Stop()
    {
        cancellationToken.Cancel();
        //GlobalData.AddTextToLogTab(string.Format("Stop saving objects"));
    }


    public void AddToQueue(object o)
    {
        Queue.Add(o);
    }


    /// <summary>
    /// Persist one queued object inside an existing transaction. Shared by the background
    /// <see cref="Execute"/> worker and the synchronous <see cref="Flush"/> path so the
    /// insert/update/delete rules (negative Id = delete, 0 = insert, positive = update) live
    /// in exactly one place.
    /// </summary>
    private static void WriteObject(CryptoDatabase databaseThread, SqliteTransaction transaction, object o)
    {
        if (o is CryptoSignal signal)
        {
            if (signal.Id == 0)
                databaseThread.Connection.Insert(signal, transaction);
            else
                databaseThread.Connection.Update(signal, transaction);
        }
        else if (o is Model.CryptoExchange exchange)
        {
            if (exchange.Id == 0)
                databaseThread.Connection.Insert(exchange, transaction);
            else
                databaseThread.Connection.Update(exchange, transaction);
        }
        else if (o is CryptoSymbol symbol)
        {
            if (symbol.Id < 0)
            {
                symbol.Id = Math.Abs(symbol.Id);
                databaseThread.Connection.Delete(symbol, transaction);
            }
            else if (symbol.Id == 0)
                databaseThread.Connection.Insert(symbol, transaction);
            else
                databaseThread.Connection.Update(symbol, transaction);
        }
        else if (o is CryptoPosition position)
        {
            if (position.Id == 0)
                databaseThread.Connection.Insert(position, transaction);
            else
                databaseThread.Connection.Update(position, transaction);
        }
        else if (o is CryptoZone zone)
        {
            if (zone.Id < 0)
            {
                zone.Id = Math.Abs(zone.Id);
                databaseThread.Connection.Delete(zone, transaction);
            }
            else if (zone.Id == 0)
            {
                // Tag every new zone with the run that created it (NULL when live). Single chokepoint so
                // all zone sources (DLZ/FVG/SMC) are covered without touching each creation site. During
                // a backtest CurrentEmulatorRunId is the active run; LoadZonesForSymbol then loads only
                // that run's zones, keeping runs isolated and reproducible.
                zone.EmulatorRunId = GlobalData.CurrentEmulatorRunId;
                databaseThread.Connection.Insert(zone, transaction);
            }
            else
                databaseThread.Connection.Update(zone, transaction);
        }
    }


    /// <summary>
    /// Synchronously writes everything currently queued and returns once the queue is empty.
    /// The live scanner persists on a background thread (<see cref="Execute"/>) with a 250 ms
    /// batch delay, but the emulator needs the DB to be current the moment it is read again:
    /// <see cref="Zones.ZoneDlz.LoadZonesForSymbol"/> resets and reloads every zone from the DB
    /// on each zone calculation, so a zone diff queued this tick MUST be on disk before the next
    /// LoadZonesForSymbol runs, otherwise it is reset away. The emulator therefore calls Flush()
    /// on the replay thread at the tick boundaries instead of starting the Execute worker. It is
    /// a no-op (and opens no DB connection) when nothing was queued, so an empty tick stays cheap.
    /// </summary>
    public void Flush()
    {
        if (Queue.Count == 0)
            return;

        List<object> list = [];
        while (Queue.TryTake(out object? x))
            list.Add(x);
        if (list.Count == 0)
            return;

        using CryptoDatabase databaseThread = new();
        databaseThread.Open();
        var transaction = databaseThread.BeginTransaction();
        try
        {
            foreach (var o in list)
                WriteObject(databaseThread, transaction, o);
            transaction.Commit();
        }
        catch (Exception error)
        {
            transaction.Rollback();
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab($"ThreadSaveObjects (flush) ERROR {error.Message}");
        }
    }


    public void Execute()
    {
        //GlobalData.AddTextToLogTab("Starting task for saving objects");
        try
        {
            CryptoDatabase databaseThread = new();
            try
            {
                databaseThread.Open();
                foreach (object obj in Queue.GetConsumingEnumerable(cancellationToken.Token))
                {
                    // try to take multiple items (because the transaction is expensive)
                    List<object> list = [];
                    list.Add(obj);
                    while (Queue.Count > 0 && Queue.TryTake(out object? x))
                        list.Add(x);


                    var transaction = databaseThread.BeginTransaction();
                    try
                    {
                        foreach (var o in list)
                            WriteObject(databaseThread, transaction, o);
                        transaction.Commit();
                    }
                    catch (Exception error)
                    {
                        transaction.Rollback();
                        ScannerLog.Logger.Error(error, "");
                    }
                    Thread.Sleep(250);
                }
            }
            finally
            {
                databaseThread.Close();
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