using Avalonia.Controls;

namespace CryptoScanner.Services;

//public interface IInitializable
//{
//    void Initialize();
//}

public interface IDialogService
{
    Task ShowDialogAsync<T>() where T : Window, new();
    Task<TResult?> ShowDialogAsync<T, TResult>() where T : Window, new();
    Task ShowDialogAsync<TWindow>(params object[] parameters) where TWindow : Window;
}
