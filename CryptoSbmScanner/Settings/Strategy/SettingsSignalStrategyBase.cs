using System.Text.Json.Serialization;

namespace CryptoSbmScanner.Settings.Strategy;

// Base class for the colors and soundfile

[Serializable]
public class SettingsSignalStrategyBase
{
    public bool PlaySound { get; set; } = false;
    public bool PlaySpeech { get; set; } = false;

    [JsonConverter(typeof(Intern.ColorConverter))]
    public Color ColorLong { get; set; } = Color.White;
    public string SoundFileLong { get; set; } = "";

    [JsonConverter(typeof(Intern.ColorConverter))]
    public Color ColorShort { get; set; } = Color.White;
    public string SoundFileShort { get; set; } = "";
}
