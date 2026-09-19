using CryptoScanner.Analyzers;
using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;
using CryptoScanner.UI.Models;

using System.Reflection;

namespace CryptoScanner.CoreTests.Settings;

/// <summary>
/// The two hosts build their strategy tab in opposite ways, and that is what makes them drift
/// apart. Photino generates its screen from the settings class by reflection, so a new property is
/// on screen the next build. Avalonia has a hand-built view per strategy, where the same property
/// has to be added to the view, the viewmodel and its Load/Save by hand. Nothing breaks when that
/// is forgotten: the setting is simply unreachable in one of the two, which is how eight of them
/// went unnoticed until 19-09-2026.
/// <para>
/// These tests compare both halves against the settings class itself, so the next one is noticed
/// the moment it is added rather than months later.
/// </para>
/// </summary>
[TestClass]
public class StrategyTabParityTests
{
    /// <summary>
    /// Settings that deliberately have no editor on one of the two sides, as
    /// "&lt;strategy&gt;.&lt;property&gt;". Add one here WITH the reason, never to silence a test
    /// that is telling the truth.
    /// </summary>
    private static readonly HashSet<string> NoEditor = new(StringComparer.OrdinalIgnoreCase)
    {
    };

    /// <summary>
    /// The seven base settings both hosts edit through a dedicated control rather than through the
    /// generated list - the same set PluginSettingsEditState keeps out of its reflection walk.
    /// </summary>
    private static readonly string[] DedicatedEditors =
    [
        nameof(SettingsSignalStrategyBase.EntryConditions),
        nameof(SettingsSignalStrategyBase.PlaySound),
        nameof(SettingsSignalStrategyBase.PlaySpeech),
        nameof(SettingsSignalStrategyBase.ColorLong),
        nameof(SettingsSignalStrategyBase.ColorShort),
        nameof(SettingsSignalStrategyBase.SoundFileLong),
        nameof(SettingsSignalStrategyBase.SoundFileShort),
    ];


    private static List<IStrategyPlugin> RegisteredPlugins()
    {
        TestBase.InitTestSession();
        AnalyzerRegistration.RegisterAll();

        // Keyed per strategy name, and one plugin can serve several of them (choch has four).
        // A plugin that registers no strategy at all - doubletopbottom - is not in here, and it gets
        // no tab in either host, which is why it is not missing from anything.
        return [.. PluginManager.LoadedPlugins.Values.Distinct()];
    }


    /// <summary>
    /// A plugin whose ConfigView is null has no tab in the Avalonia window at all, while Photino
    /// shows one for every loaded plugin. Nothing warns about it: the strategy simply is not there.
    /// That is how nwe, trend, choch, bbrsiengulfing, ichimoku.kumo.breakout and supertrendbreakout
    /// ended up editable in one host only.
    /// </summary>
    [TestMethod]
    public void EveryRegisteredStrategyHasAnAvaloniaTab()
    {
        var missing = RegisteredPlugins()
            .Where(p => p.ConfigView == null)
            .Select(p => p.StrategyName)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.AreEqual(0, missing.Count,
            $"no Avalonia tab (IConfigView is null) for: {string.Join(", ", missing)}. "
            + "Photino does show these, so the setting is reachable in one host only.");
    }


    /// <summary>
    /// The heading every Avalonia tab carries. Photino prints the same two strings, which it can
    /// only do when the config view hands them over.
    /// </summary>
    [TestMethod]
    public void EveryAvaloniaTabNamesItselfForTheOtherHost()
    {
        var missing = RegisteredPlugins()
            .Where(p => p.ConfigView != null && string.IsNullOrWhiteSpace(p.ConfigView.StrategyTitle))
            .Select(p => p.StrategyName)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.AreEqual(0, missing.Count,
            $"IConfigView.StrategyTitle is empty for: {string.Join(", ", missing)}. "
            + "Without it the strategy has no heading in the Blazor hosts.");
    }


