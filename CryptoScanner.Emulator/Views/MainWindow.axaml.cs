using Avalonia.Controls;

using CryptoScanner.Core.Core;

namespace CryptoScanner.Emulator.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Reflect the resolved runtime locations so the operator can verify the emulator
        // really points at the dedicated folder before doing any work.
        VersionText.Text = GlobalData.AppVersion;
        AppPathText.Text = GlobalData.AppPath;
        DataFolderText.Text = GlobalData.AppDataFolder;
    }
}
