"""Designed ignition variants, not separate recordings. Sources never modified."""
from pathlib import Path
import numpy as np,soundfile as sf,hashlib,json
out=Path('Assets/TowTrouble/Art/Audio/Library/Vehicles/Ignition');out.mkdir(parents=True,exist_ok=True)
source=Path('Assets/TowTrouble/Audio/Engine/Engine_Start.WAV');starter,sr=sf.read(source);starter=starter.mean(axis=1);starter-=starter.mean()
report=[]
for name,crank in [('Scout',.68),('Hauler',.85),('Buddy',.55),('Ranger',.63)]:
 p=Path('Assets/TowTrouble/Art/Audio/Library/Vehicles')/('Scout Muscle/Scout Muscle Idle.wav' if name=='Scout' else f'Fleet Engines/{name} Idle.wav')
 idle,rate=sf.read(p);assert rate==sr
 def engine(seconds,pitches):
  n=int(seconds*sr);pitch=np.linspace(*pitches,n);pos=np.cumsum(pitch)%len(idle)
  return np.interp(pos,np.arange(len(idle)),idle,period=len(idle))
 duration=crank+1.0;t=np.arange(int(duration*sr))/sr
 start=np.zeros(len(t));n=int(crank*sr)
 # Use only the starter's first .6s, before the source engine's loud catch.
 a=np.interp(np.linspace(0,.6*sr,n),np.arange(len(starter)),starter)
 a*=min(.09/max(np.sqrt(np.mean(a*a)),1e-9),3)
 start[:n]=a*np.minimum(np.arange(n)/(.02*sr),1)*np.minimum((n-np.arange(n))/(.09*sr),1)
 catch=engine(duration,(.82,1));env=np.clip((t-(crank-.12))/.2,0,1)*np.clip((duration-t)/.55,0,1)
 start+=catch*env*.65
 seconds=.95 if name=='Hauler' else .8;t2=np.arange(int(seconds*sr))/sr
 stop=engine(seconds,(1,.38))*np.exp(-3*t2/seconds)*np.clip((seconds-t2)/.22,0,1)*np.clip(t2/.015,0,1)
 for kind,x in [('Start',start),('Stop',stop)]:
  x*=min(1,.55/max(abs(x)));dest=out/f'{name} {kind}.wav';sf.write(dest,x,sr,subtype='PCM_24')
  report.append(dict(path=str(dest),source=str(source),sourceSha256=hashlib.sha256(source.read_bytes()).hexdigest(),idle=str(p),idleSha256=hashlib.sha256(p.read_bytes()).hexdigest(),seconds=len(x)/sr,peak=float(max(abs(x))),rms=float(np.sqrt(np.mean(x*x)))))
Path('Docs/Audio/Ignition/signal-audit.json').write_text(json.dumps(report,indent=2)+'\n')
