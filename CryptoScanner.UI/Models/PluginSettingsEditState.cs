using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Settings.Strategy;

using System.Reflection;

namespace CryptoScanner.UI.Models;

/// <summary>
/// Editable snapshot of one analyzer plugin's settings.
/// <para>
/// The Avalonia configuration window hosts a plugin supplied Avalonia control per analyzer
/// (<see cref="IConfigView"/>), which the Blazor hosts cannot render. Instead of leaving those
/// parameters unreachable, the plugin specific properties are discovered by reflection on the
/// concrete settings class — they are plain int/double/bool/string/enum properties — and rendered
/// generically. The values end up in the very same settings object both hosts persist.
/// </para>
/// </summary>
public class PluginSettingsEditState
{
    /// <summary>Properties of the shared base class that already have dedicated editors.</summary>
    private static readonly HashSet<string> BaseProperties = new(StringComparer.Ordinal)
    {
        nameof(SettingsSignalStrategyBase.EntryConditions),
        nameof(SettingsSignalStrategyBase.PlaySound),
        nameof(SettingsSignalStrategyBase.PlaySpeech),
        nameof(SettingsSignalStrategyBase.ColorLong),
        nameof(SettingsSignalStrategyBase.ColorShort),
        nameof(SettingsSignalStrategyBase.SoundFileLong),
        nameof(SettingsSignalStrategyBase.SoundFileShort),
    };

    public IStrategyPlugin Plugin { get; }
    public string Name => Plugin.StrategyName;

    public bool PlaySound { get; set; }
    public bool PlaySpeech { get; set; }
    public string SoundFileLong { get; set; } = "";
    public string SoundFileShort { get; set; } = "";
    /// <summary>
    /// The colours in the same #AARRGGBB notation the settings file uses, so the shared
    /// ColorPickerCell can edit the transparency as well — the Avalonia ColorPicker has an alpha
    /// component, the browser's input[type=color] has none.
    /// </summary>
    public string ColorLongArgb { get; set; } = "#FFFF95A5";
    public string ColorShortArgb { get; set; } = "#FFFF95A5";

    /// <summary>
    /// Off means the strategy follows the global trader entry conditions; the settings object then
    /// holds null. Same rule as the Avalonia StrategyEntryConditionsViewModel.
    /// </summary>
    public bool UseCustomEntryConditions { get; set; }

    public EntryConditionsData EntryConditions { get; } = new();

    public List<PluginSettingField> Fields { get; } = [];

    /// <summary>
    /// The same fields, split over the groupboxes the Avalonia tab uses. A tab that names no groups
    /// ends up with one box called "Settings", which is what every strategy view calls its single
    /// groupbox; DLZ and FVG spread their settings over several.
    /// </summary>
    public List<PluginFieldGroup> FieldGroups { get; } = [];

    public PluginSettingsEditState(IStrategyPlugin plugin)
    {
        Plugin = plugin;

        // Keep the plugin's own declaration order — that order is meaningful (related settings sit
        // together, the "use X" switch sits next to the values it governs) and sorting by label threw
        // it away. GetProperties() does not guarantee an order, so sort explicitly: base-class
        // properties first by inheritance depth, then by MetadataToken, which within one type follows
        // the order the properties are declared in the source.
        static int InheritanceDepth(Type? type)
        {
            int depth = 0;
            while (type != null && type != typeof(object))
            {
                depth++;
                type = type.BaseType;
            }
            return depth;
        }

        foreach (var property in plugin.SettingsBase.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(p => InheritanceDepth(p.DeclaringType))
            .ThenBy(p => p.MetadataToken))
        {
            if (!property.CanRead || !property.CanWrite)
                continue;
            if (BaseProperties.Contains(property.Name))
                continue;

            var caption = property.GetCustomAttribute<SettingCaptionAttribute>();
            if (caption?.Expand == true)
            {
                // A block of settings that lives in an object of its own. Its children are drawn
                // here, in the group the object names, and the object itself never appears.
                foreach (var child in property.PropertyType
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .OrderBy(p => p.MetadataToken))
                {
                    if (child.CanRead && child.CanWrite && PluginSettingField.IsSupported(child))
                        Fields.Add(new PluginSettingField(child, property));
                }
                continue;
            }

            if (!PluginSettingField.IsSupported(property))
                continue;

            Fields.Add(new PluginSettingField(property));
        }

        // A setting that shares a row hands itself to the owner of that row and disappears from the
        // group list, so it is drawn as part of that row instead of on one of its own.
        foreach (var field in Fields)
        {
            if (string.IsNullOrEmpty(field.SameRowAs))
                continue;
            Fields.Find(f => f.Name == field.SameRowAs)?.Companions.Add(field);
        }

        foreach (var field in Fields)
        {
            if (!string.IsNullOrEmpty(field.SameRowAs) && Fields.Exists(f => f.Name == field.SameRowAs))
                continue;

            var group = FieldGroups.Find(g => g.Header == field.Group);
            if (group == null)
            {
                group = new PluginFieldGroup(field.Group);
                FieldGroups.Add(group);
            }
            group.Fields.Add(field);
        }
    }

