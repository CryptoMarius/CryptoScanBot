using Avalonia.Collections;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Trader;


namespace CryptoScanner.ViewModels;

public partial class LiveDataViewModel : BaseGridViewModel<CryptoLiveData, LiveDataColumnEnum, LiveDataColumnComparer>
{
    private DispatcherTimer _updateTimer = new() { Interval = TimeSpan.FromMilliseconds(3000) };

    public LiveDataViewModel()
    {
        System.Diagnostics.Debug.WriteLine("LiveDataGridViewModel constructor called");
        SortColumn = LiveDataColumnEnum.Date;
        _columns = LiveDataColumns.GetColumns();
        _columnWidths = GetWidths(_columns);
        System.Diagnostics.Debug.WriteLine($"LiveDataGridViewModel: {_columns.Count} columns, {_columnWidths.Count} widths");

        _updateTimer.Tick += TimerAddLiveDataTick;
        _updateTimer.Start();
    }

    public void Dispose()
    {
        _updateTimer.Stop();
        _updateTimer.Tick -= TimerAddLiveDataTick;
    }



    protected override void RefreshVisibleItems()
    {
        System.Diagnostics.Debug.WriteLine("RefreshVisibleItems called");

        if (Dispatcher.UIThread.CheckAccess())
        {
            lock (_lock)
            {
                // Bewaar huidige selectie
                var selected = SelectedObject;

                // Vervang collectie
                VisibleObjects = new AvaloniaList<CryptoLiveData>(_allObjects);

                // Herstel selectie
                if (selected != null)
                {
                    SelectedObject = VisibleObjects.FirstOrDefault(p => p == selected);
                }
            }
        }
        else
        {
            Dispatcher.UIThread.Post(() =>
            {
                lock (_lock)
                {
                    var selected = SelectedObject;
                    VisibleObjects = new AvaloniaList<CryptoLiveData>(_allObjects);
                    if (selected != null)
                    {
                        SelectedObject = VisibleObjects.FirstOrDefault(p => p == selected);
                    }
                }
            });
        }
    }


    private void TimerAddLiveDataTick(object? sender, EventArgs e)
    {
        if (GlobalData.ApplicationIsClosing)
            return;

        // Speed up adding LiveDatas
        if (GlobalData.LiveDataQueue.Count == 0)
            return;

        // Background processing
        Task.Run(() =>
        {
            List<CryptoLiveData> list = [];
            if (Monitor.TryEnter(GlobalData.LiveDataQueue))
            {
                try
                {
                    while (GlobalData.LiveDataQueue.Count > 0)
                    {
                        CryptoLiveData liveData = GlobalData.LiveDataQueue.Dequeue();
                        if (liveData != null)
                        {
                            if (!(TradingConfig.Signals[CryptoTradeSide.Long].InBlackList(liveData.Symbol.Name) == MatchBlackAndWhiteList.Present ||
                                TradingConfig.Signals[CryptoTradeSide.Short].InBlackList(liveData.Symbol.Name) == MatchBlackAndWhiteList.Present))
                            {
                                list.Add(liveData);
                            }
                        }
                    }
                }
                finally
                {
                    Monitor.Exit(GlobalData.LiveDataQueue);
                }
            }

            if (list.Count > 0)
            {
                // Modify binnen lock
                lock (_lock)
                {
                    _allObjects.AddRange(list);
                    ApplySort(SortColumn);
                }

                // Update UI
                RefreshVisibleItems();
            }
        });
    }

}