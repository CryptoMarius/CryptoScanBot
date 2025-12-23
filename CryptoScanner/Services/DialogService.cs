using Avalonia.Controls;

namespace CryptoScanner.Services;

public class DialogService : IDialogService
{
    private readonly Window _owner;

    public DialogService(Window owner)
    {
        _owner = owner;
    }

    public async Task ShowDialogAsync<T>() where T : Window, new()
    {
        var dialog = new T();
        await dialog.ShowDialog(_owner);
    }

    public async Task<TResult?> ShowDialogAsync<T, TResult>() where T : Window, new()
    {
        var dialog = new T();
        return await dialog.ShowDialog<TResult>(_owner);
    }
}

