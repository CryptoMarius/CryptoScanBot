# Studie: instrument types per exchange

Peildatum 2026-08-26. Alle aantallen komen uit de publieke API van de exchange zelf, naast de symbolentabel van de bijbehorende scanner-instantie onder `E:\CryptoScanBot\Data`.

## Samenvatting

Per exchange vraagt de scanner precies een instrumentsoort op. Bij de meeste exchanges is dat terecht: wat we laten liggen zijn inverse contracten (afrekening in de basemunt) en contracten met een verloopdatum. Beide passen niet in het model van de scanner.

Er zijn twee echte gaten, en ze gaan over hetzelfde: perpetuals op aandelen, grondstoffen en indices. Bij Binance en HyperLiquid zetten die in een aparte categorie die wij niet opvragen. Bij alle andere exchanges staan ze gewoon tussen de crypto-perpetuals en krijgen we ze vanzelf mee.

| gat | markten | dagvolume | staat er nu |
|---|---|---|---|
| HyperLiquid, door bouwers ingezette perp-dexen | 147 | $2,19 miljard | 0 |
| Binance USD-M, TradFi-perpetuals | 175 | $13,44 miljard | 0 |
| Okx, xperp-contracten | 151 | $0,36 miljard | 0 (150 zijn dubbel) |

## Deel 1 - Wat de scanner nu heeft

Actieve symbolen per quote, barometer-symbolen niet meegeteld.

| instantie | totaal | verdeling |
|---|---|---|
| Binance Perpetual | 564 | USDT=521, USDC=38, U=2, USD1=2, BTC=1 |
| Binance Spot | 1358 | USDT=485, TRY=308, USDC=267, U=47, BTC=38, FDUSD=34 |
| BitMart Perpetual | 96 | USDT=95, USDC=1 |
| Bitvavo Spot | 439 | EUR=428, USDC=11 |
| BloFin Perpetual | 467 | USDT=457, USDC=10 |
| Bybit EU Spot | 133 | USDC=112, EUR=17, PLN=4 |
| Bybit Perpetual | 800 | USDT=732, USDC=68 |
| Bybit Spot | 549 | USDT=402, USDC=86, EUR=18, BTC=7, BRL=5, GBP=5 |
| Coinbase Spot | 920 | USDC=404, USD=401, EUR=34, BTC=24, GBP=23, USDT=21 |
| HyperLiquid Perpetual | 176 | USDC=176 |
| HyperLiquid Spot | 326 | USDC=309, USDH=11, USDT0=5, USDE=1 |
| Kraken Perpetual | 276 | USD=276 |
| Kraken Spot | 1382 | USD=637, EUR=519, USDC=44, USDT=44, BTC=31, GBP=26 |
| Kucoin Perpetual | 668 | USDT=663, USDC=5 |
| Kucoin Spot | 1002 | USDT=846, USDC=58, BTC=56, ETH=18, USD1=11, EUR=4 |
| Mexc Perpetual | 1137 | USDT=1024, USDC=78, USD1=35 |
| Mexc Spot | 2123 | USDT=1712, USDC=246, USD1=104, BTC=21, EUR=16, ETH=8 |
| Okx Perpetual | 438 | USDT=438 |
| Okx Spot | 1381 | USDT=395, USD=291, EUR=267, USDC=265, TRY=128, BRL=11 |

Okx Perpetual is de enige perpetual-instantie zonder USDC. Dat is geen fout van ons: Okx heeft geen enkele USDC-swap. Een rechtstreekse vraag naar `BTC-USDC-SWAP` geeft foutcode 51001, "Instrument ID doesn't exist".

## Deel 2 - Wat wij opvragen tegenover wat er is

