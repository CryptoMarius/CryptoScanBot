using Avalonia;
using Avalonia.Collections;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

using CryptoScanner.Core.Barometer;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Services;
using CryptoScanner.Core.Signal;
using CryptoScanner.Helpers;
using CryptoScanner.Services;

using SkiaSharp;

using System.Collections.ObjectModel;

namespace CryptoScanner.ViewModels;

public partial class DashBoardInformationViewModel : ObservableObject
{
    private readonly DispatcherTimer _barometerTimer;

    #region Traffic Light Status

    [ObservableProperty]
    private IBrush _scannerStatusBrush = Brushes.Gray;

    [ObservableProperty]
    private IBrush _traderStatusBrush = Brushes.Gray;

    [ObservableProperty]
    private IBrush _rulezStatusBrush = Brushes.Gray;

    [ObservableProperty]
    private IBrush _soundStatusBrush = Brushes.Gray;

    #endregion

    #region Barometer

    // Could extract barometer code to its own userControl and service..

    [ObservableProperty]
    private ObservableCollection<string> _quotes = ["USDT", "BTC", "EUR"];

    [ObservableProperty]
    private string _selectedQuote = "";

    [ObservableProperty]
    private ObservableCollection<string> _intervals = ["15m", "30m", "1h", "4h", "1d"];

    [ObservableProperty]
    private string _selectedInterval = "1h";

    [ObservableProperty]
    private WriteableBitmap? _chartImage;

    [ObservableProperty]
    private decimal _barometer1h = 0;

    [ObservableProperty]
    private decimal _barometer4h = 0;

    [ObservableProperty]
    private decimal _barometer1d = 0;

    // Market breadth per interval: the percentage of symbols that rose. The barometer above is an
    // average and reads the same for "every coin rises a little" and "a few coins carry the move";
    // the breadth tells those two apart. See BarometerResult for the other figures of the same
    // measurement, which are stored in the barometer candles.
    [ObservableProperty]
    private decimal _rising1h = 0;

    [ObservableProperty]
    private decimal _rising4h = 0;

    [ObservableProperty]
    private decimal _rising1d = 0;

    // The remaining figures - median, spread, coin count, skipped outliers - have no room of their
    // own in this panel, so they live in the tooltip of each barometer row.
    [ObservableProperty]
    private string _barometer1hTooltip = string.Empty;

    [ObservableProperty]
    private string _barometer4hTooltip = string.Empty;

    [ObservableProperty]
    private string _barometer1dTooltip = string.Empty;

    // Which figure the graph draws, and the choices for it.
    [ObservableProperty]
    private ObservableCollection<string> _graphValues = [];

    [ObservableProperty]
    private string _selectedGraphValue = BarometerCandleFields.GetName(BarometerGraphValue.Average);

    [ObservableProperty]
    private string _barometerTime = string.Empty;
    private string _barometerCalculated = string.Empty;

    [ObservableProperty]
    private string _applicationStatus = "";

    #endregion


    #region Crypto Symbols

    // De collection voor binding in de UI
    [ObservableProperty]
    private AvaloniaList<DashboardSymbolViewModel> _tvSymbols = [];

    [ObservableProperty]
    private AvaloniaList<DashboardSymbolViewModel> _topSymbols = [];


    #endregion


    [ObservableProperty]
    private int _klineTickerCount = 0;
    [ObservableProperty]
    private int _scannerExecuteCount = 0;
    [ObservableProperty]
    private int _scannerSignalCount = 0;
    [ObservableProperty]
    private string _scannerPositionCount = "";

    [ObservableProperty]
    private string _candleProgressText = "";



    private readonly ApplicationStateService _applicationStateService;
    private readonly ITradingViewService _tradingViewService;

