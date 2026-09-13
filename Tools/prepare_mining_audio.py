"""Bake short, onset-aligned mono clips from generated foley; never runs in game."""
from array import array
from pathlib import Path
import math
import wave

root = Path(__file__).resolve().parents[1] / 'Assets/Prototype/Art/Audio'
for name in ('MiningPick_Foley_01', 'MiningPick_Foley_02', 'MiningBreak_Foley_01'):
    with wave.open(str(root / (name + '.wav')), 'rb') as source:
        rate, channels = source.getframerate(), source.getnchannels()
        assert source.getsampwidth() == 2
        raw = array('h', source.readframes(source.getnframes()))
    samples = [sum(raw[i:i + channels]) / channels / 32768 for i in range(0, len(raw), channels)]
    window = rate // 1000
    levels = [math.sqrt(sum(v*v for v in samples[i:i+window]) / window) for i in range(0, len(samples), window)]
    onset = next(i for i, v in enumerate(levels) if v >= max(levels) * 0.15)
    start = max(0, (onset - 1) * window)
    duration = 0.36 if 'Break' in name else 0.22
    samples = samples[start:start + int(rate * duration)]
    # Remove DC and align loudness by energy, with peak headroom for rapid playback.
    dc = sum(samples) / len(samples)
    samples = [v-dc for v in samples]
    rms = math.sqrt(sum(v*v for v in samples) / len(samples))
    gain = min(0.14 / max(rms, 0.0001), 0.78 / max(abs(v) for v in samples))
    result = array('h')
    for i, value in enumerate(samples):
        fade = min(1, i / (rate * 0.0005), (len(samples)-1-i) / (rate * 0.035))
        result.append(round(value * gain * fade * 32767))
    output = root / (name + '_Ready.wav')
    with wave.open(str(output), 'wb') as dest:
        dest.setparams((1, 2, rate, len(result), 'NONE', 'not compressed'))
        dest.writeframes(result.tobytes())
    print(f'{output.name}: removed {start/rate:.3f}s lead; {len(result)/rate:.2f}s; peak {max(abs(v) for v in result)/32768:.3f}')