    /// <summary>
    /// A setting that hangs off a switch is only shown while that switch is on, the way the
    /// Avalonia views bind IsVisible to the checkbox above the value.
    /// </summary>
    public bool IsFieldVisible(PluginSettingField field)
    {
        if (field.Hidden)
            return false;
        if (string.IsNullOrEmpty(field.VisibleWhen))
            return true;

        var owner = Fields.Find(f => f.Name == field.VisibleWhen);
        // An unknown name means the attribute points at something that is not an editable bool;
        // showing the setting is the safer outcome than hiding it forever.
        return owner == null || owner.BoolValue;
    }

    /// <summary>A setting that greys out while its switch is off (IsEnabled in the Avalonia view).</summary>
    public bool IsFieldEnabled(PluginSettingField field)
    {
        if (string.IsNullOrEmpty(field.EnabledWhen))
            return true;

        var owner = Fields.Find(f => f.Name == field.EnabledWhen);
        return owner == null || owner.BoolValue;
    }

    public void Load()
    {
        var settings = Plugin.SettingsBase;
        PlaySound = settings.PlaySound;
        PlaySpeech = settings.PlaySpeech;
        SoundFileLong = settings.SoundFileLong;
        SoundFileShort = settings.SoundFileShort;
        ColorLongArgb = settings.ColorLong.ToString();
        ColorShortArgb = settings.ColorShort.ToString();

        UseCustomEntryConditions = settings.EntryConditions != null;
        EntryConditions.LoadFrom(settings.EntryConditions ?? GlobalData.Settings.Trading.EntryConditions);

        foreach (var field in Fields)
            field.Load(settings);
    }

    public void Save()
    {
        var settings = Plugin.SettingsBase;
        settings.PlaySound = PlaySound;
        settings.PlaySpeech = PlaySpeech;
        settings.SoundFileLong = SoundFileLong ?? "";
        settings.SoundFileShort = SoundFileShort ?? "";
        // The alpha comes from the editor itself now; it decides whether the color is applied at all
        settings.ColorLong = FromArgb(ColorLongArgb, settings.ColorLong);
        settings.ColorShort = FromArgb(ColorShortArgb, settings.ColorShort);

        if (UseCustomEntryConditions)
        {
            settings.EntryConditions ??= new SettingsEntryConditions();
            EntryConditions.SaveTo(settings.EntryConditions);
        }
        else
            settings.EntryConditions = null;

        foreach (var field in Fields)
            field.Save(settings);
    }

    public static string ToHex(CoreColor c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    public static CoreColor FromHex(string? hex, byte alpha)
    {
        if (hex != null && hex.Length == 7 && hex[0] == '#')
        {
            try
            {
                byte r = Convert.ToByte(hex[1..3], 16);
                byte g = Convert.ToByte(hex[3..5], 16);
                byte b = Convert.ToByte(hex[5..7], 16);
                return CoreColor.FromArgb(alpha, r, g, b);
            }
            catch (FormatException)
            {
            }
        }
        return CoreColor.FromArgb(alpha, 0xFF, 0x95, 0xA5);
    }

    /// <summary>
    /// Parse "#AARRGGBB" (or "#RRGGBB"). CoreColor.Parse throws on anything malformed, so keep the
    /// value that was already stored rather than replacing a colour with black.
    /// </summary>
    public static CoreColor FromArgb(string? text, CoreColor current)
    {
        text = text?.Trim() ?? "";
        if (text.Length > 0 && !text.StartsWith('#'))
            text = "#" + text;
        try
        {
            var color = CoreColor.Parse(text);
            return color == default ? current : color;
        }
        catch (FormatException)
        {
            return current;
        }
    }
}

/// <summary>One groupbox of a strategy tab, with the settings that belong in it.</summary>
public class PluginFieldGroup(string header)
{
    public string Header { get; } = header;
    public List<PluginSettingField> Fields { get; } = [];
}

/// <summary>One reflected plugin setting, held as text/bool so Blazor can bind to it.</summary>
public class PluginSettingField
{
    private readonly PropertyInfo _property;

