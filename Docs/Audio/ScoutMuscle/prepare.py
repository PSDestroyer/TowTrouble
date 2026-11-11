"""Offline preparation; reads imported originals, writes only derived Scout WAVs.
Run from project root with numpy and soundfile available. No Unity/build calls.
"""
from pathlib import Path
import hashlib,json
import numpy as np
import soundfile as sf
root=Path('Assets/TowTrouble/Audio/Engine')
out=Path('Assets/TowTrouble/Art/Audio/Library/Vehicles/Scout Muscle');out.mkdir(parents=True,exist_ok=True)
report=[]
for mode in ['Idle','Low','High']:
 p=root/f'Engine_Muscle_{mode}.wav';x,sr=sf.read(str(p));x=x-x.mean()
 f=np.fft.rfftfreq(len(x),1/sr)
 # Smooth +10 dB presence shelf, with gentle sub-bass reduction. No distortion.
 shelf=(.65+2.5*(f*f/(f*f+180**2)))/(1+(f/6500)**4)**.25
 y=np.fft.irfft(np.fft.rfft(x)*shelf,n=len(x))
 # Circular overlap gives a continuous seam without fading every revolution to silence.
 n=int(sr*.018);t=np.linspace(0,1,n,endpoint=False);t=t*t*(3-2*t)
 seam=y[-n:]*(1-t)+y[:n]*t;y=np.concatenate([seam,y[n:-n]])
 y*=min(.22/np.sqrt(np.mean(y*y)),.68/max(abs(y)))
 target=out/f'Scout Muscle {mode}.wav';sf.write(str(target),y,sr,subtype='PCM_24')
 s=abs(np.fft.rfft(y*np.hanning(len(y))))**2;f=np.fft.rfftfreq(len(y),1/sr)
 report.append(dict(source=str(p),sha256=hashlib.sha256(p.read_bytes()).hexdigest(),derived=str(target),seconds=len(y)/sr,peak=float(max(abs(y))),rms=float(np.sqrt(np.mean(y*y))),above200HzPercent=float(100*s[f>200].sum()/s.sum())))
Path('Docs/Audio/ScoutMuscle/signal-audit.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2))