    /// <summary>
    /// Every setting has to be read and written by the Avalonia viewmodel of its strategy. Load the
    /// defaults into the tab, hand it a settings object in which every single value has been
    /// changed, and let it save: a value the tab knows is overwritten with the one it loaded, and a
    /// value it does not know keeps the changed one. Whatever is still changed afterwards has no
    /// editor on that side.
    /// </summary>
    [TestMethod]
    public void TheAvaloniaTabReadsAndWritesEverySetting()
    {
        var complaints = new List<string>();

        foreach (var plugin in RegisteredPlugins())
        {
            IConfigView? configView = plugin.ConfigView;
            if (configView == null)
                continue;   // reported by the test above

            Type type = plugin.SettingsBase.GetType();
            var reference = (SettingsSignalStrategyBase)Activator.CreateInstance(type)!;
            var probe = (SettingsSignalStrategyBase)Activator.CreateInstance(type)!;

            // Never the live settings of the plugin: ToConcrete uses the object it is handed.
            configView.LoadConfig(reference);
            var changed = ChangeEverything(probe);
            configView.SaveConfig(probe);

            foreach (var leaf in changed)
            {
                if (NoEditor.Contains($"{plugin.StrategyName}.{leaf.Path}"))
                    continue;
                if (SameValue(leaf.Read(probe), leaf.Read(reference)))
                    continue;

                complaints.Add($"{plugin.StrategyName}.{leaf.Path} is not on the Avalonia tab "
                    + $"(it survived the save as {Describe(leaf.Read(probe))})");
            }
        }

        Assert.AreEqual(0, complaints.Count,
            "settings the Avalonia strategy tab does not edit:\n" + string.Join("\n", complaints));
    }


    /// <summary>
    /// And the other way round: the reflection based editor of the Blazor hosts only draws a
    /// property it recognises, so one that lives in an object of its own stays invisible unless the
    /// settings class says Expand. That is what hid the "Zone calculation" box of dlz.
    /// </summary>
    [TestMethod]
    public void ThePhotinoTabShowsEverySetting()
    {
        var complaints = new List<string>();

        foreach (var plugin in RegisteredPlugins())
        {
            var state = new PluginSettingsEditState(plugin);

            var covered = new HashSet<string>(DedicatedEditors, StringComparer.Ordinal);
            foreach (var field in state.Fields)
            {
                // "Shape.MinWickPercentage" for a setting out of an expanded block: the block itself
                // is what the settings class declares, so that is the name to tick off.
                int dot = field.Name.IndexOf('.');
                covered.Add(dot < 0 ? field.Name : field.Name[..dot]);
            }

            foreach (var property in Editable(plugin.SettingsBase.GetType()))
            {
                if (covered.Contains(property.Name))
                    continue;
                if (NoEditor.Contains($"{plugin.StrategyName}.{property.Name}"))
                    continue;

                complaints.Add($"{plugin.StrategyName}.{property.Name} ({property.PropertyType.Name}) "
                    + "is not on the Photino tab; a settings block of its own needs "
                    + "[SettingCaption(..., Expand = true)]");
            }
        }

        Assert.AreEqual(0, complaints.Count,
            "settings the Blazor strategy tab does not draw:\n" + string.Join("\n", complaints));
    }


    // ------------------------------------------------------------------ the walk over the settings

    /// <summary>One value of a settings object, and how to read it out of another one of the same type.</summary>
    private sealed class Leaf(string path, PropertyInfo property, PropertyInfo? owner)
    {
        public string Path { get; } = path;

        public object? Read(object settings)
        {
            object? target = owner == null ? settings : owner.GetValue(settings);
            return target == null ? null : property.GetValue(target);
        }

        public void Write(object settings, object? value)
        {
            object? target = owner == null ? settings : owner.GetValue(settings);
            if (target != null)
                property.SetValue(target, value);
        }
    }


