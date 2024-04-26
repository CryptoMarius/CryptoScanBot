namespace CryptoScanBot.Core.Context;

class TimeSpanHandler : SqliteTypeHandler<TimeSpan>
{
    public override TimeSpan Parse(object value) => TimeSpan.Parse((string)value);
}