| exchange | wij vragen | daarnaast beschikbaar | halen wij op |
|---|---|---|---|
| Binance Perpetual | `UsdFuturesApi`, alleen `ContractType.Perpetual` | 175 TradFi-perpetuals, 4 kwartaalcontracten, 30 coin-margined (COIN-M) | nee |
| Binance Spot | `SpotApi` | - | - |
| BitMart Perpetual | `UsdFuturesApi`, quote niet USD | 4 inverse | nee |
| Bitvavo | markets (alleen spot) | exchange heeft geen derivaten | - |
| BloFin | `GetSymbolsAsync` | 14 inverse | nee |
| Bybit Perpetual | `Category.Linear`, alleen `LinearPerpetual` | 40 gedateerde linear, 26 inverse, opties | nee |
| Bybit Spot | `GetSpotSymbolsAsync` | - | - |
| Coinbase | `SymbolType.Spot` | 120 gedateerde futures (`FUTURE`), allemaal `EXPIRING` | nee |
| HyperLiquid Perpetual | `FuturesApi`, hoofdmarkt | 10 door bouwers ingezette perp-dexen, samen 147 actieve markten | nee |
| HyperLiquid Spot | `SpotApi` | - | - |
| Kraken Perpetual | `GetSymbolsAsync`, levert `PF_` | 4 `PI_` inverse perp, 10 `FI_`, 8 `FF_` gedateerd | nee |
| Kucoin Perpetual | `GetSymbolsAsync` | 4 USD-inverse `FFWCSX`, 1 `FFICSX` | nee |
| Mexc Perpetual | `GetSymbolsAsync` | 10 USD-inverse | nee |
| Okx Perpetual | `InstrumentType.Swap` | `InstrumentType.Futures`: 151 xperp, 16 USD_UM gedateerd, 16 inverse gedateerd | nee |
| Okx Spot | `InstrumentType.Spot` | `Margin` is dezelfde instrumentenlijst | - |

## Deel 3 - De gaten, op volgorde van omvang

### 1. HyperLiquid: de door bouwers ingezette perp-dexen

HyperLiquid kent naast de hoofdmarkt tien aparte perp-markten die door externe partijen zijn ingezet. De aanroep `{"type":"perpDexs"}` geeft ze:

| dex | naam | actieve markten | dagvolume | grootste markt |
|---|---|---|---|---|
| (hoofdmarkt) | wat wij ophalen | 176 | $7.354,9 M | BTC $3.714,2 M |
| xyz | XYZ | 101 | $2.101,4 M | xyz:SKHX $295,5 M |
| io | EntropyIO | 2 | $68,2 M | io:SNDK $51,5 M |
| mkts | Markets By Kinetiq | 4 | $13,9 M | mkts:US500 $8,3 M |
| para | Paragon | 22 | $3,3 M | para:UNITREE $1,7 M |
| hyna | HyENA | 18 | $1,9 M | hyna:BTC $0,4 M |
| vntl | Ventuals | 0 | $0,0 M | - |
| km | Markets by Kinetiq | 0 | $0,0 M | - |
| flx | Felix Exchange | 0 | $0,0 M | - |
| cash | dreamcash | 0 | $0,0 M | - |

De xyz-dex alleen al is 29% van de hoofdmarkt. Top tien daarvan:

| markt | prijs | dagvolume |
|---|---|---|
| xyz:SKHX | 1222,7 | $295,5 M |
| xyz:CL | 82,415 | $191,0 M |
| xyz:SNDK | 1500,2 | $171,1 M |
| xyz:XYZ100 | 29210,0 | $155,5 M |
| xyz:CRCL | 89,49 | $107,5 M |
| xyz:BRENTOIL | 87,124 | $100,7 M |
| xyz:SP500 | 7671,0 | $96,5 M |
| xyz:DRAM | 56,39 | $95,6 M |
| xyz:SPCX | 137,91 | $95,3 M |
| xyz:MU | 940,05 | $80,8 M |

48 van de 101 halen een volume van 2.500.000. De marktnaam draagt het voorvoegsel van de dex (`xyz:GOLD`), dus botsing met de hoofdmarkt is uitgesloten.

### 2. Binance USD-M: de TradFi-perpetuals

Binance zet 175 perpetuals op aandelen, grondstoffen en indices in een eigen contractsoort, `TRADIFI_PERPETUAL`. Onze filter in `CryptoScanner.Exchanges/Binance/Perpetual/Symbol.cs` regel 91 laat alleen `ContractType.Perpetual` door, dus alle 175 vallen af.

- Samen $13,44 miljard dagvolume, tegen $41,99 miljard voor de 567 gewone perpetuals
- 82 van de 175 halen een volume van 2.500.000
- Top vijf: SNDKUSDT $2.229,3 M, XAUUSDT $2.063,9 M, SKHYNIXUSDT $1.086,1 M, SPCXUSDT $872,8 M, XAGUSDT $809,3 M

De bibliotheek Binance.Net 13.3.0 kent `TRADIFI_PERPETUAL` als aparte waarde, dus het is met een filter op te lossen, niet met een tekstvergelijking op de naam.

### 3. Okx: de xperp-contracten

151 contracten onder `InstrumentType.Futures` met `ruleType: xperp`. Ze hebben een funding-tarief en gedragen zich als perpetuals; de verloopdatum staat op 2031.

