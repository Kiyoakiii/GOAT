"""Generate original, deterministic cartoon bleats and a static discharge."""
from pathlib import Path
import math
import random
import struct
import wave

DESTINATION = Path(__file__).resolve().parents[2] / "Assets/GoatDescent/Resources/GoatHerdAudio"
RATE = 22050


def save(name, samples):
    peak = max(abs(value) for value in samples) or 1
    payload = b"".join(struct.pack("<h", round(value / peak * 27000)) for value in samples)
    with wave.open(str(DESTINATION / (name + ".wav")), "wb") as output:
        output.setparams((1, 2, RATE, 0, "NONE", "not compressed"))
        output.writeframes(payload)


def bleat(seconds, quiet=False):
    samples = []
    phase = 0
    noise = random.Random(41 if quiet else 73)
    for index in range(round(RATE * seconds)):
        t = index / RATE
        progress = t / seconds
        frequency = (195 if quiet else 164) + 23 * math.sin(progress * math.pi)
        frequency *= 1 + .048 * math.sin(t * math.tau * 6.3) + .012 * math.sin(t * 173)
        phase += math.tau * frequency / RATE
        signal = 0
        for harmonic in range(1, 23):
            f = harmonic * frequency
            formants = sum(level * math.exp(-((f - center) / width) ** 2)
                           for center, width, level in ((640, 210, 1), (1790, 340, .65), (2680, 470, .22)))
            signal += math.sin(phase * harmonic) * (.13 + formants) / harmonic
        envelope = min(1, t / .055) * min(1, (seconds - t) / .17)
        pulse = .77 + .23 * math.sin(t * math.tau * 6.1)
        signal = math.tanh(signal * 2.7) * pulse
        samples.append((signal + noise.uniform(-.018, .018)) * envelope)
    return samples


def breath():
    samples, low, high = [], 0., 0.
    noise = random.Random(91)
    for index in range(round(RATE * 1.2)):
        t = index / RATE
        value = noise.uniform(-1, 1)
        low += .035 * (value - low)
        high += .55 * (value - high)
        envelope = math.sin(math.pi * t / 1.2) ** 1.4
        samples.append((high - low) * envelope)
    return samples


def discharge():
    samples = []
    noise = random.Random(127)
    for index in range(round(RATE * .36)):
        t = index / RATE
        samples.append((noise.uniform(-.7, .7) + .25 * math.sin(t * math.tau * (1750 - t * 2300)))
                       * math.exp(-t * 15) * min(1, t / .002))
    return samples


if __name__ == "__main__":
    DESTINATION.mkdir(parents=True, exist_ok=True)
    save("QuietBleat", bleat(.65, quiet=True))
    save("Inhale", breath())
    save("LoudBleat", bleat(1.8))
    save("Discharge", discharge())
    print("Generated 4 original audio clips in", DESTINATION)
