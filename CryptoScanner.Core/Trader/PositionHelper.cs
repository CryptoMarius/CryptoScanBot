
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using System.Text;

namespace CryptoScanner.Core.Trader;

internal static class PositionHelper
{

    public static StringBuilder DumpPosition(this CryptoPosition position)
    {
        StringBuilder stringBuilder = new(); // debug
        stringBuilder.AppendLine($"positie {position.Symbol.Name}");
        foreach (CryptoPositionPart part in position.PartList.Values.ToList())
        {
            stringBuilder.AppendLine($"dca {part.PartNumber} {part.Invested} {part.Commission} {part.CommissionQuote} {part.CommissionBase}");
            foreach (CryptoPositionStep step in part.StepList.Values.ToList())
            {
                stringBuilder.AppendLine($"step {step.Side} {step.Status} {step.OrderId} {step.QuoteQuantityFilled} {step.Commission} {step.CommissionQuote} {step.CommissionBase}");
            }
        }
        stringBuilder.AppendLine($"berekening={position.BreakEvenPrice}=({position.Invested}-{position.Returned}+{position.Commission})/({position.Quantity} + {position.CommissionBase})");
        return stringBuilder;
    }


    public static void ShowPosition(this CryptoPosition position, StringBuilder stringBuilder)
    {
        decimal investedInTrades = position.Invested - position.Returned;
        string s = $"{position.Symbol.Name} {position.Side} {investedInTrades.ToString(position.Symbol.QuoteData.DisplayFormat)} " +
            //$"{position.MarketValue().ToString(position.Symbol.QuoteData.DisplayFormat)} " +
            $"{position.CurrentBreakEvenPercentage():N2}%";

        if (position.PartCount > 0)
            s += " " + position.PartCountText();
        stringBuilder.AppendLine(s);
    }


    public static void ShowPositions(this IDictionary<string, CryptoPosition> positionList, StringBuilder stringBuilder)
    {
        int positionTotal = 0;
        if (GlobalData.ActiveExchange != null)
        {
            if (positionList.Count != 0)
            {
                int positionCount = 0;
                // The position list is a ConcurrentDictionary, so its own enumeration order is hash
                // order and tells the reader nothing. Sort on the very percentage ShowPosition prints,
                // best first, so the winners are at the top and the losers at the bottom.
                foreach (var position in positionList.Values.OrderByDescending(position => position.CurrentBreakEvenPercentage()))
                {
                    //De muntparen toevoegen aan de userinterface
                    position.ShowPosition(stringBuilder);
                    positionCount++;
                    positionTotal++;
                }
                stringBuilder.AppendLine(string.Format("{0} positions", positionCount));
            }
        }
        if (positionTotal == 0)
            stringBuilder.AppendLine("no positions");
    }

}
