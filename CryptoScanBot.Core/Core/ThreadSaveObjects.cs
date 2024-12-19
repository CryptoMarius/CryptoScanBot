using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

using Dapper.Contrib.Extensions;

using System.Collections.Concurrent;

namespace CryptoScanBot.Core.Core;

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


    public void Execute()
    {
        //GlobalData.AddTextToLogTab("Starting task for saving objects");
        try
        {
            foreach (object o in Queue.GetConsumingEnumerable(cancellationToken.Token))
            {
                CryptoDatabase databaseThread = new();
                try
                {
                    databaseThread.Open();
                    var transaction = databaseThread.BeginTransaction();
                    try
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
                                databaseThread.Connection.Insert(zone, transaction);
                            else
                                databaseThread.Connection.Update(zone, transaction);
                        }
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