    public DashBoardInformationViewModel(
        ApplicationStateService applicationStateService,
        ITradingViewService tradingViewService)
    {
        _applicationStateService = applicationStateService;
        _tradingViewService = tradingViewService;


        // Subscribe to market indicator events
        //_tradingViewService.MarketCapTotalChanged += (s, v) => MarketCapTotal.Update(v, null);
        //_tradingViewService.DollarIndexChanged += (s, v) => DollarIndex.Update(v, null);
        //_tradingViewService.Spx500Changed += (s, v) => Spx500.Update(v, null);
        //_tradingViewService.BitcoinDominanceChanged += (s, v) => BitcoinDominance.Update(v, null);
        //_tradingViewService.FearAndGreedIndexChanged += (s, v) => FearAndGreedIndex.Update(v, null);

        System.Diagnostics.Debug.WriteLine("DashBoardInformationViewModel constructor called");

        WeakReferenceMessenger.Default.Register<StatusesHaveChangedMessage>(this, OnStatusesHaveChanged);
        WeakReferenceMessenger.Default.Register<SymbolsHaveChangedMessage>(this, OnSymbolsHaveChanged);
        WeakReferenceMessenger.Default.Register<ExchangeSwitchedMessage>(this, OnExchangeSwitched);

        InitializeBarometer();

        _barometerTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _barometerTimer.Tick += OnBarometerTimer;
        _barometerTimer.Start();

        RegisterExchangeSymbols(); // The symbols are probably not read at this point
        RegisterTradingViewSymbols();
        StatusesHaveChanged(); // -- event is not set properly?
    }

    public void Dispose()
    {
        _barometerTimer.Stop();
        _barometerTimer.Tick -= OnBarometerTimer;

        WeakReferenceMessenger.Default.Unregister<StatusesHaveChangedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<SymbolsHaveChangedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<ExchangeSwitchedMessage>(this);
    }


    private void OnStatusesHaveChanged(object recipient, StatusesHaveChangedMessage message)
    {
        StatusesHaveChanged();
    }

    private void OnSymbolsHaveChanged(object recipient, SymbolsHaveChangedMessage message)
    {
        // Refresh quotes and symbols after GetSymbolsAsync() or exchange switch
        InitializeBarometer();
        RegisterExchangeSymbols();
    }

    private void OnExchangeSwitched(object recipient, ExchangeSwitchedMessage message)
    {
        ChartImage = null;
        GlobalData.CreatedSignalCount = 0;
        SignalExecute.ResetAnalyseCount();
        ExchangeBase.KLineTicker?.Reset();

        // Reinitialize barometer with the quotes of the new exchange
        InitializeBarometer();
        RegisterExchangeSymbols();

        // Switch to the default quote of the new exchange if it is available
        string? defaultQuote = ExchangeBase.ExchangeOptions.DefaultQuote;
        if (!string.IsNullOrEmpty(defaultQuote) && Quotes.Contains(defaultQuote))
            SelectedQuote = defaultQuote;
    }

    private void StatusesHaveChanged()
    {
        if (GlobalData.ApplicationStatus == CryptoApplicationStatus.Running)
            ApplicationStatus = DateTime.Now.ToString("HH:mm");
        else
            ApplicationStatus = GlobalData.ApplicationStatus.ToString();

        if (GlobalData.Settings.Trading.Active && GlobalData.ApplicationStatus == CryptoApplicationStatus.Running)
            TraderStatusBrush = App.GetBrushResource("PriceUpBrush");
        else
            TraderStatusBrush = App.GetBrushResource("PriceDownBrush");

        if (GlobalData.Settings.Signal.Active && GlobalData.ApplicationStatus == CryptoApplicationStatus.Running)
            ScannerStatusBrush = App.GetBrushResource("PriceUpBrush");
        else
            ScannerStatusBrush = App.GetBrushResource("PriceDownBrush");

        if (GlobalData.ActiveExchange != null)
        {
            var pause = GlobalData.ActiveExchange.Data.PauseTrading;
            if (!pause.Calculated.HasValue || (pause.Until.HasValue && pause.Until > DateTime.UtcNow))
                RulezStatusBrush = App.GetBrushResource("PriceDownBrush");
            else
                RulezStatusBrush = App.GetBrushResource("PriceUpBrush");
        }
        else RulezStatusBrush = App.GetBrushResource("PriceNeutralBrush");

        if (GlobalData.Settings.Signal.SoundsActive && GlobalData.ApplicationStatus == CryptoApplicationStatus.Running)
            SoundStatusBrush = App.GetBrushResource("PriceUpBrush");
        else
            SoundStatusBrush = App.GetBrushResource("PriceDownBrush");
    }


