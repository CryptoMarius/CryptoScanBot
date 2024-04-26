using System.Data;
using Dapper;

namespace CryptoScanBot.Core.Context;

// Dapper slaat de kind van een datum niet op waardoor de UTC van slag is

// from https://stackoverflow.com/questions/12510299/get-datetime-as-utc-with-dapper

public class DateTimeHandler : SqlMapper.TypeHandler<DateTime>
{
    private static readonly DateTime unixOrigin = new(1970, 1, 1, 0, 0, 0, 0);


    public override void SetValue(IDbDataParameter parameter, DateTime value)
    {
        parameter.Value = value;
    }

    //public override DateTime Parse(object value)
    //{
    //    return DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc);
    //}

    public override DateTime Parse(object value)
    {
        if (!TryGetDateTime(value, out DateTime storedDateValue))
        {
            throw new InvalidOperationException($"Unable to parse value {value} as DateTimeOffset");
        }

        return DateTime.SpecifyKind(storedDateValue, DateTimeKind.Utc);
    }

    private static bool TryGetDateTime(object value, out DateTime dateTimeValue)
    {
        dateTimeValue = default;
        if (value is DateTime d)
        {
            dateTimeValue = d;
            return true;
        }

        if (value is string v)
        {
            dateTimeValue = DateTime.Parse(v);
            return true;
        }

        if (long.TryParse(value?.ToString() ?? string.Empty, out long l))
        {
            dateTimeValue = unixOrigin.AddSeconds(l);
            return true;
        }

        if (float.TryParse(value?.ToString() ?? string.Empty, out float _))
        {
            throw new InvalidOperationException("Unsupported Sqlite datetime type, REAL.");
        }

        return false;
    }
}
