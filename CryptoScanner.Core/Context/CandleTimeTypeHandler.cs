using CryptoScanner.Core.Model;

using Dapper;

using System.Data;

namespace CryptoScanner.Core.Context;

public class CandleTimeTypeHandler : SqlMapper.TypeHandler<CandleTime>
{
    // Read from database (INTEGER/BIGINT → CandleTime)
    public override CandleTime Parse(object value)
    {
        if (value == null || value is DBNull)
            return CandleTime.MinValue;

        // SQLite stores as INTEGER (int64)
        if (value is long longValue)
            return new CandleTime((uint)longValue);

        if (value is int intValue)
            return new CandleTime((uint)intValue);

        if (value is uint uintValue)
            return new CandleTime(uintValue);

        if (value is string stringValue)
        {
            if (uint.TryParse(stringValue, out uint minutes))
                return new CandleTime(minutes);

            throw new InvalidCastException($"Cannot parse '{stringValue}' to CandleTime");
        }

        throw new InvalidCastException($"Cannot convert {value.GetType()} to CandleTime");
    }

    // Write to database (CandleTime → INTEGER)
    public override void SetValue(IDbDataParameter parameter, CandleTime value)
    {
        parameter.DbType = DbType.UInt32;
        parameter.Value = value.Minutes;
    }
}