using System.Drawing;
using System.Text.Json.Serialization;

namespace CryptoScanBot.Core.Settings.Strategy;

// Base class for the colors and soundfile

[Serializable]
public class SettingsSignalStrategyBase
{
    public bool PlaySound { get; set; } = false;
    public bool PlaySpeech { get; set; } = false;

    [JsonConverter(typeof(Intern.ColorConverter))]
    public Color ColorLong { get; set; } = Color.White;
    public string SoundFileLong { get; set; } = "";
    //string text = ColorTranslator.ToHtml(Color);
    //Color color = ColorTranslator.FromHtml(text);

    [JsonConverter(typeof(Intern.ColorConverter))]
    public Color ColorShort { get; set; } = Color.White;
    public string SoundFileShort { get; set; } = "";


    ////  Colors
    //public SettingsColor ColorLongX = new();
    //public SettingsSound SoundLongX = new();

    ////  Sounds
    //public SettingsColor ColorShortX = new();
    //public SettingsSound SoundShortX = new ();
}