Waarde is minimaal: 150 van de 151 bestaan al als USDT-swap in dezelfde scanner, met 25 tot 325 keer meer volume. De 151e is `TEST002-USD_UM_XPERP-310822`, een testinstrument zonder prijs. Samen $361 miljoen tegen $21,14 miljard voor de 438 USDT-swaps.

## Deel 4 - Dezelfde markten, verschillend behandeld

Of een TradFi-markt in de scanner staat hangt volledig af van hoe de exchange hem indeelt:

| exchange | XAU | SNDK | SPCX | CL | AAPL | NVDA | TSLA |
|---|---|---|---|---|---|---|---|
| Binance | - | - | - | - | - | - | - |
| BitMart | ja | - | - | - | - | - | - |
| BloFin | ja | ja | ja | ja | ja | ja | ja |
| Bybit | ja | ja | ja | ja | ja | ja | ja |
| HyperLiquid | - | - | - | - | - | - | - |
| Kraken | ja | - | - | - | - | - | - |
| Kucoin | - | ja | ja | ja | ja | ja | ja |
| Mexc | ja | - | - | - | - | - | - |
| Okx | ja | ja | ja | ja | ja | ja | ja |

Bybit, Okx, BloFin en Kucoin geven ze als gewone perpetual, dus die krijgen we zonder enige aanpassing. Binance en HyperLiquid geven ze in een aparte categorie, en daar staan we op nul.

## Deel 5 - Wat bewust dicht blijft

| soort | waar | reden |
|---|---|---|
| inverse / coin-margined | Binance COIN-M 30, Bybit 26, Okx 15+16, Kraken 14, BloFin 14, Mexc 10, Kucoin 5, BitMart 4 | afrekening in de basemunt; prijs en volume zijn niet vergelijkbaar en de winstberekening klopt niet |
| gedateerde futures | Coinbase 120, Bybit 40, Okx 32, Kraken 18, Binance 4 | ze verlopen. De candle-historie hangt aan `SymbolInterval.ExchangeName`, en die naam verandert bij elke roll |
| opties | Bybit, Okx | geen candle-model in de scanner |

## Deel 6 - Advies

1. HyperLiquid xyz-dex erbij halen. Grootste gat, 101 markten en $2,1 miljard per dag, met een naamgeving die niet botst. De aanroep is dezelfde als nu met een extra `dex`-parameter.
2. Binance TradFi-perpetuals erbij halen. 175 markten en $13,4 miljard per dag. Een waarde extra toelaten in de filter op regel 91. Wel eerst uitzoeken hoe de bestaande controle op `UnderlyingSubType` zich hiertoe verhoudt.
3. Okx xperp laten liggen. 150 van de 151 zijn dubbel en het volume is 1,7% van de swaps. Alleen bouwen als u met USDC als onderpand wilt handelen.
4. Handelsuren onder ogen zien voordat 1 en 2 draaien. Aandelen en grondstoffen handelen niet doorlopend. Perpetuals erop wel, maar met veel dunnere boeken buiten de uren van de onderliggende markt. Dat geeft gaten en sprongen in de candles, hetzelfde probleem als bij Alpaca.

## Verantwoording

- Binance USD-M `https://fapi.binance.com/fapi/v1/exchangeInfo` en `/fapi/v1/ticker/24hr`
- Binance COIN-M `https://dapi.binance.com/dapi/v1/exchangeInfo`
- Bybit `https://api.bybit.com/v5/market/instruments-info?category=linear|inverse|spot`
- Okx `https://www.okx.com/api/v5/public/instruments?instType=SWAP|FUTURES|SPOT`, tickers en funding-rate per instrument
- HyperLiquid `https://api.hyperliquid.xyz/info` met `perpDexs`, `meta`, `spotMeta` en `metaAndAssetCtxs` per dex
- Kucoin `https://api-futures.kucoin.com/api/v1/contracts/active`
- Mexc `https://contract.mexc.com/api/v1/contract/detail`
- Kraken `https://futures.kraken.com/derivatives/api/v3/instruments`
- BloFin `https://openapi.blofin.com/api/v1/market/instruments`
- BitMart `https://api-cloud-v2.bitmart.com/contract/public/details`
- Coinbase `https://api.coinbase.com/api/v3/brokerage/market/products?product_type=SPOT|FUTURE`

BitMart telt 359 contracten op Trading tegen 96 in onze database. Dat is geen gat: BitMart laat de status op Trading staan lang nadat de handel is gestopt, en de code vangt dat op met een controle op volume en funding. Zie de toelichting in `BitMart/Perpetual/Symbol.cs` regel 66.
