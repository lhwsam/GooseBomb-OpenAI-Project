"""Generate Bomb Swap's muted, tactile UI button click sound.

"Goose Clack" combines a low pressure thud, filtered wood texture, and a tiny
mechanical latch. It avoids pitch cadences and obvious chip pulses so the click
feels physical, dark, and compatible with the adaptive BGM.
"""

from __future__ import annotations

import math
import pathlib
import wave

import numpy as np


SAMPLE_RATE = 44_100
DURATION_SECONDS = 0.170
FRAME_COUNT = round(SAMPLE_RATE * DURATION_SECONDS)

PROJECT_ROOT = pathlib.Path(__file__).resolve().parents[2]
OUTPUT_PATH = (
    PROJECT_ROOT
    / "Assets"
    / "Game"
    / "Content"
    / "Audio"
    / "SFX"
    / "UI"
    / "SFX_UI_ButtonClick_GooseClack_8Bit.wav"
)


def one_pole_lowpass(signal: np.ndarray, cutoff_hz: float) -> np.ndarray:
    alpha = 1.0 - math.exp(-2.0 * math.pi * cutoff_hz / SAMPLE_RATE)
    filtered = np.empty_like(signal, dtype=np.float64)
    state = 0.0
    for index, sample in enumerate(signal):
        state += alpha * (float(sample) - state)
        filtered[index] = state
    return filtered


def synthesize() -> np.ndarray:
    time = np.arange(FRAME_COUNT, dtype=np.float64) / SAMPLE_RATE
    mix = np.zeros(FRAME_COUNT, dtype=np.float64)
    rng = np.random.default_rng(0x6005EC1A)

    # A sine-based pressure thud replaces the pulse oscillator. Its frequency
    # settles slightly but never forms a recognisable melodic gesture.
    thud_frequency = 72.0 + 62.0 * np.exp(-24.0 * time)
    thud_phase = np.cumsum(thud_frequency, dtype=np.float64) / SAMPLE_RATE
    thud = np.sin(2.0 * math.pi * thud_phase)
    thud_envelope = (1.0 - np.exp(-1_100.0 * time)) * np.exp(-23.0 * time)
    mix += thud * thud_envelope * 0.50

    # Low-passed deterministic noise supplies a padded wooden surface.
    noise = rng.normal(0.0, 1.0, size=FRAME_COUNT)
    wooden_noise = one_pole_lowpass(noise, 720.0)
    wooden_noise /= max(float(np.max(np.abs(wooden_noise))), 1.0e-9)
    noise_envelope = (1.0 - np.exp(-1_250.0 * time)) * np.exp(-43.0 * time)
    mix += wooden_noise * noise_envelope * 0.18

    # A non-melodic wooden resonance and a second filtered latch impulse give
    # the click definition without a bright arcade snap.
    wood_envelope = (1.0 - np.exp(-850.0 * time)) * np.exp(-36.0 * time)
    mix += np.sin(2.0 * math.pi * 172.0 * time + 0.22) * wood_envelope * 0.105

    latch_start = round(0.026 * SAMPLE_RATE)
    latch_count = round(0.042 * SAMPLE_RATE)
    latch_time = np.arange(latch_count, dtype=np.float64) / SAMPLE_RATE
    latch_noise = one_pole_lowpass(
        rng.normal(0.0, 1.0, size=latch_count),
        520.0,
    )
    latch_noise /= max(float(np.max(np.abs(latch_noise))), 1.0e-9)
    latch_envelope = (1.0 - np.exp(-900.0 * latch_time)) * np.exp(-72.0 * latch_time)
    mix[latch_start:latch_start + latch_count] += (
        latch_noise * latch_envelope * 0.085
    )

    mix = one_pole_lowpass(mix, 1_500.0)

    mix -= np.mean(mix)
    peak = float(np.max(np.abs(mix)))
    mix *= 0.58 / max(peak, 1.0e-9)

    # Keep the transient immediate while landing both file edges on zero.
    attack_frames = round(0.0015 * SAMPLE_RATE)
    release_frames = round(0.038 * SAMPLE_RATE)
    attack_ramp = np.sin(
        np.linspace(0.0, math.pi / 2.0, attack_frames)
    ) ** 2
    release_ramp = np.sin(
        np.linspace(0.0, math.pi / 2.0, release_frames)
    ) ** 2
    mix[:attack_frames] *= attack_ramp
    mix[-release_frames:] *= release_ramp[::-1]

    # Keep only a fine synthetic grain; coarse 8-bit steps made the previous
    # version read as an arcade UI cue.
    mix = np.round(mix * 2047.0) / 2047.0
    mix[0] = 0.0
    mix[-1] = 0.0
    return mix.astype(np.float32, copy=False)


def write_wav(path: pathlib.Path, mix: np.ndarray) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    pcm = np.rint(np.clip(mix, -1.0, 1.0) * 32767.0).astype("<i2")
    with wave.open(str(path), "wb") as wav:
        wav.setnchannels(1)
        wav.setsampwidth(2)
        wav.setframerate(SAMPLE_RATE)
        wav.writeframes(pcm.tobytes())


def main() -> None:
    mix = synthesize()
    write_wav(OUTPUT_PATH, mix)

    rms = float(np.sqrt(np.mean(mix * mix)))
    peak = float(np.max(np.abs(mix)))
    print(f"Wrote: {OUTPUT_PATH}")
    print(
        f"Duration: {FRAME_COUNT / SAMPLE_RATE:.3f}s "
        f"({FRAME_COUNT} mono frames at {SAMPLE_RATE} Hz)"
    )
    print(f"Peak: {peak:.5f}, RMS: {rms:.5f}")
    print(f"Edge samples: first={mix[0]:.6f}, last={mix[-1]:.6f}")


if __name__ == "__main__":
    main()