    /// <summary>
    /// The property holding the object this setting lives in, for a setting that came from an
    /// expanded block (SettingCaption.Expand); null for a setting on the settings class itself.
    /// </summary>
    private readonly PropertyInfo? _owner;

    /// <summary>
    /// Dotted for a setting inside an expanded block ("Shape.MinWickPercentage"), which is also the
    /// path the emulator queue addresses it by. Names have to stay unique: SameRowAs, VisibleWhen
    /// and EnabledWhen look each other up by this.
    /// </summary>
    public string Name => _owner == null ? _property.Name : $"{_owner.Name}.{_property.Name}";
    public string Label { get; }
    /// <summary>Hover text, taken from the ToolTip.Tip of the matching Avalonia control.</summary>
    public string? Tooltip { get; }
    /// <summary>Draw a divider above this setting, where the Avalonia view has a Separator.</summary>
    public bool SeparatorBefore { get; }
    /// <summary>Bold sub-heading above this setting.</summary>
    public string? SubHeader { get; }
    /// <summary>Indented, for a value that belongs to the switch above it.</summary>
    public bool Indented { get; }
    /// <summary>Name of the bool setting that has to be on for this one to show.</summary>
    public string? VisibleWhen { get; }
    /// <summary>Header of the groupbox this setting belongs to.</summary>
    public string Group { get; }
    /// <summary>Text after the field, e.g. "(1h candles)".</summary>
    public string? Unit { get; }
    /// <summary>Name of the setting this one shares its row with.</summary>
    public string? SameRowAs { get; }
    /// <summary>Extra white space above this setting.</summary>
    public bool SpaceBefore { get; }
    /// <summary>Name of the bool setting that has to be on for this one to be editable.</summary>
    public string? EnabledWhen { get; }
    /// <summary>Not drawn at all; the value still loads and saves.</summary>
    public bool Hidden { get; }
    /// <summary>Settings drawn on this one's row, without a caption of their own.</summary>
    public List<PluginSettingField> Companions { get; } = [];
    public PluginFieldKind Kind { get; }
    public string TextValue { get; set; } = "";
    public bool BoolValue { get; set; }
    public List<string> EnumOptions { get; } = [];
    /// <summary>
    /// The selected names of an <see cref="PluginFieldKind.IntervalList"/> or an
    /// <see cref="PluginFieldKind.EnumList"/>.
    /// </summary>
    public List<string> ListValue { get; } = [];

    public PluginSettingField(PropertyInfo property, PropertyInfo? owner = null)
    {
        _property = property;
        _owner = owner;

        // A caption declared on the property wins; it is the same text the Avalonia view shows.
        // Splitting the property name on capitals is the fallback for a plugin that has none yet,
        // and that fallback is what put "BBMin Percentage" on screen.
        var caption = property.GetCustomAttribute<SettingCaptionAttribute>();
        Label = caption?.Caption ?? SplitCamelCase(property.Name);
        Tooltip = caption?.Tooltip;
        SeparatorBefore = caption?.SeparatorBefore ?? false;
        SubHeader = caption?.SubHeader;
        Indented = caption?.Indented ?? false;
        VisibleWhen = caption?.VisibleWhen;
        string? ownerGroup = owner?.GetCustomAttribute<SettingCaptionAttribute>()?.Group;
        Group = string.IsNullOrEmpty(caption?.Group)
            ? (string.IsNullOrEmpty(ownerGroup) ? "Settings" : ownerGroup)
            : caption.Group;
        Unit = caption?.Unit;
        SameRowAs = caption?.SameRowAs;
        SpaceBefore = caption?.SpaceBefore ?? false;
        EnabledWhen = caption?.EnabledWhen;
        Hidden = caption?.Hidden ?? false;

        Type type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        if (IsIntervalList(property))
            Kind = PluginFieldKind.IntervalList;
        else if (EnumListType(property) is Type members)
        {
            Kind = PluginFieldKind.EnumList;
            EnumOptions.AddRange(Enum.GetNames(members));
        }
        else if (type == typeof(bool))
            Kind = PluginFieldKind.Bool;
        else if (type.IsEnum)
        {
            Kind = PluginFieldKind.Enum;
            EnumOptions.AddRange(Enum.GetNames(type));
        }
        else if (type == typeof(string))
            Kind = PluginFieldKind.Text;
        else
            Kind = PluginFieldKind.Number;
    }

