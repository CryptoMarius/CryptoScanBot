using CryptoScanner.Core.Contracts;
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
            // "Signal" addresses SettingsSignal itself, for properties that do not live in one of
            // its sections - AnalysisBandRangeIndexCheck / AnalysisMinBandRangeIndex for instance.
            // Without this there is no way to switch the band range index filter per queue entry.
            if (sectionName.Equals("Signal", StringComparison.OrdinalIgnoreCase))
            {
                ApplyProps(GlobalData.Settings.Signal, props, saved);
                continue;
            }

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
        RejectRetiredProperty(propPath, jsonVal);

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

    /// <summary>
    /// Settings that have been removed from the code but are still spelled out in older queue
    /// entries. A path that is simply gone is skipped without a word by the loop below - and a rule
    /// that asks for a filter which then never runs reads exactly like a strategy that produced
    /// nothing. Switching one OFF is harmless and stays silent, so the many entries that carry
    /// "EntryConditions.EntryWaitForPatterns": [] keep working; asking for it is a hard stop.
    /// </summary>
    private static readonly Dictionary<string, string> RetiredProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        ["EntryConditions.EntryWaitForPatterns"] =
            "waiting for a reversal shape was removed on 02-09-2026 (measured on runs 532-568, "
            + "616-618 and 690-705: it lost money on every strategy). Use the CandlePattern strategy "
            + "instead, which trades the shape itself",
        ["EntryConditions.EntryPatternShape"] = "the shape thresholds went with EntryWaitForPatterns",
    };

    /// <summary>
    /// Stops the run when a queue entry sets a retired setting to anything but "off". Off is an
    /// empty list, false, or zero - the value the entry would have had with the setting still in
    /// place, so nothing is lost by ignoring it.
    /// </summary>
    private static void RejectRetiredProperty(string propPath, JsonElement jsonVal)
    {
        string path = propPath;
        foreach (var (retired, reason) in RetiredProperties)
        {
            // Also catches a child of a retired object, e.g. EntryPatternShape.MinWickPercentage.
            if (!path.Equals(retired, StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith(retired + ".", StringComparison.OrdinalIgnoreCase))
                continue;

            bool isOff = jsonVal.ValueKind switch
            {
                JsonValueKind.Array => jsonVal.GetArrayLength() == 0,
                JsonValueKind.False or JsonValueKind.Null or JsonValueKind.Undefined => true,
                JsonValueKind.Number => jsonVal.TryGetDecimal(out decimal d) && d == 0m,
                _ => false,
            };
            if (isOff)
                return;

            throw new NotSupportedException($"Queue entry sets \"{propPath}\", but {reason}.");
        }
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

        // A list-valued setting, such as the interval list of a zone strategy
        // (ZonesDlz/ZonesFvg/ZonesSmc.IntervalList). Without this an array in the queue file
        // ends in the NotSupportedException below, so those settings could only be changed in
        // the settings file - which is exactly what left the zone strategies without intervals.
        if (element.ValueKind == JsonValueKind.Array)
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