    // Click handlers for the dashboard status icons — mirror the menu checkboxes.
    // Each one flips the underlying setting and broadcasts StatusesHaveChangedMessage so
    // both the dashboard brushes and the menu checkbox stay in sync.
    public void ToggleScanner()
    {
        GlobalData.Settings.Signal.Active = !GlobalData.Settings.Signal.Active;
        GlobalData.SendMvvmMessage(new StatusesHaveChangedMessage());
    }

    public void ToggleTrader()
    {
        GlobalData.Settings.Trading.Active = !GlobalData.Settings.Trading.Active;
        GlobalData.SendMvvmMessage(new StatusesHaveChangedMessage());
    }

    public void ToggleSounds()
    {
        GlobalData.Settings.Signal.SoundsActive = !GlobalData.Settings.Signal.SoundsActive;
        GlobalData.SendMvvmMessage(new StatusesHaveChangedMessage());
    }



    public void InitializeBarometer()
    {
        // Add the active quotes (default=usdt)
        List<string> quotes = [];
        foreach (CryptoQuoteData cryptoQuoteData in GlobalData.Settings.QuoteCoins.Values)
        {
            if (cryptoQuoteData.FetchCandles && cryptoQuoteData.SymbolList.Count > 0)
                quotes.Add(cryptoQuoteData.Name);
        }
        if (quotes.Count == 0)
            quotes.Add("USDT");
        Quotes = new ObservableCollection<string>(quotes);

        if (string.IsNullOrEmpty(_applicationStateService.BarometerQuote) || !quotes.Contains(_applicationStateService.BarometerQuote))
            SelectedQuote = quotes[0];
        else
            SelectedQuote = _applicationStateService.BarometerQuote;

        // Add all intervals (default=1h)
        List<string> intervals = [];
        intervals.Add("1h");
        intervals.Add("4h");
        intervals.Add("1d");
        Intervals = new ObservableCollection<string>(intervals);

        if (string.IsNullOrEmpty(_applicationStateService.BarometerInterval) || !Intervals.Contains(_applicationStateService.BarometerInterval))
            SelectedInterval = intervals[0];
        else
            SelectedInterval = _applicationStateService.BarometerInterval;

        // Which figure of the measurement the graph draws (default = the average, the barometer as
        // it always was). See BarometerCandleFields for the other five.
        GraphValues = new ObservableCollection<string>(BarometerCandleFields.Names);

        if (string.IsNullOrEmpty(_applicationStateService.BarometerGraphValue) || !GraphValues.Contains(_applicationStateService.BarometerGraphValue))
            SelectedGraphValue = GraphValues[0];
        else
            SelectedGraphValue = _applicationStateService.BarometerGraphValue;

        BarometerTime = _barometerCalculated;

        // If nothing changed we need to fill the symbols
        if (TopSymbols.Count == 0)
            RegisterExchangeSymbols(); // The symbols are probably not read at this point
        UpdateSymbolPrices(); // try? Not sure if everything is initialized
    }


    partial void OnSelectedQuoteChanged(string value)
    {
        _applicationStateService.BarometerQuote = value;
        System.Diagnostics.Debug.WriteLine($"Quote changed to: {value}");
        Task.Run(CalculateBarometer);

        if (GlobalData.SignalRService != null)
            GlobalData.SignalRService.SelectedQuote = value;

        RegisterExchangeSymbols();
        UpdateSymbolPrices();
    }


    partial void OnSelectedIntervalChanged(string value)
    {
        _applicationStateService.BarometerInterval = value;
        System.Diagnostics.Debug.WriteLine($"Interval changed to: {value}");
        Task.Run(CalculateBarometer);

        if (GlobalData.SignalRService != null)
            GlobalData.SignalRService.SelectedInterval = value;
    }


