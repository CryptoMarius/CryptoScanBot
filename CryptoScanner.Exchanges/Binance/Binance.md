# Binance

Notes about placing orders on Binance. These used to sit as a comment block at the top of
`Spot/Api.cs` and `Perpetual/Api.cs`; they say nothing about the scanner itself, only about what
the exchange answers when an order is rejected, so they live here instead.

Error code overview:
https://toscode.gitee.com/purplecity/binance-official-api-docs/blob/d5bab6053da63aecd71ed6393fbd7de1da88a43a/errors.md

## Errors while buying (or selling)

| Error | Meaning |
| --- | --- |
| `Filter failure: MIN_NOTIONAL` | The amount is too low: price * quantity is too low to be a valid order for the symbol. |
| `-1111: Precision is over the maximum defined for this asset.` | Too many decimals, on the price as well as on the quantity. |
| `-1013: Filter failure: PRICE_FILTER` | There is not enough money to place the order. |
| `-1013: Filter failure: LOT_SIZE` | The quantity is outside the allowed step size or range. |

## OCO price rules

"The relationship of the prices for the orders is not correct" means the prices set in the OCO
break the price rules:

* **Sell orders**: limit price > last price > stop price
* **Buy orders**: limit price < last price < stop price

In practice this shows up when the price has already moved past the chosen sell price by the time
the order is placed (or has already dropped below the stop price).

## What an OCO answers

The response carries two order reports:

* the 1st order is the stop loss, recognisable by `"type": "STOP_LOSS"`
* the 2nd order is the normal sell, recognisable by `"type": "LIMIT_MAKER"`

One of the two has a price and a stop price, the other one only a price.
See https://github.com/binance/binance-spot-api-docs/blob/master/rest-api.md
