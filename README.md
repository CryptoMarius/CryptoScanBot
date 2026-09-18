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

Furthermore: Very nice that you try this application, in the wiki you can find an explanation of what the application does, the installation, necessary settings etc.. I hope you enjoy trading, the communities and have some luck in this special world!

<img width="1920" height="1200" alt="Main screen + signals" src="https://github.com/user-attachments/assets/7f50955d-66bd-416c-b9cf-b7cd1eee4b29" />

## License

Copyright (C) 2026 Marius

CryptoScanBot is free software: you can redistribute it and/or modify it under the terms of the
GNU General Public License as published by the Free Software Foundation, either version 3 of the
License, or (at your option) any later version.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
General Public License for more details.

You should have received a copy of the GNU General Public License along with this program. It is in
the LICENSE file in the root of this repository. If not, see <https://www.gnu.org/licenses/>.

### Third party code

The indicators that name a published source in their comments (the Nadaraya-Watson Envelope, the
LuxAlgo RSI Multi Length areas and the StoRsi TradingView script) are re-implementations in C#,
written from the published formula and the described behaviour. They are not translations of the
original Pine scripts. The comments name the source so the origin of the idea stays visible.

## Code signing policy

> Status: code signing by the SignPath Foundation has been applied for. Releases up to and including
> 2.6.7 are not signed. Remove this line once the certificate is in use.

Free code signing provided by [SignPath.io](https://about.signpath.io), certificate by
[SignPath Foundation](https://signpath.org).

### Team roles

- Committers and reviewers: Marius ([CryptoMarius](https://github.com/CryptoMarius))
- Approvers (who approves a signing request): Marius ([CryptoMarius](https://github.com/CryptoMarius))

The project is maintained by one person. Every release is built by the GitHub Actions workflow in
`.github/workflows/release.yml` from a tagged commit of this repository, and every signing request
is approved by hand.

### Privacy policy

The application runs on your own machine and stores everything it produces there: its settings, its
candle databases and its log files. It has no account system, it sends nothing to the author, and it
does not check for updates, collect statistics or report errors anywhere.

It does connect to the outside world, because that is what it is for. Everything below is traffic
you switch on yourself by enabling an exchange or filling in a key:

- The exchanges you enable, for market data and - only when you supply API keys and switch trading
  on - for placing orders. Your API keys stay in your own data folder and are sent to that exchange
  and to nowhere else.
- `api.telegram.org`, when you configure a Telegram bot, to send you the signals you asked for.
- `app.altrady.com`, when you configure the Altrady webhook, to pass a signal to your Altrady
  account.
- `www.tradingview.com` and `api.alternative.me`, for the market indicators and the Fear and Greed
  index on the dashboard.

The SignalR hub is a server inside the application that another program on your own network can read
from. It listens, it does not send anything out by itself.

These external services have their own privacy policies, and what you send them is governed by those
rather than by this one: your exchange, [Telegram](https://telegram.org/privacy),
[Altrady](https://app.altrady.com/privacy-policy), [TradingView](https://www.tradingview.com/privacy-policy/)
and [alternative.me](https://alternative.me/privacy/).
