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

        //if (dialog.DataContext is IInitializable initializable)
        //{
        //    initializable.Initialize();
        //}

        await dialog.ShowDialog(_owner);
    }

    public async Task<TResult?> ShowDialogAsync<T, TResult>() where T : Window, new()
    {
        var dialog = new T();

        //if (dialog.DataContext is IInitializable initializable)
        //{
        //    initializable.Initialize();
        //}

        return await dialog.ShowDialog<TResult>(_owner);
    }

    public async Task ShowDialogAsync<TWindow>(params object[] parameters) where TWindow : Window
    {
        var window = (TWindow)Activator.CreateInstance(typeof(TWindow), parameters)!;
        await window.ShowDialog(_owner);
    }
}

