using CryptoScanner.Core.Core;
using CryptoScanner.Core.Settings;

using System.Reflection;
using System.Text.Json;

namespace CryptoScanner.Emulator.Engine;

/// <summary>
/// Applies and reverts signal/trading overrides from <see cref="EmulatorQueueEntry"/> onto
/// GlobalData.Settings via reflection.
/// </summary>
public static class SignalGridExpander
{
    public readonly record struct Override(object Target, PropertyInfo Property, object? SavedValue);

    /// <summary>
    /// Applies signal and trading overrides from a queue entry onto GlobalData.Settings.
    /// Returns a list of overrides that can be passed to <see cref="Revert"/> to restore
    /// the original values.
    /// </summary>
    public static List<Override> Apply(EmulatorQueueEntry entry)
    {
        var saved = new List<Override>();

        foreach (var (sectionName, props) in entry.SignalOverrides)
        {
            FieldInfo? field = typeof(SettingsSignal).GetField(sectionName);
            if (field == null)
                continue;

            object sectionObj = field.GetValue(GlobalData.Settings.Signal)!;
            Type sectionType = sectionObj.GetType();

            foreach (var (propName, jsonVal) in props)
            {
                PropertyInfo? prop = sectionType.GetProperty(propName);
                if (prop == null)
                    continue;

                saved.Add(new Override(sectionObj, prop, prop.GetValue(sectionObj)));
                prop.SetValue(sectionObj, ConvertJsonElement(jsonVal, prop.PropertyType));
            }
        }

        object tradingObj = GlobalData.Settings.Trading;
        Type tradingType = tradingObj.GetType();

        foreach (var (propName, jsonVal) in entry.TradingOverrides)
        {
            PropertyInfo? prop = tradingType.GetProperty(propName);
            if (prop == null)
                continue;

            saved.Add(new Override(tradingObj, prop, prop.GetValue(tradingObj)));
            prop.SetValue(tradingObj, ConvertJsonElement(jsonVal, prop.PropertyType));
        }

        return saved;
    }

    /// <summary>Revert overrides to their saved values.</summary>
    public static void Revert(List<Override> overrides)
    {
        foreach (var ov in overrides)
            ov.Property.SetValue(ov.Target, ov.SavedValue);
    }


    private static object? ConvertJsonElement(JsonElement element, Type targetType)
    {
        Type underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlying.IsEnum)
        {
            if (element.ValueKind == JsonValueKind.Number)
                return Enum.ToObject(underlying, element.GetInt32());
            if (element.ValueKind == JsonValueKind.String)
                return Enum.Parse(underlying, element.GetString()!, ignoreCase: true);
        }

        return underlying switch
        {
            _ when underlying == typeof(int) => element.GetInt32(),
            _ when underlying == typeof(double) => element.GetDouble(),
            _ when underlying == typeof(float) => (float)element.GetDouble(),
            _ when underlying == typeof(decimal) => element.GetDecimal(),
            _ when underlying == typeof(bool) => element.GetBoolean(),
            _ when underlying == typeof(string) => element.GetString(),
            _ when underlying == typeof(long) => element.GetInt64(),
            _ => throw new NotSupportedException($"Cannot convert JsonElement to {targetType.Name}")
        };
    }
}
