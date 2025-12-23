using Avalonia.Controls;

namespace CryptoScanner.Services;

public interface IDialogService
{
    Task ShowDialogAsync<T>() where T : Window, new();
    Task<TResult?> ShowDialogAsync<T, TResult>() where T : Window, new();
}
    