    partial void OnSelectedGraphValueChanged(string value)
    {
        // Not forwarded to SignalR: the hub serves the average to an external consumer and switching
        // the desktop graph should not change what that consumer receives.
        _applicationStateService.BarometerGraphValue = value;
        Task.Run(CalculateBarometer);
    }


    private void RegisterExchangeSymbols()
    {
        List<DashboardSymbolViewModel> list = [];

        string quote = SelectedQuote;
        if (string.IsNullOrEmpty(quote))
            return;
        var exchange = GlobalData.ActiveExchange;
        if (GlobalData.Settings.QuoteCoins.TryGetValue(quote, out CryptoQuoteData? quoteData) && exchange != null)
        {
            // Might just sort de exchange symbols and take the top 5 based on volume?
            foreach (string baseCoin in GlobalData.Settings.ShowSymbolInformation)
            {
                if (exchange.SymbolListName.TryGetValue(baseCoin + quoteData.Name, out CryptoSymbol? symbol)
                    || exchange.SymbolListName.TryGetValue(baseCoin + "USDT", out symbol))
                {
                    list.Add(new(IndicatorType.Exchange, symbol.Name, symbol.Name, symbol.PriceDisplayFormat));
                }
            }
        }

        // Did something change?
        if (TopSymbols != null && list.Count == TopSymbols.Count)
        {
            bool equal = true;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Type != TopSymbols[i].Type)
                    equal = false;
                if (list[i].Symbol != TopSymbols[i].Symbol)
                    equal = false;
                if (list[i].Name != TopSymbols[i].Name)
                    equal = false;
            }
            if (equal)
                return;
        }

        TopSymbols = [.. list];
    }


    private void RegisterTradingViewSymbols()
    {
        List<DashboardSymbolViewModel> list = [];
        list.Add(new(IndicatorType.TradingView, "CRYPTOCAP:TOTAL3", "Market Cap Total", "N2", true));
        list.Add(new(IndicatorType.TradingView, "TVC:DXY", "US Dollar Index", "N2", true));
        list.Add(new(IndicatorType.TradingView, "SP:SPX", "S&P 500", "N2", true));
        list.Add(new(IndicatorType.TradingView, "CRYPTOCAP:BTC.D", "BTC Dominance", "N2", false));
        list.Add(new(IndicatorType.FearAndGreed, "https://alternative.me/crypto/fear-and-greed-index/", "Fear and Greed index", "N2", false));
        TvSymbols = [.. list];

        _tradingViewService.TvSymbols = TvSymbols; // forward symbols
    }

    private int BarometerLastMinute = -1;
    private void OnBarometerTimer(object? sender, EventArgs e)
    {
        try
        {
            if (TopSymbols.Count == 0)
                RegisterExchangeSymbols(); // Becase symbols are probably read  later then expected

            // Update barometer chart
            if (((DateTime.Now.Second > 10) && (DateTime.Now.Minute != BarometerLastMinute)) || BarometerLastMinute == -1)
            {
                if (CalculateBarometer())
                {
                    BarometerTime = _barometerCalculated;
                    BarometerLastMinute = DateTime.Now.Minute;
                    UpdateSymbolPrices();
                }
            }
        }
        catch (Exception ex)
        {
            ScannerLog.Logger.Error(ex, "Error in OnBarometerTimer");
        }


        if (ExchangeBase.KLineTicker != null)
        {
            int count = ExchangeBase.KLineTicker.Count();
            if (KlineTickerCount != count)
                KlineTickerCount = count;
        }
        if (ScannerExecuteCount != SignalExecute.AnalyseCount)
            ScannerExecuteCount = SignalExecute.AnalyseCount;
        if (ScannerSignalCount != GlobalData.CreatedSignalCount)
            ScannerSignalCount = GlobalData.CreatedSignalCount;

        string text = "";
        if (GlobalData.Settings.Trading.Active)
        {
            int positionCount = 0;
            if (GlobalData.ActiveExchange!.Data.PositionList.Count != 0)
            {
                foreach (var position in GlobalData.ActiveExchange!.Data.PositionList.Values)
                {
                    positionCount++;
                }
            }
            text = $"({GlobalData.Settings.Trading.SlotsMaximalLong}/{GlobalData.Settings.Trading.SlotsMaximalShort}) {positionCount}";
        }
        if (ScannerPositionCount != text)
            ScannerPositionCount = text;

        if (CandleProgressText != GlobalData.CandleProgressText)
            CandleProgressText = GlobalData.CandleProgressText;
    }



    public void UpdateSymbolPrices()
    {
        foreach (var symbolViewModel in TopSymbols)
        {
            if (!string.IsNullOrEmpty(symbolViewModel.Name) &&
                GlobalData.ActiveExchange!.SymbolListName.TryGetValue(symbolViewModel.Name, out CryptoSymbol? symbol))
            {
                decimal price = 0;
                if (symbol.LastPrice.HasValue)
                {
                    price = symbol.LastPrice.Value;
                }
                else
                {
                    var symbolInterval = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
                    try
                    {
                        if (symbolInterval.CandleList.Count > 0)
                        {
                            var candle = symbolInterval.CandleList.Last();
                            price = candle.Value.Close;
                        }
                    }
                    catch
                    {
                        // nothing
                    }
                }
                symbolViewModel.Price = price;
                symbolViewModel.Volume = symbol.Volume;
            }
        }
    }


    private static SKColor GetThemeColor(string themeColor)
    {
        var app = Application.Current;
        if (app?.TryGetResource(themeColor, app.ActualThemeVariant, out object? resource) == true)
        {
            if (resource is SolidColorBrush brush)
            {
                var color = brush.Color;
                return new SKColor(color.R, color.G, color.B, color.A);
            }
        }
        return SKColors.White;
    }


    private void CreateBarometerBitmap(CryptoSymbolInterval symbolPeriod)
    {
        int blocks = CryptoScanner.Core.Const.Constants.BarometerGraphHours;

        // Dimensions
        int intWidth = 400;
        int intHeight = 100;

        // Get theme colors
        SKColor bgColor = SKColor.Empty;
        Dispatcher.UIThread.Post(() => { bgColor = GetThemeColor("SystemControlBackgroundAltHighBrush"); });
        //var fgColor = GetForegroundColor();

        var bitmap = new WriteableBitmap(new PixelSize(intWidth, intHeight), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        using var frameBuffer = bitmap.Lock(); // unsafe?

        CryptoCandleList candleList = symbolPeriod.CandleList;
        if (candleList.Count == 0)
            return;

        // Which figure of the measurement is drawn, and how it should be scaled: the average and the
        // median swing around zero, breadth runs 0..100, spread and coin count are never negative.
        // The centered branch below is the original code, unchanged.
        BarometerGraphValue graphValue = BarometerCandleFields.Parse(SelectedGraphValue);
        BarometerGraphScale graphScale = BarometerCandleFields.GetScale(graphValue);

        // determine range of data
        CandleTime loX = CandleTime.MaxValue;
        CandleTime hiX = CandleTime.MinValue;
        float loY = float.MaxValue;
        float hiY = float.MinValue;
        int candleCount = blocks * 60; // minutes
        CandleTime candleTime;
        try
        {
            candleTime = candleList.Keys.Last();
        }
        catch (InvalidOperationException)
        {
            return;
        }
        while (candleCount-- > 0)
        {
            if (candleList.TryGetValue(candleTime, out CryptoCandle candle))
            {
                if (loX > candle!.OpenTime)
                    loX = candle.OpenTime;
                if (hiX < candle.OpenTime)
                    hiX = candle.OpenTime;

                float value = (float)BarometerCandleFields.ReadForGraph(candle, graphValue);

                // Ignore very high barometer values (malfunctions Bybit Futures). Only figures with a
                // ceiling drop their outliers, see BarometerGraphScale.IgnoreBeyond.
                if (!graphScale.IgnoreBeyond.HasValue || Math.Abs(value) < (float)graphScale.IgnoreBeyond.Value)
                {
                    if (loY > value)
                        loY = value;
                    if (hiY < value)
                        hiY = value;
                }
            }
            candleTime -= 1; // The barometer has each 1 minute a barometer value
        }
        if (loX == CandleTime.MaxValue)
            return;
        if (loY == float.MaxValue)
            return;


        // ranges symbolViewModel and y
        float screenX = hiX - loX; // unix time

        // Both branches produce the same two numbers, so the drawing below is shared:
        //   y = offsetY + scaleY * value   (scaleY is flipped below, screen y runs downwards)
        // scaleY is still positive here; offsetY is computed with that positive scale.
        float scaleY, offsetY;
        float gridLow, gridHigh, gridStep;
        float? referenceLine;

        if (graphScale.CenteredOnZero)
        {
            float minimumSpan = (float)graphScale.MinimumSpan;
            float screenY = hiY - loY; // barometer, something like -5 .. +5
            if (screenY < minimumSpan)
                screenY = minimumSpan; // from -2 to +2
            if (hiY > 0.5 * screenY)
                screenY = +2 * hiY;
            if (loY < -0.5 * screenY)
                screenY = -2 * loY;

            scaleY = intHeight / screenY;
            offsetY = scaleY * 0.5f * screenY; // center of picture
            gridLow = (float)graphScale.GridFrom;
            gridHigh = (float)graphScale.GridTo;
            gridStep = (float)graphScale.GridEvery; // 1% per line for the average and the median
            referenceLine = 0f;
        }
        else
        {
            float low = (float?)graphScale.Low ?? loY;
            float high = (float?)graphScale.High ?? hiY;
            if (high <= low)
                high = low + 1f; // a completely flat line still needs a range

            scaleY = intHeight / (high - low);
            offsetY = intHeight + scaleY * low; // value == low lands on the bottom edge
            gridLow = low;
            gridHigh = high;
            gridStep = (float?)graphScale.GridStep ?? (high - low) / 4f;
            referenceLine = (float?)graphScale.ReferenceLine;
        }

        // factor to keep points within picture
        float scaleX = intWidth / screenX;

        // ofset to first point
        float offsetX = 0; // start in the left of the picture

        // flix y (specific for winform - what a crap)
        scaleY = -1 * scaleY;


        // Create SkiaSharp surface
        var info = new SKImageInfo(intWidth, intHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info, frameBuffer.Address, frameBuffer.RowBytes);
        var canvas = surface.Canvas;

        // Clear background
        canvas.Clear(bgColor);

        // Anti-aliasing
        using var paint = new SKPaint
        {
            IsAntialias = true,
            //FilterQuality = SKFilterQuality.High
        };

        // horizontal lines (1% per line for the average and the median, a step that fits the range
        // for the other figures), coloured red on the reference line
        if (gridStep > 0)
        {
            for (float y = gridLow; y <= gridHigh; y += gridStep)
            {
                float screenLineY = offsetY + scaleY * y;
                if (screenLineY < 0 || screenLineY > intHeight)
                    continue; // outside the scale

                SKPoint p1 = new(0, screenLineY);
                SKPoint p2 = new(intWidth, screenLineY);
                if (referenceLine.HasValue && y == referenceLine.Value)
                {
                    paint.Color = SKColors.Red;
                    paint.StrokeWidth = 1;
                    paint.Style = SKPaintStyle.Stroke;
                    canvas.DrawLine(p1, p2, paint);
                }
                else
                {
                    paint.Color = SKColors.Gray;
                    paint.StrokeWidth = 1;
                    paint.Style = SKPaintStyle.Stroke;
                    canvas.DrawLine(p1, p2, paint);
                }
            }
        }

        // Vertical lines (show the hours)
        //Pen pen = new Pen(Color.Gray, 0.5F);
        int intervalTime = 60;
        CandleTime lastX = hiX - (hiX.Minutes % intervalTime);

        // Every hour line carries its hour as a label at the top, in LOCAL time so it matches the
        // timestamp shown next to the chart.
        using var hourFont = new SKFont { Size = 9 };
        using var hourPaint = new SKPaint
        {
            IsAntialias = true,
            Color = SKColors.Gray,
            Style = SKPaintStyle.Fill,
        };

        while (lastX > loX)
        {
            //DateTime ehh = CandleTools.GetUnixDate(lastX);
            //GlobalData.AddTextToLogTab(ehh.ToLocalTime() + " " + lastX.ToString() + " intervaltime=" + intervalTime.ToString());
            SKPoint p1 = new(0, 0) { X = offsetX + scaleX * (float)(lastX - loX) };
            SKPoint p2 = new(0, intHeight) { X = offsetX + scaleX * (float)(lastX - loX) };
            paint.Color = SKColors.Gray;
            paint.StrokeWidth = 1;
            paint.Style = SKPaintStyle.Stroke;
            canvas.DrawLine(p1, p2, paint);

            // The label always sits to the RIGHT of its line, and is simply left out when it no
            // longer fits there - the rightmost hour therefore sometimes has no number. The plate
            // underneath keeps it readable when the barometer line runs along the top of the chart
            // (breadth does). Bare hour, no minutes and no leading zero.
            string hourText = lastX.ToLocalTime().Hour.ToString(System.Globalization.CultureInfo.InvariantCulture);
            float hourWidth = hourFont.MeasureText(hourText);
            float hourX = p1.X + 3;
            if (hourX + hourWidth <= intWidth)
            {
                paint.Color = bgColor;
                paint.Style = SKPaintStyle.Fill;
                canvas.DrawRect(hourX - 2, 1, hourWidth + 4, 11, paint);
                canvas.DrawText(hourText, hourX, 10, SKTextAlign.Left, hourFont, hourPaint);
            }

            lastX -= intervalTime;
        }


        bool init = false;
        SKPoint point1 = new(0, 0);
        SKPoint point2 = new(0, 0);
        candleCount = blocks * 60;
        candleTime = candleList.Values.Last().OpenTime;
        while (candleCount-- > 0)
        {
            if (candleList.TryGetValue(candleTime, out CryptoCandle candle))
            {
                float value = (float)BarometerCandleFields.ReadForGraph(candle!, graphValue);
                point2.X = offsetX + scaleX * (float)(candle!.OpenTime - loX);
                point2.Y = offsetY + scaleY * value;
                //GlobalData.AddTextToLogTab(candle.OhlcText(symbol.DisplayFormat) + " " + point2.X.ToString("N8") + " " + point2.Y.ToString("N8"));

                if (init)
                {
                    // Green above the reference line, red below it. Figures without such a line
                    // (spread) are never good or bad, so those get one neutral colour. Not grey:
                    // that vanished against the grey grid lines on a dark background.
                    if (!referenceLine.HasValue)
                    {
                        paint.Color = SKColors.WhiteSmoke;
                        paint.StrokeWidth = 1;
                        paint.Style = SKPaintStyle.Stroke;
                    }
                    else if (value < referenceLine.Value)
                    {
                        //g.DrawLine(Pens.Red, point1, point2, paint);
                        paint.Color = SKColors.Red;
                        paint.StrokeWidth = 1;
                        paint.Style = SKPaintStyle.Stroke;
                    }
                    else
                    {
                        paint.Color = SKColors.Green;
                        paint.StrokeWidth = 1;
                        paint.Style = SKPaintStyle.Stroke;
                        //g.DrawLine(Pens.DarkGreen, point1, point2, paint);
                    }
                    canvas.DrawLine(point1, point2, paint);
                }

                point1 = point2;
                init = true;
            }
            candleTime -= 1; // The barometer has each 1 minute a barometer value
        }
        //??????? dead code..
        //}
        //else
        //{
        //    // TODO: Een kruis door de bitmap zetten zodat we iets zien (het is nu een lege bitmap)
        //    var info = new SKImageInfo(intWidth, intHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
        //    using var surface = SKSurface.Create(info, frameBuffer.Address, frameBuffer.RowBytes);
        //    var canvas = surface.Canvas;

        //    // Clear background
        //    canvas.Clear(bgColor);
        //}
        ChartImage = bitmap;
    }



    private bool CalculateBarometer()
    {
        try
        {
            if (GlobalData.ApplicationStatus != CryptoApplicationStatus.Running)
                return false;
            if (string.IsNullOrEmpty(SelectedQuote))
                return false;
            if (string.IsNullOrEmpty(SelectedInterval))
                return false;

            // Calculate the latest barometer if needed
            BarometerTools barometerTools = new();
            barometerTools.ExecuteAsync();


            // Update the barometer graph
            if (GlobalData.ActiveExchange == null)
                return false;

            string quoteName = SelectedQuote;
            if (!GlobalData.Settings.QuoteCoins.TryGetValue(quoteName, out CryptoQuoteData? quoteData))
                return false;

            string intervalName = SelectedInterval;
            if (string.IsNullOrEmpty(intervalName))
                return false;

            if (!GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out CryptoInterval? interval))
                return false;

            if (!GlobalData.ActiveExchange.SymbolListName.TryGetValue(CryptoScanner.Core.Const.Constants.SymbolNameBarometerPrice + quoteData.Name, out CryptoSymbol? symbol))
                return false;

            CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
            CreateBarometerBitmap(symbolPeriod);


            // Update the barometer Values
            CryptoIntervalPeriod[] list = [CryptoIntervalPeriod.interval1h, CryptoIntervalPeriod.interval4h, CryptoIntervalPeriod.interval1d];
            foreach (CryptoIntervalPeriod intervalPeriod in list)
            {
                CryptoBarometerData? barometerData = GlobalData.ActiveExchange!.Data.GetBarometer(quoteData.Name, intervalPeriod);
                if (barometerData != null)
                {
                    if (barometerData.PriceBarometer != null)
                    {
                        string tooltip = BarometerCandleFields.Describe(barometerData);
                        if (intervalPeriod == CryptoIntervalPeriod.interval1h)
                        {
                            Barometer1h = barometerData.PriceBarometer.Value;
                            Rising1h = barometerData.PricePercentageRising ?? 0;
                            Barometer1hTooltip = tooltip;
                        }
                        else if (intervalPeriod == CryptoIntervalPeriod.interval4h)
                        {
                            Barometer4h = barometerData.PriceBarometer.Value;
                            Rising4h = barometerData.PricePercentageRising ?? 0;
                            Barometer4hTooltip = tooltip;
                        }
                        else if (intervalPeriod == CryptoIntervalPeriod.interval1d)
                        {
                            Barometer1d = barometerData.PriceBarometer.Value;
                            Rising1d = barometerData.PricePercentageRising ?? 0;
                            Barometer1dTooltip = tooltip;
                        }
                    }
                }
            }


            // Update the barometer time
            try
            {
                if (symbolPeriod.CandleList.Count > 0)
                {
                    CryptoCandle candle = symbolPeriod.CandleList.Values.Last();
                    _barometerCalculated = (candle.OpenTime + 1).ToDateTime().ToLocalTime().ToString("HH:mm");
                }
            }
            catch (InvalidOperationException)
            {
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            return true;
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab(error.ToString());
            return false;
        }
    }


    internal void OnSymbolTapped(DashboardSymbolViewModel symbolViewModel)
    {
        switch (symbolViewModel.Type)
        {
            case IndicatorType.Exchange:
                CryptoExternalUrlType tradingAppInternExtern = CryptoExternalUrlType.External;
                if (GlobalData.Settings.General.TradingApp == CryptoTradingApp.TradingView || GlobalData.Settings.General.TradingApp == CryptoTradingApp.ExchangeUrl)
                    tradingAppInternExtern = GlobalData.Settings.General.TradingAppInternExtern;
                GlobalData.LoadWebLinkConfiguration(); // refresh links

                if (!string.IsNullOrEmpty(symbolViewModel.Symbol) &&
                    GlobalData.ActiveExchange!.SymbolListName.TryGetValue(symbolViewModel.Symbol, out CryptoSymbol? symbol))
                {
                    var interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval15m];
                    CommandHelper.ActivateTradingApp(GlobalData.Settings.General.TradingApp, symbol, interval, tradingAppInternExtern);
                }
                break;
            case IndicatorType.TradingView:
            case IndicatorType.FearAndGreed:
                App.OpenInInternalBrowser(symbolViewModel.GetUrl(), true);
                break;
        }


    }


}