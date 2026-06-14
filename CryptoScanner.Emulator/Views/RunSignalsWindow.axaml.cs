using Avalonia.Controls;

using CryptoScanner.Emulator.ViewModels;

namespace CryptoScanner.Emulator.Views;

public partial class RunSignalsWindow : Window
{
    // The single reused signals window. Static so opening signals for another run reuses this window
    // (its content is swapped) instead of stacking new windows, which got confusing over time.
    // Mirrors ChartWindowLauncher's single-window handling.
    private static RunSignalsWindow? _instance;

    public RunSignalsWindow() : this(new RunRow())
    {
        // Designer-only path: empty constructor for the XAML preview.
    }

    public RunSignalsWindow(RunRow run)
    {
        InitializeComponent();
        DataContext = new RunSignalsViewModel(run);
    }

    /// <summary>
    /// Opens the signals window for the given run, reusing the one existing instance. When a window is
    /// already open its content is replaced with the new run and it is brought to the front, so only a
    /// single signals window is ever shown from the emulator. A closed window (IsVisible == false)
    /// causes a fresh one to be created on the next call.
    /// </summary>
    public static void ShowSingle(RunRow run, Window owner)
    {
        if (_instance == null || !_instance.IsVisible)
        {
            _instance = new RunSignalsWindow(run);
            _instance.Show(owner);
        }
        else
        {
            _instance.DataContext = new RunSignalsViewModel(run);
            if (_instance.WindowState == WindowState.Minimized)
                _instance.WindowState = WindowState.Normal;
            _instance.Activate();
        }
    }
}
