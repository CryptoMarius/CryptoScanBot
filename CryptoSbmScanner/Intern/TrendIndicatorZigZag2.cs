using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Intern;

// https://ctrader.com/algos/indicators/show/157

public class TrendIndicatorZigZag2
{

    //[Parameter(DefaultValue = 12)]
    private int Depth { get; set; } = 12;

    //[Parameter(DefaultValue = 5)]
    private int Deviation { get; set; } = 5;

    //[Parameter(DefaultValue = 3)]
    private int BackStep { get; set; } = 3;

    //[Output("ZigZag", Color = Colors.OrangeRed)]
    //public IndicatorDataSeries Result { get; set; }
    public List<double> Result;


    private double _lastLow;
    private double _lastHigh;
    private double _low;
    private double _high;
    private int _lastHighIndex;
    private int _lastLowIndex;

    private int _type;
    private readonly double _point;
    private double _currentLow;
    private double _currentHigh;

    public bool UseOpenClose = true;
    public List<double> _lowZigZags;
    public List<double> _highZigZags;
    public List<CryptoCandle> CandleList;

    public TrendIndicatorZigZag2(CryptoSymbol symbol, List<CryptoCandle> candleList, int backStep, int depth, int deviation)
    {
        _point = (double)symbol.PriceTickSize;
        //If the symbol is a 5 digit symbol, the point size is 0.00001

        Result = new List<double>();
        _lowZigZags = new List<double>();
        _highZigZags = new List<double>();

        CandleList = candleList;
        BackStep = backStep;
        Depth = depth;
        Deviation = deviation;
    }



    public void Calculate(int index)
    {
        CryptoCandle candle = CandleList[index];

        Result.Add(0);
        _lowZigZags.Add(0);
        _highZigZags.Add(0);

        // De aanloop periode
        if (index < Depth)
            return;

        //returns the lowest and largest number in the Series in the range [Series[index-Period], Series[index]]
        //_currentLow = Functions.Minimum(MarketSeries.Low, Depth);
        //_currentHigh = MarketSeries.High.Maximum(Depth);
        decimal value;
        _currentLow = double.MaxValue;
        _currentHigh = double.MinValue;
        for (int i = index - Depth; i <= index; i++)
        {
            CryptoCandle c = CandleList[i];
            if (UseOpenClose)
            {
                value = Math.Min(c.Open, c.Close);
                if ((double)value < _currentLow)
                    _currentLow = (double)value;
                value = Math.Max(c.Open, c.Close);
                if ((double)value > _currentHigh)
                    _currentHigh = (double)value;
            }
            else
            {
                if ((double)c.Low < _currentLow)
                    _currentLow = (double)c.Low;
                if ((double)c.High > _currentHigh)
                    _currentHigh = (double)c.High;
            }
        }


        //_currentLow = Functions.Minimum(MarketSeries.Low, Depth);
        if (UseOpenClose)
            value = Math.Min(candle.Open, candle.Close);
        else
            value = candle.Low;
        if (Math.Abs(_currentLow - _lastLow) < double.Epsilon)
            _currentLow = 0.0;
        else
        {
            _lastLow = _currentLow;

            if ((double)value - _currentLow > Deviation * _point)
                _currentLow = 0.0;
            else
            {
                for (int i = 1; i <= BackStep; i++)
                {
                    if (Math.Abs(_lowZigZags[index - i]) > double.Epsilon && _lowZigZags[index - i] > _currentLow)
                        _lowZigZags[index - i] = 0.0;
                }
            }
        }
        if (Math.Abs((double)value - _currentLow) < double.Epsilon)
            _lowZigZags[index] = _currentLow;
        else
            _lowZigZags[index] = 0.0;



        //_currentHigh = MarketSeries.High.Maximum(Depth);
        if (UseOpenClose)
            value = Math.Max(candle.Open, candle.Close);
        else
            value = candle.High;
        if (Math.Abs(_currentHigh - _lastHigh) < double.Epsilon)
            _currentHigh = 0.0;
        else
        {
            _lastHigh = _currentHigh;

            if (_currentHigh - (double)value > Deviation * _point)
                _currentHigh = 0.0;
            else
            {
                for (int i = 1; i <= BackStep; i++)
                {
                    if (Math.Abs(_highZigZags[index - i]) > double.Epsilon && _highZigZags[index - i] < _currentHigh)
                        _highZigZags[index - i] = 0.0;
                }
            }
        }
        if (Math.Abs((double)value - _currentHigh) < double.Epsilon)
            _highZigZags[index] = _currentHigh;
        else
            _highZigZags[index] = 0.0;




        switch (_type)
        {
            case 0:
                if (Math.Abs(_low - 0) < double.Epsilon && Math.Abs(_high - 0) < double.Epsilon)
                {
                    if (Math.Abs(_highZigZags[index]) > double.Epsilon)
                    {
                        if (UseOpenClose)
                            value = Math.Max(candle.Open, candle.Close);
                        else
                            value = candle.High;
                        _high = (double)value;
                        _lastHighIndex = index;
                        _type = -1;
                        Result[index] = _high;
                        //Result[index].Value = _high;
                        //Result[index].PointType = "H";
                    }
                    if (Math.Abs(_lowZigZags[index]) > double.Epsilon)
                    {
                        if (UseOpenClose)
                            value = Math.Min(candle.Open, candle.Close);
                        else
                            value = candle.Low;
                        _low = (double)value;
                        _lastLowIndex = index;
                        _type = 1;
                        Result[index] = _low;
                        //Result[index].Value = _low;
                        //Result[index].PointType = "L";
                    }
                }
                break;
            case 1:
                if (Math.Abs(_lowZigZags[index]) > double.Epsilon && _lowZigZags[index] < _low && Math.Abs(_highZigZags[index] - 0.0) < double.Epsilon)
                {
                    Result[_lastLowIndex] = double.NaN;
                    //Result[_lastLowIndex].PointType = "";
                    //Result[_lastLowIndex].Value = double.NaN;
                    _lastLowIndex = index;
                    _low = _lowZigZags[index];
                    Result[index] = _low;
                    //Result[index].Value = _low;
                    //Result[index].PointType = "L";
                }
                if (Math.Abs(_highZigZags[index] - 0.0) > double.Epsilon && Math.Abs(_lowZigZags[index] - 0.0) < double.Epsilon)
                {
                    _high = _highZigZags[index];
                    _lastHighIndex = index;
                    Result[index] = _high;
                    //Result[index].Value = _high;
                    //Result[index].PointType = "H";
                    _type = -1;
                }
                break;
            case -1:
                if (Math.Abs(_highZigZags[index]) > double.Epsilon && _highZigZags[index] > _high && Math.Abs(_lowZigZags[index] - 0.0) < double.Epsilon)
                {
                    Result[_lastHighIndex] = double.NaN;
                    //Result[_lastHighIndex].PointType = "";
                    //Result[_lastHighIndex].Value = double.NaN;
                    _lastHighIndex = index;
                    _high = _highZigZags[index];
                    Result[index] = _high;
                    //Result[index].Value = _high;
                    //Result[index].PointType = "H";
                }
                if (Math.Abs(_lowZigZags[index]) > double.Epsilon && Math.Abs(_highZigZags[index]) <= double.Epsilon)
                {
                    _low = _lowZigZags[index];
                    _lastLowIndex = index;
                    Result[index] = _low;
                    //Result[index].Value = _low;
                    //Result[index].PointType = "L";
                    _type = 1;
                }
                break;
            default: return;
        }

    }
}