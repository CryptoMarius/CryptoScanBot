using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Services;
using CryptoScanner.Model;
using CryptoScanner.ViewModels;

using System.ComponentModel;

namespace CryptoScanner.Views;

public partial class PositionClosedView : UserControlWithListBox<CryptoPosition, PositionColumnEnum, PositionClosedViewModel, PositionColumnComparer>
{
    public PositionClosedView()
    {
        _gridName = "PositionClosed";
        _targetMenu = TargetMenu.Position;
        System.Diagnostics.Debug.WriteLine($"{_gridName} constructor called");
        InitializeComponent();

        if (Design.IsDesignMode)
            return;

        Loaded += ListBox_Loaded;
    }
}