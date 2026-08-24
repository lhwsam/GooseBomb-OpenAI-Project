"""Generate Bomb Swap's short 8-bit UI button click sound.

"Goose Clack" combines a dry beak-like snap, a tiny mechanical body, and a
quiet C#-to-D confirmation pulse. It is mono and intentionally short so rapid
menu navigation does not build a distracting tail.
"""

from __future__ import annotations

import math
import pathlib
import wave

import numpy as np


SAMPLE_RATE = 44_100
DURATION_SECONDS = 0.145
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


def pulse_from_frequency(
    frequency: np.ndarray,
    *,
    duty: float,
) -> np.ndarray:
    phase = np.cumsum(frequency, dtype=np.float64) / SAMPLE_RATE
    return np.where(np.mod(phase, 1.0) < duty, 1.0, -1.0)


def add_tone(
    mix: np.ndarray,
    *,
    start_seconds: float,
    duration_seconds: float,
    frequency_hz: float,
    amplitude: float,
) -> None:
    start = round(start_seconds * SAMPLE_RATE)
    count = round(duration_seconds * SAMPLE_RATE)
    end = min(FRAME_COUNT, start + count)
    if end <= start:
        return

    time = np.arange(end - start, dtype=np.float64) / SAMPLE_RATE
    phase = frequency_hz * time
    signal = np.where(np.mod(phase, 1.0) < 0.18, 1.0, -1.0)
    envelope = np.sin(np.linspace(0.0, math.pi, end - start)) ** 2
    signal = np.round(signal * 7.0) / 7.0
    mix[start:end] += signal * envelope * amplitude


def synthesize() -> np.ndarray:
    time = np.arange(FRAME_COUNT, dtype=np.float64) / SAMPLE_RATE
    mix = np.zeros(FRAME_COUNT, dtype=np.float64)
    rng = np.random.default_rng(0x6005EC1A)

    # The main beak snap falls rapidly from a bright pulse into a dry clack.
    snap_frequency = 1_420.0 * np.exp(-31.0 * time) + 305.0
    snap = pulse_from_frequency(snap_frequency, duty=0.14)
    snap_envelope = (1.0 - np.exp(-2_400.0 * time)) * np.exp(-52.0 * time)
    mix += np.round(snap * 7.0) / 7.0 * snap_envelope * 0.46

    # A deterministic, high-passed noise transient provides the woody edge.
    noise = rng.choice(np.array([-1.0, 1.0]), size=FRAME_COUNT)
    high_noise = noise - np.concatenate(([0.0], noise[:-1])) * 0.78
    noise_envelope = (1.0 - np.exp(-3_200.0 * time)) * np.exp(-76.0 * time)
    mix += np.round(high_noise * 5.0) / 5.0 * noise_envelope * 0.115

    # A small low body makes the click readable on laptop speakers without a
    # long bass tail that would muddy fast navigation.
    body_frequency = 205.0 * np.exp(-24.0 * time) + 92.0
    body_phase = np.cumsum(body_frequency, dtype=np.float64) / SAMPLE_RATE
    body = 1.0 - 4.0 * np.abs(np.mod(body_phase, 1.0) - 0.5)
    body_envelope = (1.0 - np.exp(-1_100.0 * time)) * np.exp(-34.0 * time)
    mix += np.round(body * 15.0) / 15.0 * body_envelope * 0.16

    # A restrained C#5 -> D5 pulse resolves the project's dangerous leading
    # tone without turning every click into a melodic notification.
    add_tone(
        mix,
        start_seconds=0.019,
        duration_seconds=0.028,
        frequency_hz=554.365,
        amplitude=0.055,
    )
    add_tone(
        mix,
        start_seconds=0.047,
        duration_seconds=0.034,
        frequency_hz=587.330,
        amplitude=0.046,
    )

    mix -= np.mean(mix)
    peak = float(np.max(np.abs(mix)))
    mix *= 0.70 / max(peak, 1.0e-9)

    # Keep the transient immediate while landing both file edges on zero.
    attack_frames = round(0.0007 * SAMPLE_RATE)
    release_frames = round(0.014 * SAMPLE_RATE)
    attack_ramp = np.sin(
        np.linspace(0.0, math.pi / 2.0, attack_frames)
    ) ** 2
    release_ramp = np.sin(
        np.linspace(0.0, math.pi / 2.0, release_frames)
    ) ** 2
    mix[:attack_frames] *= attack_ramp
    mix[-release_frames:] *= release_ramp[::-1]

    # Retain deliberate chip grain in a 16-bit delivery file.
    mix = np.round(mix * 511.0) / 511.0
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
