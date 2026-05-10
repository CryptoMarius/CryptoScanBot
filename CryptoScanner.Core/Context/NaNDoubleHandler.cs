using Dapper;

using System.Data;

namespace CryptoScanner.Core.Context;

public class NaNDoubleHandler : SqlMapper.TypeHandler<double>
{
    public override void SetValue(IDbDataParameter parameter, double value)
    {
        parameter.Value = double.IsNaN(value) ? DBNull.Value : value;
    }

    public override double Parse(object value)
    {
        if (value == null || value is DBNull) return double.NaN; // of 0
        return Convert.ToDouble(value);
    }
}

