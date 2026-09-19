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
        try
        {
            ApplyCore(entry, saved);
            return saved;
        }
        catch
        {
            // Half-applied overrides would leak into every later run of the batch, silently
            // measuring something nobody asked for. Put back what was already set before handing
            // the problem to the caller.
            Revert(saved);
            throw;
        }
    }


    private static void ApplyCore(EmulatorQueueEntry entry, List<Override> saved)
    {
        // Before anything is set: an entry condition that cannot reach its strategy is a measurement
        // that silently is not one. Validate() asks the same question before the batch starts; this
        // is here for the callers that do not.
        string? unreachable = DescribeUnreachableEntryCondition(entry);
        if (unreachable != null)
            throw new NotSupportedException(unreachable);

        foreach (var (sectionName, props) in entry.SignalOverrides)
        {
            // "Signal" addresses SettingsSignal itself, for properties that do not live in one of
            // its sections - AnalysisEffectivePercentage or SymbolMustExistsDays for instance.
            // Without this those can only be changed in the settings file, not per queue entry.
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
            IStrategyPlugin? plugin = PluginManager.FindByName(sectionName);
            if (plugin != null)
            {
                ApplyProps(plugin.SettingsBase, props, saved);
                continue;
            }
        }

        object tradingObj = GlobalData.Settings.Trading;
        foreach (var (propPath, jsonVal) in entry.TradingOverrides)
            ApplyDottedProperty(tradingObj, propPath, jsonVal, saved);
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
        ["AnalysisBandRangeIndexCheck"] =
            "the band range index was removed on 05-09-2026. On the dbr setup that earns money it "
            + "turned +547,86 USDT over 2.294 positions into -239,04 over 1.404 (threshold 2.0) and "
            + "-103,56 over 528 (threshold 3.5): fewer trades AND worse ones, the profit per position "
            + "going from +0,24 to -0,17",
    };

    /// <summary>
    /// Stops the run when a queue entry sets a retired setting to anything but "off". Off is an
    /// empty list, false, or zero - the value the entry would have had with the setting still in
    /// place, so nothing is lost by ignoring it.
    /// </summary>
    private static void RejectRetiredProperty(string propPath, JsonElement jsonVal)
    {
        string? reason = DescribeRetiredProperty(propPath, jsonVal);
        if (reason != null)
            throw new NotSupportedException(reason);
    }


    /// <summary>
    /// Why this one override cannot be applied, or null when it can. Split out of
    /// <see cref="RejectRetiredProperty"/> so <see cref="Validate"/> can ask the same question
    /// without throwing and without touching a setting.
    /// </summary>
    private static string? DescribeRetiredProperty(string propPath, JsonElement jsonVal)
    {
        string path = propPath;
        foreach (var (retired, reason) in RetiredProperties)
        {
            // Also catches a child of a retired object, e.g. EntryPatternShape.MinWickPercentage.
            if (!path.Equals(retired, StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith(retired + ".", StringComparison.OrdinalIgnoreCase))
                continue;

            if (IsSwitchedOff(jsonVal))
                return null;

            return $"Queue entry sets \"{propPath}\", but {reason}.";
        }

        return null;
    }


    /// <summary>
    /// Whether this value is the setting in its "off" position - an empty list, false, or zero.
    /// Asking for a setting that cannot be applied is a hard stop; switching one off is harmless and
    /// stays silent, because the entry would have measured the same thing either way.
    /// </summary>
    private static bool IsSwitchedOff(JsonElement jsonVal)
    {
        return jsonVal.ValueKind switch
        {
            JsonValueKind.Array => jsonVal.GetArrayLength() == 0,
            JsonValueKind.False or JsonValueKind.Null or JsonValueKind.Undefined => true,
            JsonValueKind.Number => jsonVal.TryGetDecimal(out decimal d) && d == 0m,
            _ => false,
        };
    }


    /// <summary>
    /// Why an entry condition in this entry's TRADING overrides will never be seen by the strategy
    /// it is meant for, or null when there is nothing wrong.
    /// <para>
    /// A strategy may bring its own <c>EntryConditions</c> - mac, bbsqueeze and kumosqueeze do - and
    /// SignalBase.ResolveEntryConditions then prefers that set over
    /// <c>GlobalData.Settings.Trading.EntryConditions</c>. An entry that switches a condition on in
    /// the trading overrides therefore changes nothing for such a strategy, and nothing says so: the
    /// run completes, the stored settings show the condition as ON, and the result is bit-identical
    /// to the run without it. Four runs of 17-09-2026 were spent that way before the identical
    /// numbers gave it away.
    /// </para>
    /// <para>
    /// Only an entry that names its <see cref="EmulatorQueueEntry.Algorithm"/> can be judged, which
    /// is every entry the folder queue accepts. Switching a condition OFF is left alone: that is
    /// what the strategy's own set already says.
    /// </para>
    /// </summary>
    private static string? DescribeUnreachableEntryCondition(EmulatorQueueEntry entry)
    {
        if (string.IsNullOrEmpty(entry.Algorithm))
            return null;

        IStrategyPlugin? plugin = PluginManager.FindByName(entry.Algorithm)
            ?? PluginManager.LoadedPlugins.Values.FirstOrDefault(p =>
                p.Strategies.Any(s => s.Name.Equals(entry.Algorithm, StringComparison.OrdinalIgnoreCase)));
        if (plugin?.SettingsBase.EntryConditions == null)
            return null;

        foreach (var (propPath, jsonVal) in entry.TradingOverrides)
        {
            if (!propPath.StartsWith("EntryConditions.", StringComparison.OrdinalIgnoreCase))
                continue;
            if (IsSwitchedOff(jsonVal))
                continue;

            return $"Queue entry switches \"{propPath}\" on through the trading overrides, but "
                + $"{plugin.StrategyName} has entry conditions of its own and never reads those. "
                + $"Move it to the signal overrides: \"SignalOverrides\": {{ \"{plugin.StrategyName}\": "
                + $"{{ \"{propPath}\": ... }} }}";
        }

        return null;
    }


    /// <summary>
    /// Why this entry cannot run, or null when it can. Runs the same checks <see cref="Apply"/>
    /// performs, but without setting anything, so a whole queue can be inspected BEFORE the first
    /// run instead of dying halfway through.
    /// <para>
    /// That is not a theoretical worry: on 03-09-2026 entry 26 of 54 asked for the retired
    /// EntryWaitForPatterns, the exception left <see cref="Apply"/> unhandled and took the process
    /// with it at 03:45, and the 28 entries behind it never ran. A batch meant to run unattended has
    /// to say what it cannot do at the start, when someone is still watching.
    /// </para>
    /// </summary>
    public static string? Validate(EmulatorQueueEntry entry)
    {
        foreach (var (_, props) in entry.SignalOverrides)
        {
            foreach (var (propPath, jsonVal) in props)
            {
                string? reason = DescribeRetiredProperty(propPath, jsonVal);
                if (reason != null)
                    return reason;
            }
        }

        foreach (var (propPath, jsonVal) in entry.TradingOverrides)
        {
            string? reason = DescribeRetiredProperty(propPath, jsonVal);
            if (reason != null)
                return reason;
        }

        return DescribeUnreachableEntryCondition(entry);
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
