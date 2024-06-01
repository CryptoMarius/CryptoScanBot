﻿using CryptoScanBot.Core.Model;


//https://github.com/StockSharp/StockSharp/blob/master/Algo/Indicators/ZigZag.cs

namespace CryptoScanBot.Core.Trend;

/// <summary>
/// ZigZag.
/// </summary>
/// <remarks>
/// ZigZag traces and combines extreme points of the chart, distanced for not less than specified percentage by the price scale.
/// </remarks>
public class ZigZagIndicator
{
    public readonly List<CryptoCandle> Candles = [];
    public readonly List<decimal> _lowBuffer = [];
    public readonly List<decimal> _highBuffer = [];
    public readonly List<decimal> _zigZagBuffer = [];

    private Func<CryptoCandle, decimal> _currentValue = candle => candle.Close;
    private int _depth;
    private int _backStep;
    private bool _needAdd = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZigZag"/>.
    /// </summary>
    public ZigZagIndicator()
    {
        BackStep = 3;
        Depth = 12;
    }

    /// <summary>
    /// Minimum number of bars between local maximums, minimums.
    /// </summary>
    public int BackStep
    {
        get => _backStep;
        set
        {
            if (_backStep == value)
                return;

            _backStep = value;
            Reset();
        }
    }

    /// <summary>
    /// Bars minimum, on which Zigzag will not build a second maximum (or minimum), if it is smaller (or larger) by a deviation of the previous respectively.
    /// </summary>
    public int Depth
    {
        get => _depth;
        set
        {
            if (_depth == value)
                return;

            _depth = value;
            Reset();
        }
    }

    //private Unit _deviation = new(0.45m, UnitTypes.Percent);

    ///// <summary>
    ///// Minimum number of points between maximums (minimums) of two adjacent bars used by Zigzag indicator to form a local peak (local trough).
    ///// </summary>
    //public Unit Deviation
    //{
    //    get => _deviation;
    //    set
    //    {
    //        if (value == null)
    //            throw new ArgumentNullException(nameof(value));

    //        if (_deviation == value)
    //            return;

    //        _deviation = value;
    //        Reset();
    //    }
    //}

    private Func<CryptoCandle, decimal> _highValue = candle => Math.Max(candle.Open, candle.Close); // candle.High;
    /// <summary>
    /// The converter, returning from the candle a price for search of maximum.
    /// </summary>
    public Func<CryptoCandle, decimal> HighValueFunc
    {
        get => _highValue;
        set
        {
            _highValue = value;
            Reset();
        }
    }

    private Func<CryptoCandle, decimal> _lowValue = candle => Math.Min(candle.Open, candle.Close); // candle.Low;
    /// <summary>
    /// The converter, returning from the candle a price for search of minimum.
    /// </summary>
    public Func<CryptoCandle, decimal> LowValueFunc
    {
        get => _lowValue;
        set
        {
            _lowValue = value;
            Reset();
        }
    }

    /// <summary>
    /// The converter, returning from the candle a price for the current value.
    /// </summary>
    public Func<CryptoCandle, decimal> CurrentValueFunc
    {
        get => _currentValue;
        set
        {
            _currentValue = value;
            Reset();
        }
    }

    /// <summary>
    /// The indicator current value.
    /// </summary>
    public decimal CurrentValue { get; private set; }

    /// <summary>
    /// Shift for the last indicator value.
    /// </summary>
    public int LastValueShift { get; private set; }

    /// <inheritdoc />
    public void Reset()
    {
        _needAdd = true;
        Candles.Clear();
        _lowBuffer.Clear();
        _highBuffer.Clear();
        _zigZagBuffer.Clear();
        CurrentValue = 0;
        LastValueShift = 0;
        //base.Reset();
    }

