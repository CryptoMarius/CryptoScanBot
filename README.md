CryptoScanBot signal scanner for the following exchanges:
- Binance Spot and Perpetual
- Bitmart Spot and Perpetual
- Bitvavo Spot
- BloFin Perpetual
- Bybit Spot and Perpetual
- Bybit EU Spot
- Coinbase Spot
- HyperLiquid Spot and Perpetual
- Kraken Spot and Perpetual
- Kucoin Spot and Perpetual
- Mexc Spot and Perpetual (the perpetual market has no api-order endpoints)
- OKX Spot, Perpetual and XPerp (the USD_UM contracts, settled in USD value and payable in USDC)

And, not a crypto exchange but plugged in as one so the same analyzers can run on US equities:
- Alpaca (paper trading, an api key is mandatory even for market data)

The Crypto scanner was initially only intended to generate oversold signals on the Binance exchange (because someone said something about DYOR and you shouldn't say that to a programmer). In the meantime, the application has been overhauled a number of times, split, merged the best points, improved, simplified, adapted for SBM signals and made multi-exchange.

The purpose of this application is to generate 3 types of signals (STOBB, SBM and JUMP). These signals can be used to enter the crypto market on predetermined conditions. With all these signals, only certain conditions have occurred, always validate the market and currency conditions before you get in anything. In particular, the PSAR is calculated differently by TradinView and the SBM lines always have to be interpreted by a human.

In latest editions we also try to show dominant zones and FVG (see chart form).

The list at the top is the current state, all of those are switched on and scanning. A few more are in the source but switched off, because they cannot deliver what the scanner needs: BitMart floods the log with rate limit errors, Coinbase only streams 5m candles which is too coarse, BloFin has no spot client in the library we use, and the european Bybit entity lists no futures contracts at all. The application is built in a mix of English and Dutch, because a number of tools have been combined (please indicate whether any texts are disturbing and/or should be adjusted), so apologies in advance for English crypto terms, for an explanation you have to be on the internet or ask in a crypto group what it means (but always do your own research first).

Furthermore: Very nice that you try this application, below is an explanation of what the application does, the installation, necessary settings and so on. I hope you enjoy trading, the communities and so on. Good luck in this special world!

<img width="1464" height="901" alt="Main screen + signals" src="https://github.com/user-attachments/assets/8e40ea81-7403-4400-ada2-9ef783b6941e" />
