using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Signal;

public class SignalHelper
{
    public static SignalBase GetSignalAlgorithm(SignalStrategy Strategy, CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle)
    {
        SignalBase algorithm;
        algorithm = Strategy switch
        {
            SignalStrategy.stobbOversold => new SignalStobbOversold(symbol, interval, candle),
            SignalStrategy.sbm1Oversold => new SignalSbm1Oversold(symbol, interval, candle),
            SignalStrategy.sbm2Oversold => new SignalSbm2Oversold(symbol, interval, candle),
            SignalStrategy.sbm3Oversold => new SignalSbm3Oversold(symbol, interval, candle),
            //SignalStrategy.sbm4Oversold => new SignalSbm4Oversold(symbol, interval, candle),
            //SignalStrategy.sbm5Oversold => new SignalSbm5Oversold(symbol, interval, candle),

            SignalStrategy.stobbOverbought => new SignalStobbOverbought(symbol, interval, candle),
            SignalStrategy.sbm1Overbought => new SignalSbm1Overbought(symbol, interval, candle),
            SignalStrategy.sbm2Overbought => new SignalSbm2Overbought(symbol, interval, candle),
            SignalStrategy.sbm3Overbought => new SignalSbm3Overbought(symbol, interval, candle),
            //SignalStrategy.sbm4Overbought => new SignalSbm4Overbought(symbol, interval, candle),
            //SignalStrategy.sbm5Overbought => new SignalSbm5Overbought(symbol, interval, candle),

            SignalStrategy.priceCrossedEma20 => new SignalPriceCrossedEma20(symbol, interval, candle),
            SignalStrategy.priceCrossedSma20 => new SignalPriceCrossedSma20(symbol, interval, candle),
            SignalStrategy.priceCrossedEma50 => new SignalPriceCrossedEma50(symbol, interval, candle),
            SignalStrategy.priceCrossedSma50 => new SignalPriceCrossedSma50(symbol, interval, candle),
            //SignalStrategy.strategyBullishEngulfing=>new SignalBullishEngulfing(symbol, interval, candle),
            //SignalStrategy.strategyBearischEngulfing=>new SignalBearischEngulfing(symbol, interval, candle),
            SignalStrategy.slopeEma50TurningPositive => new SignalSlopeEma50TurningPositive(symbol, interval, candle),
            SignalStrategy.slopeSma50TurningPositive => new SignalSlopeSma50TurningPositive(symbol, interval, candle),
            SignalStrategy.slopeEma20TurningPositive => new SignalSlopeEma20TurningPositive(symbol, interval, candle),
            SignalStrategy.slopeSma20TurningPositive => new SignalSlopeSma20TurningPositive(symbol, interval, candle),

            SignalStrategy.slopeEma50TurningNegative => new SignalSlopeEma50TurningNegative(symbol, interval, candle),
            SignalStrategy.slopeSma50TurningNegative => new SignalSlopeSma50TurningNegative(symbol, interval, candle),
            _ => null
        };

        return algorithm;
    }


    public static string GetSignalAlgorithmText(SignalStrategy Strategy)
    {
        string text = Strategy switch
        {
            SignalStrategy.candlesJumpUp => "jump+",
            SignalStrategy.candlesJumpDown => "jump-",
            SignalStrategy.stobbOverbought => "stobb+",
            SignalStrategy.stobbOversold => "stobb-",
            SignalStrategy.sbm1Overbought => "sbm+",
            SignalStrategy.sbm1Oversold => "sbm-",
            SignalStrategy.sbm2Overbought => "sbm2+",
            SignalStrategy.sbm2Oversold => "sbm2-",
            SignalStrategy.sbm3Overbought => "sbm3+",
            SignalStrategy.sbm3Oversold => "sbm3-",
            SignalStrategy.sbm4Overbought => "sbm4+",
            SignalStrategy.sbm4Oversold => "sbm4-",
            SignalStrategy.sbm5Overbought => "sbm5+",
            SignalStrategy.sbm5Oversold => "sbm5-",
            SignalStrategy.priceCrossedEma20 => "close>ema20",
            SignalStrategy.priceCrossedEma50 => "close>ema50",
            SignalStrategy.priceCrossedSma20 => "close>sma20",
            SignalStrategy.priceCrossedSma50 => "close>sma50",
            SignalStrategy.bullishEngulfing => "Bullish engulf",
            SignalStrategy.slopeSma50TurningNegative => "sma 50 slope negative",
            SignalStrategy.slopeSma50TurningPositive => "sma 50 slope positive",
            SignalStrategy.slopeEma50TurningNegative => "ema 50 slope negative",
            SignalStrategy.slopeEma50TurningPositive => "ema 50 slope positive",
            SignalStrategy.slopeEma20TurningPositive => "ema 20 slope positive",
            SignalStrategy.slopeSma20TurningPositive => "sma 20 slope positive",
            _ => Strategy.ToString(),
        };
        return text;
    }

}

