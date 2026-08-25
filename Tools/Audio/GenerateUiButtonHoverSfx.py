"""Generate Bomb Swap's muted, weighty UI button hover sound.

"Goose Nudge" is a low padded push against a small wooden body. It avoids
pitch gestures, bright brush noise, and obvious chip oscillators so repeated
focus changes feel physical rather than arcade-like while remaining softer
than the button click.
"""

from __future__ import annotations

import math
import pathlib
import wave

import numpy as np


SAMPLE_RATE = 44_100
DURATION_SECONDS = 0.140
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
    / "SFX_UI_ButtonHover_GooseNudge_8Bit.wav"
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
    rng = np.random.default_rng(0x6005E10F)

    # A short low pressure wave supplies weight without borrowing the click's
    # mechanical latch. The quick downward settle reads as a padded push rather
    # than a melodic pitch gesture.
    pressure_frequency = 70.0 + 55.0 * np.exp(-22.0 * time)
    pressure_phase = np.cumsum(pressure_frequency) / SAMPLE_RATE
    pressure_envelope = (1.0 - np.exp(-650.0 * time)) * np.exp(-23.0 * time)
    mix += np.sin(2.0 * math.pi * pressure_phase) * pressure_envelope * 0.43

    # Keep only a muted wooden contact texture. Its low cutoff removes the airy
    # brush quality that made the previous hover feel too light.
    noise = rng.normal(0.0, 1.0, size=FRAME_COUNT)
    texture = one_pole_lowpass(noise, 480.0)
    texture /= max(float(np.max(np.abs(texture))), 1.0e-9)
    texture_envelope = (1.0 - np.exp(-700.0 * time)) * np.exp(-42.0 * time)
    mix += texture * texture_envelope * 0.11

    # One heavily damped resonance suggests the same wooden UI material as the
    # click, while staying too soft and simple to compete with confirmation.
    body_envelope = (1.0 - np.exp(-520.0 * time)) * np.exp(-31.0 * time)
    mix += np.sin(2.0 * math.pi * 148.0 * time + 0.21) * body_envelope * 0.075

    mix = one_pole_lowpass(mix, 1_050.0)

    mix -= np.mean(mix)
    peak = float(np.max(np.abs(mix)))
    mix *= 0.44 / max(peak, 1.0e-9)

    attack_frames = round(0.005 * SAMPLE_RATE)
    release_frames = round(0.040 * SAMPLE_RATE)
    attack_ramp = np.sin(
        np.linspace(0.0, math.pi / 2.0, attack_frames)
    ) ** 2
    release_ramp = np.sin(
        np.linspace(0.0, math.pi / 2.0, release_frames)
    ) ** 2
    mix[:attack_frames] *= attack_ramp
    mix[-release_frames:] *= release_ramp[::-1]

    # A very fine grain keeps the first-party synthetic source reproducible
    # without turning the cue into an obvious 8-bit oscillator.
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
