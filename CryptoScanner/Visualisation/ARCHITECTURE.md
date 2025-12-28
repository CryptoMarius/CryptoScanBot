# Crypto Visualisation - Architecture Guide

Complete MVVM architecture for chart visualisation window.

## Project Structure

```
Visualisation/
├── ViewModels/
│   ├── VisualisationViewModel.cs          - Main ViewModel (orchestrator)
│   ├── SymbolSelectorViewModel.cs         - Symbol/Interval selection
│   ├── TrendSettingsViewModel.cs          - Trend display settings
│   ├── FibSettingsViewModel.cs            - Fibonacci settings
│   ├── DisplayOptionsViewModel.cs         - Chart display toggles
│   └── PlaybackControlsViewModel.cs       - Navigation controls
└── Views/
    ├── VisualisationWindow.axaml          - Main window
    ├── VisualisationWindow.axaml.cs
    ├── SymbolSelectorView.axaml           - Symbol selector component
    └── SymbolSelectorView.axaml.cs
```

## Architecture Pattern: **Composite ViewModel**

### Main ViewModel (VisualisationViewModel)
**Responsibility:** Orchestrates all sub-ViewModels and handles business logic

```csharp
public partial class VisualisationViewModel : ObservableObject
{
    // Sub-ViewModels
    [ObservableProperty] private SymbolSelectorViewModel _symbolSelector;
    [ObservableProperty] private TrendSettingsViewModel _trendSettings;
    // etc...

    // OxyPlot Model
    [ObservableProperty] private PlotModel _plotModel;
    
    public VisualisationViewModel()
    {
        // Initialize sub-ViewModels
        _symbolSelector = new SymbolSelectorViewModel();
        
        // Subscribe to changes
        SymbolSelector.PropertyChanged += OnSymbolChanged;
    }
}
```

### Sub-ViewModels Pattern
Each sub-ViewModel:
1. Manages its own state (properties)
2. Loads/Saves from ZoneSession
3. Raises PropertyChanged events
4. Is independent and reusable

```csharp
public partial class SymbolSelectorViewModel : ObservableObject
{
    [ObservableProperty] private string _selectedBase;
    
    public void LoadFromSession(ZoneSession session) { }
    public void SaveToSession(ZoneSession session) { }
}
```

## Communication Flow

### 1. User Changes Symbol
```
User selects new symbol in UI
    ↓
SymbolSelectorViewModel.SelectedBase changes
    ↓
PropertyChanged event raised
    ↓
VisualisationViewModel.OnSymbolChanged() handler
    ↓
RefreshCommand.Execute()
    ↓
Calculate() reloads data
    ↓
PlotModel updated
    ↓
UI refreshes automatically
```

### 2. User Changes Display Option
```
User toggles "Show Bollinger Band"
    ↓
DisplayOptionsViewModel.ShowBollingerBand changes
    ↓
PropertyChanged event raised
    ↓
VisualisationViewModel.OnDisplayOptionsChanged()
    ↓
RefreshPlot() called
    ↓
PlotModel.InvalidatePlot(true)
    ↓
UI refreshes
```

## OxyPlot Integration

### Package Required
```xml
<PackageReference Include="OxyPlot.Avalonia" Version="2.2.0" />
```

### XAML Usage
```xml
<oxy:PlotView Model="{Binding PlotModel}"/>
```

### ViewModel Usage
```csharp
private PlotModel _plotModel;

private void InitializePlot()
{
    PlotModel = new PlotModel { Title = "Chart" };
    
    // Add axes
    PlotModel.Axes.Add(new DateTimeAxis { ... });
    PlotModel.Axes.Add(new LinearAxis { ... });
    
    // Add series
    var candleSeries = new CandleStickSeries();
    PlotModel.Series.Add(candleSeries);
}

private void RefreshPlot()
{
    PlotModel.InvalidatePlot(true);
}
```

## Session Management

### ZoneSession Pattern
```csharp
// Load on startup
private void LoadSession()
{
    _session = ZoneSession.LoadSessionSettings();
    SymbolSelector.LoadFromSession(_session);
    TrendSettings.LoadFromSession(_session);
    // etc...
}

// Save on close
public void OnClosing()
{
    SymbolSelector.SaveToSession(_session);
    TrendSettings.SaveToSession(_session);
    ZoneSession.SaveSessionSettings(_session);
}
```

