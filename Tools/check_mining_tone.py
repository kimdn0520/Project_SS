from pathlib import Path
from array import array
import wave
for p in Path('Assets/Prototype/Art/Audio').glob('*_Ready.wav'):
    with wave.open(str(p)) as w:
        x = list(array('h', w.readframes(w.getnframes())))[::8]
    e = sum(v*v for v in x)
    corr = max(sum(a*b for a,b in zip(x,x[k:]))/e for k in range(3,70))
    print(p.name, round(corr,3))