    /// <summary>
    /// The list of intervals a strategy reports signals for. Avalonia renders it with its own
    /// IntervalView; here it is the one collection property that gets an editor, recognised by name
    /// so an unrelated List&lt;string&gt; is still skipped rather than drawn as a row of intervals.
    /// </summary>
    private static bool IsIntervalList(PropertyInfo property)
    {
        return property.Name == "IntervalList" && property.PropertyType == typeof(List<string>);
    }

    /// <summary>
    /// The enum a List&lt;string&gt; setting holds the member names of, or null when it is not one.
    /// A plugin says so with SettingCaption.EnumType; there is no way to tell from the type alone,
    /// and guessing by name is what made the interval list above the exception it is.
    /// </summary>
    private static Type? EnumListType(PropertyInfo property)
    {
        if (property.PropertyType != typeof(List<string>))
            return null;

        Type? type = property.GetCustomAttribute<SettingCaptionAttribute>()?.EnumType;
        return type != null && type.IsEnum ? type : null;
    }

    /// <summary>A setting whose value lives in <see cref="ListValue"/> rather than in the text.</summary>
    private bool IsList => Kind is PluginFieldKind.IntervalList or PluginFieldKind.EnumList;

    public static bool IsSupported(PropertyInfo property)
    {
        if (IsIntervalList(property) || EnumListType(property) != null)
            return true;

        Type type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        return type == typeof(bool)
            || type == typeof(string)
            || type.IsEnum
            || type == typeof(int)
            || type == typeof(long)
            || type == typeof(float)
            || type == typeof(double)
            || type == typeof(decimal);
    }

    /// <summary>Turn one interval on or off.</summary>
    public void ToggleListValue(string name, bool selected)
    {
        if (selected)
        {
            if (!ListValue.Contains(name))
                ListValue.Add(name);
        }
        else
            ListValue.Remove(name);
    }

    /// <summary>The object this setting's value lives in - the settings themselves, or the block it came from.</summary>
    private object? Owner(object settings) => _owner == null ? settings : _owner.GetValue(settings);

    public void Load(object settings)
    {
        object? target = Owner(settings);
        if (target == null)
            return;

        object? value = _property.GetValue(target);
        if (IsList)
        {
            ListValue.Clear();
            if (value is List<string> list)
                ListValue.AddRange(list);
        }
        else if (Kind == PluginFieldKind.Bool)
            BoolValue = value is bool b && b;
        else
            TextValue = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "";
    }

    public void Save(object settings)
    {
        object? target = Owner(settings);
        if (target == null)
            return;

        Type type = Nullable.GetUnderlyingType(_property.PropertyType) ?? _property.PropertyType;
        try
        {
            if (Kind == PluginFieldKind.EnumList)
            {
                // In the order the enum declares its members, not in the order they were ticked.
                // The Avalonia tab builds its list from the enum and therefore cannot do anything
                // else, and the order is not cosmetic: the strategy reports the FIRST shape in the
                // list that a candle forms, so a click order would make the two hosts name a
                // different pattern for the same candle.
                _property.SetValue(target, EnumOptions.FindAll(ListValue.Contains));
                return;
            }

            if (IsList)
            {
                _property.SetValue(target, new List<string>(ListValue));
                return;
            }

            if (Kind == PluginFieldKind.Bool)
            {
                _property.SetValue(target, BoolValue);
                return;
            }

            if (string.IsNullOrWhiteSpace(TextValue))
            {
                if (type == typeof(string))
                    _property.SetValue(target, "");
                else if (Nullable.GetUnderlyingType(_property.PropertyType) != null)
                    _property.SetValue(target, null);
                return;
            }

            object converted = type.IsEnum
                ? Enum.Parse(type, TextValue)
                : Convert.ChangeType(TextValue, type, System.Globalization.CultureInfo.InvariantCulture);
            _property.SetValue(target, converted);
        }
        catch (Exception)
        {
            // Leave the stored value untouched when the typed text is not convertible
        }
    }

    private static string SplitCamelCase(string name)
    {
        var sb = new System.Text.StringBuilder(name.Length + 8);
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
                sb.Append(' ');
            sb.Append(name[i]);
        }
        return sb.ToString();
    }
}

public enum PluginFieldKind
{
    Text,
    Number,
    Bool,
    Enum,
    /// <summary>One checkbox per member of an enum, the selected names stored as a list of strings.</summary>
    EnumList,
    IntervalList,
}
