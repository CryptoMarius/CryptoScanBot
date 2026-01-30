using Avalonia.Collections;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Trader;


namespace CryptoScanner.ViewModels;

public partial class LiveDataGridViewModel : ObservableObject
{
    private DispatcherTimer _updateTimer = new() { Interval = TimeSpan.FromMilliseconds(3000) };


    [ObservableProperty]
    private AvaloniaList<LiveDataViewModel> _LiveDatas = [];

    public LiveDataGridViewModel()
    {
        System.Diagnostics.Debug.WriteLine("LiveDataGridViewModel constructor called");

        _updateTimer.Tick += TimerAddLiveDataTick;
        _updateTimer.Start();
    }

    public void Dispose()
    {
        _updateTimer.Stop();
        _updateTimer.Tick -= TimerAddLiveDataTick;
    }


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
                    List<LiveDataViewModel> liveDataList = [];
                    while (GlobalData.LiveDataQueue.Count > 0)
                    {
                        CryptoLiveData liveData = GlobalData.LiveDataQueue.Dequeue();
                        if (liveData != null)
                        {
                            if (!(TradingConfig.Signals[CryptoTradeSide.Long].InBlackList(liveData.Symbol.Name) == MatchBlackAndWhiteList.Present ||
                                TradingConfig.Signals[CryptoTradeSide.Short].InBlackList(liveData.Symbol.Name) == MatchBlackAndWhiteList.Present))
                            {
                                var liveDataInfo = new LiveDataViewModel() { Object = liveData, };
                                liveDataList.Add(liveDataInfo);
                            }
                        }
                    }
                    LiveDatas.AddRange(liveDataList);
                }
                finally
                {
                    Monitor.Exit(GlobalData.LiveDataQueue);
                }
            }
        }
    }

}