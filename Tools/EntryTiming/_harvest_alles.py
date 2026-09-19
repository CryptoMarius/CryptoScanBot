import json
import _harvest_run as R
SC=r"C:\Users\Marius\AppData\Local\Temp\claude\E--Projects-CryptoScanBot\c7733a19-557d-4660-9a65-946849a15a51\scratchpad\frames2"
paren=[("g0004.jpg","BTCUSDT.PERP"),("g0139.jpg","NEARUSDT.PERP"),("g0142.jpg","BNBUSDT.PERP"),
       ("g0146.jpg","BNBUSDT.PERP"),("g0153.jpg","UNIUSDT.PERP"),("g0162.jpg","DASHUSDT.PERP"),
       ("g0179.jpg","JUPUSDT.PERP"),("g0185.jpg","ZECUSDT.PERP"),("g0241.jpg","LINKUSDT.PERP")]
uit=[]
for frame,sym in paren:
    try:
        r=R.read(SC+"\\"+frame,sym)
        if r: r["frame"]=frame; uit.append(r)
    except Exception as e:
        print(f"  {sym}: MISLUKT {type(e).__name__}: {e}")
json.dump(uit,open("mac-oogst.json","w"),indent=1)
print(f"\n{len(uit)} vensters geoogst")
