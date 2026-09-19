import datetime, glob, os, json
import numpy as np
import _harvest_kern as K, _harvest_run as R
SC=r"C:\Users\Marius\AppData\Local\Temp\claude\E--Projects-CryptoScanBot\c7733a19-557d-4660-9a65-946849a15a51\scratchpad\fijn"
RAND_VAN=datetime.date(2026,9,5); RAND_TOT=datetime.date(2026,10,5)
MUNT={"btc":"BTCUSDT.PERP","bnb":"BNBUSDT.PERP","uni":"UNIUSDT.PERP","dash":"DASHUSDT.PERP",
      "jup":"JUPUSDT.PERP","zec":"ZECUSDT.PERP","link":"LINKUSDT.PERP"}

def beoordeel(frame, sym, reeks, oc):
    top,bot,area=K.column_profile(frame)
    if np.isnan(top).mean()>0.5: return None
    datums=reeks[0]; o,c=oc
    gk,rk=K.colour_columns(area); echt=c>=o; kol=np.arange(len(top))
    beste=None
    for pitch in np.arange(3.5,12.01,0.05):
        dag=np.floor((kol-905)/pitch).astype(int)
        for a in range(len(datums)):
            laatste=a+dag.max()
            if not (0<=laatste<len(datums)) or not (RAND_VAN<=datums[laatste]<=RAND_TOT): continue
            goed=tot=0
            for d in range(dag.min(),dag.max()+1):
                h=dag==d; i=a+d
                if not h.any() or not 0<=i<len(datums): continue
                g,r=gk[h].sum(),rk[h].sum()
                if g+r<20: continue
                tot+=1
                if (g>r)==bool(echt[i]): goed+=1
            if tot>=60 and (beste is None or goed/tot>beste[0]): beste=(goed/tot,float(pitch),a,tot)
    if beste is None: return None
    s,pitch,a,tot=beste
    return dict(score=s,pitch=pitch,anker=datums[a],dagen=tot,area=area,top=top,datums=datums)

uit=[]
for kort,sym in MUNT.items():
    reeks=K.daily_series(sym); oc=R.opens_closes(sym)
    frames=sorted(glob.glob(os.path.join(SC,kort+"_*.jpg")))
    beste=None
    for f in frames:
        try: r=beoordeel(f,sym,reeks,oc)
        except Exception: continue
        if r and (beste is None or r["score"]>beste["score"]):
            beste=r; beste["frame"]=os.path.basename(f)
    if beste is None:
        print(f"  {sym:14} geen bruikbaar frame"); continue
    ok = beste["score"]>=R.ACCEPT
    stippen=R.dots(beste["area"],beste["pitch"],905,beste["anker"]) if ok else []
    eerste=beste["anker"]+datetime.timedelta(days=int(np.floor((0-905)/beste["pitch"])))
    laatste=beste["anker"]+datetime.timedelta(days=int(np.floor((len(beste["top"])-905)/beste["pitch"])))
    print(f"  {sym:14} beste van {len(frames)} frames: {beste['frame']} kleur {beste['score']*100:5.1f}% "
          f"{'GOED' if ok else 'verworpen'}  beeld {eerste} t/m {laatste}")
    if ok: print(f"     stippen: {', '.join(str(d) for d in stippen) or 'geen'}")
    uit.append(dict(symbol=sym,frame=beste["frame"],score=round(beste["score"],4),
                    pitch=round(beste["pitch"],2),first=str(eerste),last=str(laatste),
                    accepted=bool(ok),dots=[str(d) for d in stippen]))
json.dump(uit,open("mac-oogst-fijn.json","w"),indent=1)
print(f"\nklaar: {sum(1 for x in uit if x['accepted'])} van {len(uit)} munten geaccepteerd")
