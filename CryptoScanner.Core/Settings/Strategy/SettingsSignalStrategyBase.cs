using Avalonia.Media;

namespace CryptoScanner.Core.Settings.Strategy;

// Base class for the colors and soundfile

[Serializable]
public class SettingsSignalStrategyBase
{
    public bool PlaySound { get; set; } = false;
    public bool PlaySpeech { get; set; } = false;

    public Color ColorLong { get; set; } = Color.Parse("#00FFFFFF");
    public string SoundFileLong { get; set; } = "";

    public Color ColorShort { get; set; } = Color.Parse("#00FFFFFF");
    public string SoundFileShort { get; set; } = "";
}