## Implementing Missing Logic

### Step 1: Add Chart Drawing Logic
In `VisualisationViewModel.Calculate()`:

```csharp
private void Calculate(bool recalculate)
{
    // 1. Get symbol and interval
    var symbol = SymbolSelector.SelectedSymbol;
    var interval = SymbolSelector.SelectedInterval;
    
    // 2. Load candle data
    var candles = LoadCandles(symbol, interval);
    
    // 3. Calculate zones if needed
    if (recalculate)
    {
        _data = CalculateZones(candles);
    }
    
    // 4. Draw chart
    DrawCandles(candles);
    if (DisplayOptions.ShowDlzZones) DrawDlzZones();
    if (DisplayOptions.ShowBollingerBand) DrawBollingerBands();
    // etc...
    
    RefreshPlot();
}
```

### Step 2: Port Drawing Methods
From original WinForms code, port these methods:

```csharp
private void DrawCandles(List<Candle> candles)
{
    var series = new CandleStickSeries
    {
        Color = OxyColors.Green,
        IncreasingColor = OxyColors.Green,
        DecreasingColor = OxyColors.Red
    };
    
    foreach (var candle in candles)
    {
        series.Items.Add(new HighLowItem(
            DateTimeAxis.ToDouble(candle.OpenTime),
            candle.High,
            candle.Low,
            candle.Open,
            candle.Close
        ));
    }
    
    PlotModel.Series.Add(series);
}

private void DrawDlzZones()
{
    // Port zone drawing logic from original
    foreach (var zone in _data.Zones)
    {
        var annotation = new RectangleAnnotation
        {
            MinimumX = DateTimeAxis.ToDouble(zone.StartTime),
            MaximumX = DateTimeAxis.ToDouble(zone.EndTime),
            MinimumY = zone.Low,
            MaximumY = zone.High,
            Fill = OxyColor.FromArgb(50, 0, 128, 255)
        };
        PlotModel.Annotations.Add(annotation);
    }
}
```

## Adding More Views

### Template for New Component

**1. ViewModel:**
```csharp
public partial class NewFeatureViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _someProperty;
    
    public void LoadFromSession(ZoneSession session)
    {
        SomeProperty = session.SomeValue;
    }
    
    public void SaveToSession(ZoneSession session)
    {
        session.SomeValue = SomeProperty;
    }
}
```

**2. View:**
```xml
<UserControl ...>
    <HeaderedContentControl Classes="groupbox" Header="New Feature">
        <StackPanel>
            <CheckBox IsChecked="{Binding SomeProperty}"/>
        </StackPanel>
    </HeaderedContentControl>
</UserControl>
```

**3. Integrate in Main ViewModel:**
```csharp
[ObservableProperty]
private NewFeatureViewModel _newFeature;

public VisualisationViewModel()
{
    _newFeature = new NewFeatureViewModel();
    NewFeature.PropertyChanged += OnNewFeatureChanged;
}
```

**4. Add to Main View:**
```xml
<views:NewFeatureView DataContext="{Binding NewFeature}"/>
```

## Benefits of This Architecture

✓ **Modular** - Each component is independent
✓ **Testable** - ViewModels can be unit tested
✓ **Maintainable** - Clear separation of concerns
✓ **Reusable** - Components can be used elsewhere
✓ **Scalable** - Easy to add new features

## Next Steps

1. Install **OxyPlot.Avalonia** NuGet package
2. Port candle loading logic from original code
3. Port zone calculation logic
4. Port drawing methods one by one
5. Test with real data

## Common Pitfalls

❌ **Don't:** Put all logic in code-behind
✓ **Do:** Keep logic in ViewModels

❌ **Don't:** Directly manipulate PlotModel from View
✓ **Do:** Expose PlotModel via ViewModel

❌ **Don't:** Subscribe to events without unsubscribing
✓ **Do:** Unsubscribe in OnClosing or use WeakEventManager

❌ **Don't:** Block UI thread with heavy calculations
✓ **Do:** Use async/await for long operations
