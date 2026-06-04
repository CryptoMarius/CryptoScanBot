using Dapper;

using System.Data;
using System.Globalization;

namespace CryptoScanner.Core.Context;

public class NaNDoubleHandler : SqlMapper.TypeHandler<double>
{
    public override void SetValue(IDbDataParameter parameter, double value)
    {
        parameter.Value = double.IsNaN(value) ? DBNull.Value : value;
    }

    public override double Parse(object value)
    {
        // BUGFIX: Convert.ToDouble(value) without an explicit IFormatProvider routes
        // through the current thread culture. On a Dutch-culture machine that parses
        // "5964473.50624" as broken (',' is the decimal separator). All values stored
        // by Dapper/SQLite use '.' as decimal separator → always parse with
        // InvariantCulture.
        if (value == null || value is DBNull) return double.NaN; // of 0
        if (value is double d) return d;
        if (value is float f) return f;
        if (value is decimal dec) return (double)dec;
        if (value is long lg) return lg;
        if (value is int i) return i;
        if (value is string s)
        {
            if (string.IsNullOrEmpty(s)) return double.NaN;
            return double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);
        }
        return Convert.ToDouble(value, CultureInfo.InvariantCulture);
    }
}

