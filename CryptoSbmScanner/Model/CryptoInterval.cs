namespace CryptoSbmScanner.Model;

public enum CryptoIntervalPeriod
{
    interval1m, //0
    interval2m, //1
    interval3m, //2
    interval5m, //3
    interval10m, //4
    interval15m, //5
    interval30m, //6
    interval1h, //7
    interval2h, //8
    interval3h, //9
    interval4h, //10
    interval6h, //11
    interval8h, //12
    interval12h, //13
    interval1d //14
}

public class CryptoInterval
{
    public string Name { get; set; }
    public CryptoIntervalPeriod IntervalPeriod { get; set; }

    // Number of seconds for this interval
    public long Duration { get; set; }

    //[JsonIgnore]
    public CryptoInterval ConstructFrom { get; set; }


    public static CryptoInterval CreateInterval(CryptoIntervalPeriod intervalPeriod, string name, long duration, CryptoInterval constructFrom)
    {
        CryptoInterval cryptoInterval = new()
        {
            IntervalPeriod = intervalPeriod,
            Name = name,
            Duration = duration,
            ConstructFrom = constructFrom
        };
        return cryptoInterval;
    }
}