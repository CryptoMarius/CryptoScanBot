using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class ColorAndSoundView : UserControl
{
    public ColorAndSoundView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new ColorAndSoundViewModel();
        }
    }

    private async void ButtonSelectSound_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ColorAndSoundViewModel viewModel)
            return;

        // Get the parent window
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
            return;

        // Determine initial directory
        string initialDir = string.Empty;
        if (!string.IsNullOrEmpty(viewModel.SoundFile))
        {
            var dir = Path.GetDirectoryName(viewModel.SoundFile);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                initialDir = dir;
        }

        // If no valid directory, use Sounds subdirectory
        if (string.IsNullOrEmpty(initialDir))
        {
            string? exePath = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrEmpty(exePath))
            {
                string soundsPath = Path.Combine(exePath, "Sounds");
                if (Directory.Exists(soundsPath))
                    initialDir = soundsPath;
            }
        }

        // Create file picker options
        var options = new FilePickerOpenOptions
        {
            Title = "Select Sound File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("WAV Files")
                {
                    Patterns = new[] { "*.wav" }
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        };

        // Set initial directory if available
        if (!string.IsNullOrEmpty(initialDir))
        {
            try
            {
                var folder = await topLevel.StorageProvider.TryGetFolderFromPathAsync(initialDir);
                if (folder != null)
                    options.SuggestedStartLocation = folder;
            }
            catch
            {
                // Ignore errors with initial directory
            }
        }

        // Show file picker
        var result = await topLevel.StorageProvider.OpenFilePickerAsync(options);

        if (result.Count > 0)
        {
            var selectedFile = result[0];
            var filePath = selectedFile.Path.LocalPath;

            if (File.Exists(filePath))
            {
                viewModel.SoundFile = filePath;
            }
        }
    }
}
