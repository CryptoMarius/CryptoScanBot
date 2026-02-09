using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Services;
using CryptoScanner.Model;
using CryptoScanner.ViewModels;

using System.ComponentModel;

namespace CryptoScanner.Views;

public partial class LogView : UserControlWithListBox<CryptoLog, LogColumnEnum, LogViewModel, LogColumnComparer>
{
    public LogView()
    {
        _gridName = "Log";
        _targetMenu = TargetMenu.Log;
        System.Diagnostics.Debug.WriteLine($"{_gridName} constructor called");
        InitializeComponent();

        if (Design.IsDesignMode)
            return;

        Loaded += ListBox_Loaded;
    }
}