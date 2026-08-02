using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
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
            // First try named fields on SettingsSignal (ZonesDlz, ZonesFvg, ZonesSmc)
            FieldInfo? field = typeof(SettingsSignal).GetField(sectionName);
            if (field != null)
            {
                object sectionObj = field.GetValue(GlobalData.Settings.Signal)!;
                ApplyProps(sectionObj, props, saved);
                continue;
            }

            // Fall back to plugin settings in AnalyzerSettings (keyed by plugin name)
            IStrategyPlugin? plugin = PluginManager.LoadedPlugins.Values
                .FirstOrDefault(p => p.StrategyName.Equals(sectionName, StringComparison.OrdinalIgnoreCase));
            if (plugin != null)
            {
                ApplyProps(plugin.SettingsBase, props, saved);
                continue;
            }
        }

        object tradingObj = GlobalData.Settings.Trading;
        foreach (var (propPath, jsonVal) in entry.TradingOverrides)
            ApplyDottedProperty(tradingObj, propPath, jsonVal, saved);

        return saved;
    }

    private static void ApplyProps(object target, Dictionary<string, JsonElement> props, List<Override> saved)
    {
        foreach (var (propPath, jsonVal) in props)
            ApplyDottedProperty(target, propPath, jsonVal, saved);
    }

    /// <summary>
    /// Resolves a dotted property path (e.g. "EntryConditions.Ma200MinDistancePercentage")
    /// and sets the leaf value. Intermediate segments are navigated via reflection.
    /// </summary>
    private static void ApplyDottedProperty(object root, string propPath, JsonElement jsonVal, List<Override> saved)
    {
        string[] parts = propPath.Split('.');
        object current = root;

        for (int i = 0; i < parts.Length - 1; i++)
        {
            PropertyInfo? nav = current.GetType().GetProperty(parts[i]);
            if (nav == null)
                return;
            object? next = nav.GetValue(current);
            if (next == null)
            {
                next = Activator.CreateInstance(nav.PropertyType);
                if (next == null)
                    return;
                saved.Add(new Override(current, nav, null));
                nav.SetValue(current, next);
            }
            current = next;
        }

        PropertyInfo? leaf = current.GetType().GetProperty(parts[^1]);
        if (leaf == null)
            return;

        saved.Add(new Override(current, leaf, leaf.GetValue(current)));
        leaf.SetValue(current, ConvertJsonElement(jsonVal, leaf.PropertyType));
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

        if (element.ValueKind == JsonValueKind.Object)
            return JsonSerializer.Deserialize(element.GetRawText(), underlying,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

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
