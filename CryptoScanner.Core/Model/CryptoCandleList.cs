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

    public void Lock()
    {
        _lock.EnterWriteLock();
    }

    public void Unlock()
    {
        _lock.ExitWriteLock();
    }

    // Thread-safe indexer: getter uses read lock, setter uses write lock.
    // Without this override, direct assignment (e.g. candles[key] = value) bypasses the lock,
    // causing InvalidOperationException in concurrent enumerators (version mismatch).
    public new CryptoCandle this[CandleTime key]
    {
        get
        {
            _lock.EnterReadLock();
            try { return base[key]; }
            finally { _lock.ExitReadLock(); }
        }
        set
        {
            _lock.EnterWriteLock();
            try { base[key] = value; }
            finally { _lock.ExitWriteLock(); }
        }
    }

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

    // Thread-safe access to the first candle (lowest key).
    // Callers must NOT use candleList.Values.First() — that enumerates without the read lock.
    public bool TryGetFirstCandle(out CryptoCandle candle)
    {
        _lock.EnterReadLock();
        try
        {
            using var e = base.GetEnumerator();
            if (e.MoveNext())
            {
                candle = e.Current.Value;
                return true;
            }
            candle = default;
            return false;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    // Thread-safe access to the last candle (highest key).
    // Callers must NOT use candleList.Values.Last() — that enumerates without the read lock.
    public bool TryGetLastCandle(out CryptoCandle candle)
    {
        _lock.EnterReadLock();
        try
        {
            if (Count == 0)
            {
                candle = default;
                return false;
            }
            using var e = base.GetEnumerator();
            CryptoCandle last = default;
            while (e.MoveNext())
                last = e.Current.Value;
            candle = last;
            return true;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    // Thread-safe snapshot of all entries, ordered by key (ascending, matching SortedDictionary order).
    // Use this instead of direct LINQ enumeration (e.g. .OrderBy/.Select/.ToList) to avoid
    // ArgumentException / InvalidOperationException when another thread calls Add() concurrently.
    //
    // NOTE: new List<>(this) must NOT be used here. The List<T>(ICollection<T>) constructor calls
    // ICollection.Count once for pre-allocation and then SortedSet.CopyTo re-reads Count internally.
    // When Count increases between those two reads (e.g. concurrent Add via a base-type reference),
    // SortedSet.CopyTo throws ArgumentException: "Destination array is not long enough".
    // Enumerating manually avoids both Count reads and the CopyTo path entirely.
    public List<KeyValuePair<CandleTime, CryptoCandle>> GetSnapshot()
    {
        _lock.EnterReadLock();
        try
        {
            var snapshot = new List<KeyValuePair<CandleTime, CryptoCandle>>(base.Count);
            using var e = base.GetEnumerator();
            while (e.MoveNext())
                snapshot.Add(e.Current);
            return snapshot;
        }
        finally
        {
            _lock.ExitReadLock();
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
