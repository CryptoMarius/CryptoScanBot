# Bybit

## Legacy v2 endpoints

Scratch notes from an early exploration of the (now legacy) v2 api, kept for reference. They ended
up in the Binance `Api.cs` files by copy/paste and were moved here. The scanner talks to the v5 api
through `Bybit.Net`, so nothing below is in use.

https://bybit-exchange.github.io/docs-legacy/futuresV2/inverse/

| What | Endpoint | Answer |
| --- | --- | --- |
| Server time | `/v2/public/time` | `{"ret_code":0,"ret_msg":"OK","result":{},"ext_code":"","ext_info":"","time_now":"1688116858.760925"}` |
| Announcement | `/v2/public/announcement` | `{"ret_code":0,"ret_msg":"OK","result":[],"ext_code":"","ext_info":"","time_now":"1688116961.886013"}` (looks a lot like the first one) |
| Kline | `/v2/public/kline/list?symbol=BTCUSDT&interval=1` | without a symbol: `{"retCode":10001,"retMsg":"The requested symbol is invalid.",...}` |
| Symbols | `/spot/v3/public/symbols` | mind the version differences |

## Placing orders

A market order on the v5 api takes price * quantity instead of the quantity, see the example at
https://bybit-exchange.github.io/docs/v5/order/create-order

An OCO deviates from a standard buy or sell and is not implemented. On Binance an OCO was a
completely different call with its own parameters and results; on Bybit it would be an order with
`orderFilter: OcoOrder` plus a trigger price. `PlaceOrder` throws for both `Oco` and `StopLimit`.

Cancelling an order that no longer exists answers error code `110001` ("Order does not exist").
`Cancel` treats that as success, because the goal - the order is gone - has been reached.
