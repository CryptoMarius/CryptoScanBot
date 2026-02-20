namespace CryptoScanner.Core.Model;

// More or less thread safe (no need for the expensive ToList())
public class CryptoCandleList : SortedDictionary<CandleTime, CryptoCandle> // experiment via SortedDictionary? SortedList TrimExcess!!
{
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.SupportsRecursion);

    //#if DEBUG
    //    // Tel huidige locks
    //    var readCount = _lock.RecursiveReadCount;
    //    var writeCount = _lock.RecursiveWriteCount;
    //    var upgradeCount = _lock.RecursiveUpgradeCount;

    //			if (readCount > 0 || writeCount > 0 || upgradeCount > 0)
    //			{
    //				System.Diagnostics.Debug.WriteLine($"⚠️ Recursive lock detected:");
    //				System.Diagnostics.Debug.WriteLine($"   Read locks: {readCount}");
    //				System.Diagnostics.Debug.WriteLine($"   Write locks: {writeCount}");
    //				System.Diagnostics.Debug.WriteLine($"   Upgrade locks: {upgradeCount}");
    //				System.Diagnostics.Debug.WriteLine(new System.Diagnostics.StackTrace());
    //			}
    //#endif

    // Thread-safe Add
    public new void Add(CandleTime key, CryptoCandle value)
    {
        _lock.EnterWriteLock();
        try
        {
            base.Add(key, value);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    // Thread-safe Remove
    public new bool Remove(CandleTime key)
    {
        _lock.EnterWriteLock();
        try
        {
            return base.Remove(key);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    // Thread-safe TryGetValue
    public new bool TryGetValue(CandleTime key, out CryptoCandle value)
    {
        _lock.EnterReadLock();
        try
        {
            return base.TryGetValue(key, out value);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    // Thread-safe clear
    public new void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            base.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    // Dispose lock
    public void Dispose()
    {
        _lock?.Dispose();
    }
}

//
// For a future Helperclass? (StringStream?)
//
//public void DumpSymbol()
//{
//    //Ter debug van een hardnekig probleem met het tonen van de signal
//    var csv = new StringBuilder();
//    var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7}", "OpenTime", "IntervalId", "Open", "High", "Low", "Close", "Volume");
//    csv.AppendLine(newLine);

//    Monitor.Enter(History);
//    try
//    {
//        for (int i = 0; i < History.Count; i++)
//        {
//            CryptoCandle candle = History[i];

//            newLine = string.Format("{0};{1};{2};{3};{4};{5};{6}",
//            candle.Time.ToString(),
//            candle.IntervalId.ToString(),
//            candle.Open.ToString(),
//            candle.High.ToString(),
//            candle.Low.ToString(),
//            candle.Close.ToString(),
//            candle.Volume.ToString());

//            csv.AppendLine(newLine);
//        }
//    }
//    finally
//    {
//        Monitor.Exit(History);
//    }
//    string filename = System.IO.Path.GetDirectoryName((System.Reflection.Assembly.GetEntryAssembly().Location));
//    filename = filename + @"\data\" + Symbol.Exchange.Name + @"\Candles\" + Interval.Name + @"\";
//    System.IO.Directory.CreateDirectory(filename);
//    System.IO.File.WriteAllText(filename + Symbol.Name + "-" + Interval.Name + ".csv", csv.ToString());
//}
