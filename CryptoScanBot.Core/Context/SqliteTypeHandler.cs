﻿using System.Data;
using Dapper;

namespace CryptoScanBot.Core.Context;

// From https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/dapper-limitations

abstract class SqliteTypeHandler<T> : SqlMapper.TypeHandler<T>
{
    // Parameters are converted by Microsoft.Data.Sqlite
    public override void SetValue(IDbDataParameter parameter, T? value) => parameter.Value = value;
}
