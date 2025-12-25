using Avalonia.Media;
using Avalonia.Platform.Storage;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Core;

namespace CryptoScanner.Settings.ViewModels;

public partial class ColorAndSoundViewModel : ObservableObject
{
    [ObservableProperty]
    private string _caption = "string.Empty";

    [ObservableProperty]
    private Color _selectedColor = Colors.Transparent;

    [ObservableProperty]
    private string _soundFile = string.Empty;

    public ColorAndSoundViewModel()
    {
    }

    //public ColorAndSoundViewModel(string caption, Color color, string soundFile)
    //{
    //    _caption = caption;
    //    _selectedColor = color;
    //    _soundFile = soundFile;
    //}

    public void LoadConfig(string caption, Color color, string soundFile)
    {
        Caption = caption;
        SelectedColor = color;
        SoundFile = soundFile;
    }
    // This method is called from the View with access to the parent window
    // See ColorAndSoundView.axaml.cs for implementation

    [RelayCommand]
    private async Task SelectSoundFileAsync()
    {
        // This needs to be called from the View with access to the Window
        // See the code-behind for implementation
    }

    [RelayCommand]
    private void PlaySound()
    {
        if (!string.IsNullOrEmpty(SoundFile))
        {
            GlobalData.PlaySomeMusic(SoundFile, true);
        }
    }

}
