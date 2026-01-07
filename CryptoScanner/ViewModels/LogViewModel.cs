using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.ViewModels
{
    public partial class LogViewModel : ObservableObject
    {
        [ObservableProperty]
        private DateTime _date;

        [ObservableProperty]
        private string _text = string.Empty;
    }
}