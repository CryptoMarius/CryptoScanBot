using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;
using CryptoScanner.Core.Settings;

namespace CryptoScanner.Config.Views;

public partial class ConfigurationWindow : Window
{
    public ConfigurationWindow()
    {
        InitializeComponent();

        // Set DataContext if not already set by parent
        if (DataContext == null)
        {
            DataContext = new ConfigurationViewModel();
        }
    }

    /// <summary>
    /// Opens the dialog on another settings set than GlobalData.Settings. With readOnly the Okay
    /// button writes nothing back, so a stored set (a finished emulator run) can be inspected while
    /// a replay is running.
    /// </summary>
    public ConfigurationWindow(SettingsBasic settings, bool readOnly)
    {
        InitializeComponent();
        DataContext = new ConfigurationViewModel(settings, readOnly);
    }
}
