# Claude Code – Projectafspraken

## Taal in de chat
- **Praat altijd Nederlands** met de gebruiker in de chat.
- Schakel nooit over naar Engels in de chat, ongeacht de taal waarin de gebruiker schrijft.

## Taal in de broncode
- **Gebruik altijd Engels** voor alle comments, XML-doc-comments, namen van variabelen en alle andere tekst in bronbestanden.
- Schrijf nooit Nederlandse (of andere) tekst in de broncode.

## Bestaande bestanden bewerken
- Laat bij het wijzigen van een bestaand bestand **bestaande comments en uitgecommentarieerde code altijd staan**.
- Verwijder, herschrijf of ruim geen comments of uitgeschakelde (`//`) codeblokken op die er al stonden.

## Codestijl
- Het statement na `if` en na `else` staat altijd op een nieuwe regel.
- Percentages altijd op de schaal 0..100, nooit 0..1.
- Platformafhankelijke zaken (mappen, processen, besturingssysteem) alleen via `IPlatformService`, nooit rechtstreeks via `Environment.SpecialFolder` of iets vergelijkbaars.

## Werkwijze
- Wijzigingen altijd in de echte projectmap (`E:\Projects\CryptoScanBot`), niet in een worktree.
- Git alleen om te lezen (log, show, diff, status). Committen doet de gebruiker: commits nooit noemen, aanbieden of voorstellen, ook geen commitbericht, tenzij de gebruiker er zelf om vraagt.
- `Roadmap.txt` en `Release Changes.txt` niet aanraken.
- Nooit applicaties starten of afsluiten (scanners, emulator, UI's); dat doet de gebruiker zelf.
- Geen build van de hele solution zolang de emulator draait: die schrijft naar de Bin-map waaruit de emulator draait. Bouw losse projecten.
- Elke wijziging aan de UI in beide scanners doorvoeren: Avalonia en Photino/Blazor.

## Trader en strategieën
- Bij rekenregels van de trader (prijzen, take profit, stop, bijkopen) eerst nagaan hoe Altrady het doet en dat volgen.
- Geen algemene instelling invoeren die maar voor één strategie bedoeld is; zo'n keuze hoort bij die strategie.
- Geen tweede, volledige berekening (een run die de handelslogica nog eens apart narekent) in de emulator. Een kleine sanity check tijdens de run mag wel, bijvoorbeeld controleren dat een positie niet meer verkoopt dan hij heeft.
- Na een wijziging aan de trader de flow laten doorlichten: een echte positie van instap tot sluiting door de code volgen, en eerst één proefrun nalopen tegen de candles.

## Emulator-wachtrij
- Een wachtrijbestand dat eindigt op `.debug.json` draait alleen in de Debug-emulator. De Release-emulator slaat het over en laat het staan (`EmulatorQueueFolder.DebugOnlySuffix`). De Debug-emulator draait zowel `.json` als `.debug.json`.
- Zet nieuwe wachtrijbestanden daarom direct als `.debug.json` klaar wanneer ze in de Debug-emulator moeten draaien (zoals strategieën die alleen in Debug bestaan). Dan hoeft er later niets hernoemd te worden.
- Een bestand dat (nog) niet mag draaien, krijgt een extra extensie achter `.json`, bijvoorbeeld `.json.na-nieuwe-build`. Beide emulators slaan het dan over.

## Marktdata
- Periodes zonder candles of trades zijn normaal en worden geaccepteerd. Een minuut zonder trade wordt vlak ingevuld, `LastCandleSynchronized` blijft bij de eerste ingevulde minuut staan, en het uurlijkse verversen haalt die minuten alsnog bij de exchange op. De herstart van de streams na 24 uur is de buitengrens.
- Nooit een inactiviteits- of stiltecontrole per symbool of per abonnement bouwen (`SilentSymbolCatchUp` is daarom op 11-10-2026 verwijderd).
- Voordat een munt als "dood" of "stil" wordt gezien: eerst bij de exchange nagaan of hij gedelist of afgewikkeld is (status in exchangeInfo / contracts). Geen code bouwen op een diagnose die niet is nagegaan.

## Rechten
- Claude heeft **volledige lees- en schrijftoegang** tot alle bestanden in deze repository.
- Claude mag elk bestand lezen, aanmaken, bewerken en verwijderen zonder extra toestemming te vragen.
- Claude mag code over meerdere bestanden heen refactoren, herstructureren of herindelen als dat nodig is.
