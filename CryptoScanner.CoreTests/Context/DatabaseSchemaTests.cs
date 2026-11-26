using System.Reflection;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Model;

using Dapper;
using Dapper.Contrib.Extensions;

using Microsoft.Data.Sqlite;

namespace CryptoScanner.CoreTests.Context;

/// <summary>
/// Every column a model writes has to exist in the table that <see cref="CryptoDatabase.CreateTables"/>
/// creates, not only in the one a migration altered. The emulator drops Position (and the other run
/// tables) before every run and creates them again from CreateTables; on 21-11-2026 the column
/// StopLossDisabled (migration 115) was missing from that statement, so every position insert failed
/// with "table Position has no column named StopLossDisabled" and runs 2070-2092 ended with signals
/// but zero positions.
/// </summary>
[TestClass]
public class DatabaseSchemaTests
{
    [TestMethod]
    public void CreateTables_HasAColumnForEveryWrittenModelProperty()
    {
        // CreateTables inserts the hardcoded intervals and exchanges; the type handlers that
        // CryptoDatabase.CreateDatabase registers are needed for that
        SqlMapper.RemoveTypeMap(typeof(DateTimeHandler));
        SqlMapper.AddTypeHandler(new DateTimeHandler());
        SqlMapper.AddTypeHandler(new CandleTimeTypeHandler());
        SqlMapper.RemoveTypeMap(typeof(double));
        SqlMapper.RemoveTypeMap(typeof(double?));
        SqlMapper.AddTypeHandler(new NaNDoubleHandler());

        using var database = new CryptoDatabase();
        database.Connection.Dispose();
        database.Connection = new SqliteConnection("Data Source=:memory:");
        database.Connection.Open();
        CryptoDatabase.CreateTables(database);

        var models = typeof(CryptoPosition).Assembly.GetTypes()
            .Select(t => (Type: t, Table: t.GetCustomAttribute<TableAttribute>()))
            .Where(m => m.Table != null)
            .ToList();
        Assert.IsTrue(models.Any(m => m.Type == typeof(CryptoPosition)), "CryptoPosition has no [Table] attribute");

        List<string> missing = [];
        foreach (var (type, table) in models)
        {
            string tableName = table!.Name.Trim('[', ']');
            HashSet<string> columns = new(
                database.Connection.Query<string>($"select name from pragma_table_info('{tableName}')"),
                StringComparer.OrdinalIgnoreCase);
            if (columns.Count == 0)
            {
                missing.Add($"{type.Name}: table {tableName} is not created by CreateTables");
                continue;
            }

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!property.CanWrite || property.GetSetMethod() == null)
                    continue;
                if (property.GetCustomAttribute<ComputedAttribute>() != null)
                    continue;
                if (property.GetCustomAttribute<WriteAttribute>() is { Write: false })
                    continue;
                if (!columns.Contains(property.Name))
                    missing.Add($"{type.Name}.{property.Name}: no column in {tableName}");
            }
        }

        Assert.AreEqual(0, missing.Count, "Columns missing from CreateTables:\n" + string.Join("\n", missing));
    }
}