    /// <inheritdoc />
    public void Calculate(CryptoCandle candle, bool isFinal)
    {
        //var candle = input.GetValue<CryptoCandle>();

        // Opmerking: De nieuwste candle staat vooraan in de lijst (even wennen)
        // En qua lijst management niet zo handig (iedere keer alles verschuiven?)

        if (_needAdd)
        {
            Candles.Insert(0, candle);
            _lowBuffer.Insert(0, 0);
            _highBuffer.Insert(0, 0);
            _zigZagBuffer.Insert(0, 0);
        }
        else
        {
            Candles[0] = candle;
            _lowBuffer[0] = 0;
            _highBuffer[0] = 0;
            _zigZagBuffer[0] = 0;
        }

        const int level = 3;
        int limit;
        decimal lastHigh = 0;
        decimal lastLow = 0;

        if (Candles.Count - 1 == 0)
        {
            limit = Candles.Count - Depth;
        }
        else
        {
            int i = 0, count = 0;
            while (count < level && i < Candles.Count - Depth)
            {
                var res = _zigZagBuffer[i];
                if (res != 0)
                {
                    count++;
                }
                i++;
            }
            limit = --i;
        }

        for (var shift = limit; shift >= 0; shift--)
        {
            //--- low
            var val = Candles.Skip(shift).Take(Depth).Min(v => _lowValue(v));
            if (val == lastLow)
            {
                val = 0.0m;
            }
            else
            {
                lastLow = val;
                if (_lowValue(Candles[shift]) - val > 0.0m * val / 100)
                {
                    val = 0.0m;
                }
                else
                {
                    for (var back = 1; back <= BackStep; back++)
                    {
                        if (shift + back < _lowBuffer.Count)//fix
                        {
                            var res = _lowBuffer[shift + back];
                            if (res != 0 && res > val)
                            {
                                _lowBuffer[shift + back] = 0.0m;
                            }
                        }
                    }
                }
            }
            if (_lowValue(Candles[shift]) == val)
                _lowBuffer[shift] = val;
            else
                _lowBuffer[shift] = 0m;

            //--- high
            val = Candles.Skip(shift).Take(Depth).Max(v => _highValue(v));
            if (val == lastHigh)
            {
                val = 0.0m;
            }
            else
            {
                lastHigh = val;
                if (val - _highValue(Candles[shift]) > 0.0m * val / 100)
                {
                    val = 0.0m;
                }
                else
                {
                    for (var back = 1; back <= BackStep; back++)
                    {
                        if (shift + back < _lowBuffer.Count) //fix
                        {
                            var res = _highBuffer[shift + back];
                            if (res != 0 && res < val)
                            {
                                _highBuffer[shift + back] = 0.0m;
                            }
                        }
                    }
                }
            }
            if (_highValue(Candles[shift]) == val)
                _highBuffer[shift] = val;
            else
                _highBuffer[shift] = 0m;
        }

        // final cutting 
        lastHigh = -1;
        lastLow = -1;
        var lastHighPos = -1;
        var lastLowPos = -1;

        for (var shift = limit; shift >= 0; shift--)
        {
            var curLow = _lowBuffer[shift];
            var curHigh = _highBuffer[shift];

            if (curLow == 0 && curHigh == 0)
                continue;

            //---
            if (curHigh != 0)
            {
                if (lastHigh > 0)
                {
                    if (lastHigh < curHigh)
                    {
                        _highBuffer[lastHighPos] = 0;
                    }
                    else
                    {
                        _highBuffer[shift] = 0;
                    }
                }
                //---
                if (lastHigh < curHigh || lastHigh < 0)
                {
                    lastHigh = curHigh;
                    lastHighPos = shift;
                }
                lastLow = -1;
            }

            //----
            if (curLow != 0)
            {
                if (lastLow > 0)
                {
                    if (lastLow > curLow)
                    {
                        _lowBuffer[lastLowPos] = 0;
                    }
                    else
                    {
                        _lowBuffer[shift] = 0;
                    }
                }
                //---
                if (curLow < lastLow || lastLow < 0)
                {
                    lastLow = curLow;
                    lastLowPos = shift;
                }
                lastHigh = -1;
            }
        }

        for (var shift = limit; shift >= 0; shift--)
        {
            if (shift >= Candles.Count - Depth)
            {
                _zigZagBuffer[shift] = 0.0m;
            }
            else
            {
                var res = _highBuffer[shift];
                if (res != 0.0m)
                {
                    _zigZagBuffer[shift] = res;
                }
                else
                {
                    _zigZagBuffer[shift] = _lowBuffer[shift];
                }
            }
        }

        int valuesCount = 0, valueId = 0;

        for (; valueId < _zigZagBuffer.Count && valuesCount < 2; valueId++)
        {
            if (_zigZagBuffer[valueId] != 0)
                valuesCount++;
        }

        _needAdd = isFinal; // input.IsFinal;

        if (valuesCount != 2)
            return; // new DecimalIndicatorValue(this);

        //if (isFinal) //input.IsFinal
        //IsFormed = true;

        LastValueShift = valueId - 1;

        CurrentValue = _currentValue(Candles[0]);
        return;

        //return new DecimalIndicatorValue(this, _zigZagBuffer[LastValueShift]);
    }

    ///// <inheritdoc />
    //public override void Load(SettingsStorage storage)
    //{
    //	base.Load(storage);

    //	BackStep = storage.GetValue<int>(nameof(BackStep));
    //	Depth = storage.GetValue<int>(nameof(Depth));
    //	Deviation.Load(storage.GetValue<SettingsStorage>(nameof(Deviation)));
    //}

    ///// <inheritdoc />
    //public override void Save(SettingsStorage storage)
    //{
    //	base.Save(storage);

    //	storage.SetValue(nameof(BackStep), BackStep);
    //	storage.SetValue(nameof(Depth), Depth);
    //	storage.SetValue(nameof(Deviation), Deviation.Save());
}