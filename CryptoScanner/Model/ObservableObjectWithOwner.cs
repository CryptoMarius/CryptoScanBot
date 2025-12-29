using Avalonia.Controls;

using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Model;

public partial class ObservableObjectWithOwner : ObservableObject
{
    internal Window? _owner;

    public void SetOwner(Window owner)
    {
        _owner = owner;
    }
}
