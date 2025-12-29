using Avalonia.Controls;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Model;
using CryptoScanner.LiveData.Model;
using CryptoScanner.Core.Trader;
using CryptoScanner.Core.Settings;


namespace CryptoScanner.LiveData.ViewModels;

public partial class LiveDataGridViewModel : ObservableObjectWithOwner
{
    private DispatcherTimer? _updateTimer = new() { Interval = TimeSpan.FromMilliseconds(3000) };


    [ObservableProperty]
    private ObservableRangeCollection<LiveDataInfo> _LiveDatas = [];

    // Event voor parent ViewModel
    public event EventHandler<string>? EventOpenInInternalBrowser;

    public LiveDataGridViewModel()
    {
        System.Diagnostics.Debug.WriteLine("LiveDataGridViewModel constructor called");

        _updateTimer.Tick += TimerAddLiveDataTick;
        _updateTimer.Start();
    }

    public void Dispose()
    {
        _updateTimer?.Stop();
        _updateTimer = null;
    }

    public event EventHandler<LiveDataInfo>? RequestSortedInsert;
    public event EventHandler? RequestSort;


    private void TimerAddLiveDataTick(object? sender, EventArgs e)
    {
        if (GlobalData.ApplicationIsClosing)
            return;

        // Speed up adding LiveDatas
        if (GlobalData.LiveDataQueue.Count > 0)
        {
            if (Monitor.TryEnter(GlobalData.LiveDataQueue))
            {
                try
                {
                    List<LiveDataInfo> liveDataList = [];
                    while (GlobalData.LiveDataQueue.Count > 0)
                    {
                        CryptoLiveData liveData = GlobalData.LiveDataQueue.Dequeue();
                        if (liveData != null)
                        {
                            if (!(TradingConfig.Signals[CryptoTradeSide.Long].InBlackList(liveData.Symbol.Name) == MatchBlackAndWhiteList.Present ||
                                TradingConfig.Signals[CryptoTradeSide.Short].InBlackList(liveData.Symbol.Name) == MatchBlackAndWhiteList.Present))
                            {
                                var liveDataInfo = new LiveDataInfo()
                                {
                                    LiveDataObject = liveData,
                                };
                                liveDataList.Add(liveDataInfo);
                            }
                        }
                    }


                    if (liveDataList.Count == 1)
                    {
                        RequestSortedInsert?.Invoke(this, liveDataList[0]);
                        System.Diagnostics.Debug.WriteLine($"TimerAddLiveDatasTick added {liveDataList.Count} LiveData via binsearch");
                    }
                    else
                    {
                        LiveDatas.AddRange(liveDataList);
                        RequestSort?.Invoke(this, EventArgs.Empty);
                        System.Diagnostics.Debug.WriteLine($"TimerAddLiveDatasTick added {liveDataList.Count} LiveDatas via complete sort");
                    }

                }
                finally
                {
                    Monitor.Exit(GlobalData.LiveDataQueue);
                }
            }
        }
    }

}