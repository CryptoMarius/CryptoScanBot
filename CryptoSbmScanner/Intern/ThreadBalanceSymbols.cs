using System.Collections.Concurrent;

using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Intern;

public class ThreadBalanceSymbols
{
    private DateTime LastRunDate = DateTime.Now;
    public int BalanceCount = 0; //Tellertje die in de taakbalk c.q. applicatie titel komt (indicatie meldingen)
    private readonly BlockingCollection<CryptoSymbol> Queue = new();
    private readonly CancellationTokenSource cancellationToken = new();

    public void Stop()
    {
        cancellationToken.Cancel();

        GlobalData.AddTextToLogTab(string.Format("Stop balancing"));
    }

    public void AddToQueue(CryptoSymbol symbol)
    {
        Queue.Add(symbol);
    }

    /// <summary>
    /// Thread main handler
    /// </summary>
    public async Task ExecuteAsync()
    {
        foreach (CryptoSymbol symbol in Queue.GetConsumingEnumerable(cancellationToken.Token))
        {
            // Er is een nieuwe prijs beschikbaar van deze munt
            try
            {
                if (GlobalData.Settings.BalanceBot.Active && GlobalData.Settings.BalanceBot.IntervalPeriod > 0)
                {
                    // De prijs updates komen vaak en snel, iedere x seconden is genoeg!
                    DateTime nowDate = DateTime.Now;
                    double diffInSeconds = (nowDate - LastRunDate).TotalSeconds;
                    if (diffInSeconds >= GlobalData.Settings.BalanceBot.IntervalPeriod)
                    {
                        BalanceCount++;
                        LastRunDate = nowDate;

                        BalanceSymbolsAlgoritm balanceSymbolsAlgoritm = new(GlobalData.BinanceRealTradeAccount);
                        await balanceSymbolsAlgoritm.Execute();
                    }
                }

            }
            catch (Exception error)
            {
                // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
                GlobalData.Logger.Error(error);
                GlobalData.AddTextToLogTab("");
                GlobalData.AddTextToLogTab(symbol.Name + " error symbol balance thread" + error.ToString());
            }
        }
    }
}