    private static IEnumerable<PropertyInfo> Editable(Type type)
        => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
               .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0);


    /// <summary>
    /// Give every setting a value it did not have, one level deep - the same depth the Blazor editor
    /// reaches with Expand. Returns the leaves it managed to change; a type it cannot make a second
    /// value for is left alone, because an unchanged value proves nothing either way.
    /// </summary>
    private static List<Leaf> ChangeEverything(object settings)
    {
        var leaves = new List<Leaf>();

        foreach (var property in Editable(settings.GetType()))
        {
            object? current = property.GetValue(settings);

            // A block of settings in an object of its own (the zigzag of dlz, the shape of
            // candlepattern): change what is inside it rather than replacing the object, because the
            // viewmodel writes straight into the object the settings hand it.
            if (current != null && IsSettingsBlock(property.PropertyType))
            {
                foreach (var child in Editable(property.PropertyType))
                {
                    object? childValue = child.GetValue(current);
                    object? other = Different(childValue, child.PropertyType);
                    if (other == null && childValue == null)
                        continue;
                    child.SetValue(current, other);
                    leaves.Add(new Leaf($"{property.Name}.{child.Name}", child, property));
                }
                continue;
            }

            object? changed = Different(current, property.PropertyType);
            if (changed == null && current == null)
                continue;
            property.SetValue(settings, changed);
            leaves.Add(new Leaf(property.Name, property, null));
        }

        return leaves;
    }


    /// <summary>A settings block of our own: a class of ours that has settings in it, not a list or a string.</summary>
    private static bool IsSettingsBlock(Type type)
        => type.IsClass
           && type != typeof(string)
           && !typeof(System.Collections.IEnumerable).IsAssignableFrom(type)
           && (type.Namespace?.StartsWith("CryptoScanner", StringComparison.Ordinal) ?? false)
           && Editable(type).Any();


    /// <summary>A value of this type that differs from the one given, or null when there is none.</summary>
    private static object? Different(object? value, Type type)
    {
        Type bare = Nullable.GetUnderlyingType(type) ?? type;

        if (bare == typeof(bool))
            return !(value as bool? ?? false);
        if (bare == typeof(string))
            return (value as string ?? "") + "~";
        if (bare == typeof(int))
            return (value as int? ?? 0) + 7;
        if (bare == typeof(long))
            return (value as long? ?? 0) + 7;
        if (bare == typeof(double))
            return (value as double? ?? 0) + 7.5;
        if (bare == typeof(float))
            return (value as float? ?? 0) + 7.5f;
        if (bare == typeof(decimal))
            return (value as decimal? ?? 0) + 7.5m;

        if (bare.IsEnum)
        {
            foreach (object member in Enum.GetValues(bare))
            {
                if (value == null || !member.Equals(value))
                    return member;
            }
            return null;    // a one-member enum has no second value
        }

        if (bare == typeof(List<string>))
            return new List<string> { "~not-a-setting~" };

        // Anything else - an entry condition block that starts out null, a colour: a fresh one is a
        // different one, as long as it can be made without arguments.
        if (bare.GetConstructor(Type.EmptyTypes) != null)
        {
            object fresh = Activator.CreateInstance(bare)!;
            return SameValue(fresh, value) ? null : fresh;
        }

        return null;
    }


    private static bool SameValue(object? left, object? right)
    {
        if (left is List<string> leftList && right is List<string> rightList)
        {
            // The interval box writes its list back in its own order (days, hours, minutes) and the
            // pattern list in the order the enum declares, so only the contents can be compared.
            return leftList.Count == rightList.Count
                && !leftList.Except(rightList, StringComparer.OrdinalIgnoreCase).Any();
        }
        return Equals(left, right);
    }


    private static string Describe(object? value)
        => value switch
        {
            null => "null",
            List<string> list => "[" + string.Join(", ", list) + "]",
            _ => value.ToString() ?? "",
        };
}
