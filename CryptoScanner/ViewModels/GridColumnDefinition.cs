using Avalonia.Controls;
using Avalonia.Layout;

using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.ViewModels;

public partial class GridColumnDefinition<TColumnEnum> : ObservableObject where TColumnEnum : Enum 
{
    public required TColumnEnum ColumnEnum { get; set; }
    public required string Header { get; set; }
    public double Width { get; set; }
    public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;
    public bool IsVisible { get; set; } = true;
    public int DisplayIndex { get; set; }

    [ObservableProperty]
    private GridLength _actualWidth; 
}

