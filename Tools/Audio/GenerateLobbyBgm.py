"""Generate the loopable Bomb Swap lobby chiptune.

The arrangement is deliberately data-driven and deterministic so that the
checked-in WAV can be reproduced without a DAW.  It renders 32 bars at 96 BPM
in D minor and keeps every ambience/delay operation circular, making the last
sample flow into the first sample when an AudioSource loops the clip.
"""

from __future__ import annotations

import math
import pathlib
import wave

import numpy as np


SAMPLE_RATE = 44_100
BPM = 96
BEAT_SECONDS = 60.0 / BPM
BAR_BEATS = 4
BAR_COUNT = 32
TOTAL_BEATS = BAR_BEATS * BAR_COUNT
FRAME_COUNT = round(TOTAL_BEATS * BEAT_SECONDS * SAMPLE_RATE)

PROJECT_ROOT = pathlib.Path(__file__).resolve().parents[2]
OUTPUT_PATH = (
    PROJECT_ROOT
    / "Assets"
    / "Game"
    / "Content"
    / "Audio"
    / "Music"
    / "BGM_Lobby_GooseExodus_8Bit_Loop.wav"
)


def midi_to_hz(note: float) -> float:
    return 440.0 * 2.0 ** ((note - 69.0) / 12.0)


def smoothstep(values: np.ndarray) -> np.ndarray:
    return values * values * (3.0 - 2.0 * values)


class ChipSynth:
    def __init__(self) -> None:
        self.mix = np.zeros((2, FRAME_COUNT), dtype=np.float32)
        self.rng = np.random.default_rng(0xB04B5A9)

    @staticmethod
    def _pan_gains(pan: float) -> tuple[float, float]:
        angle = (max(-1.0, min(1.0, pan)) + 1.0) * math.pi / 4.0
        return math.cos(angle), math.sin(angle)

    @staticmethod
    def _waveform(
        phase: np.ndarray,
        waveform: str,
        duty: float,
    ) -> np.ndarray:
        cycle = np.mod(phase, 1.0)
        if waveform == "pulse":
            return np.where(cycle < duty, 1.0, -1.0)
        if waveform == "triangle":
            return 1.0 - 4.0 * np.abs(cycle - 0.5)
        if waveform == "saw":
            return 2.0 * cycle - 1.0
        if waveform == "sine":
            return np.sin(phase * math.tau)
        raise ValueError(f"Unsupported waveform: {waveform}")

    def add_note(
        self,
        note: float,
        start_beat: float,
        length_beats: float,
        amplitude: float,
        *,
        waveform: str = "pulse",
        duty: float = 0.25,
        pan: float = 0.0,
        attack: float = 0.004,
        release: float = 0.045,
        vibrato_hz: float = 0.0,
        vibrato_depth: float = 0.0,
        crush_levels: int = 32,
    ) -> None:
        start = round(start_beat * BEAT_SECONDS * SAMPLE_RATE)
        count = round(length_beats * BEAT_SECONDS * SAMPLE_RATE)
        end = min(start + count, FRAME_COUNT)
        if start < 0 or start >= FRAME_COUNT or end <= start:
            return

        count = end - start
        time = np.arange(count, dtype=np.float32) / SAMPLE_RATE
        frequency = midi_to_hz(note)
        phase = frequency * time
        if vibrato_hz > 0.0 and vibrato_depth > 0.0:
            phase += (
                frequency
                * vibrato_depth
                * np.sin(math.tau * vibrato_hz * time)
                / (math.tau * vibrato_hz)
            )

        signal = self._waveform(phase, waveform, duty)
        if crush_levels > 0:
            signal = np.round(signal * crush_levels) / crush_levels

        envelope = np.ones(count, dtype=np.float32)
        attack_count = min(count, max(1, round(attack * SAMPLE_RATE)))
        release_count = min(count, max(1, round(release * SAMPLE_RATE)))
        envelope[:attack_count] *= smoothstep(
            np.linspace(0.0, 1.0, attack_count, dtype=np.float32)
        )
        envelope[-release_count:] *= smoothstep(
            np.linspace(1.0, 0.0, release_count, dtype=np.float32)
        )
        signal = signal.astype(np.float32, copy=False) * envelope * amplitude

        left, right = self._pan_gains(pan)
        self.mix[0, start:end] += signal * left
        self.mix[1, start:end] += signal * right

    def add_kick(self, start_beat: float, amplitude: float = 0.22) -> None:
        start = round(start_beat * BEAT_SECONDS * SAMPLE_RATE)
        count = round(0.18 * SAMPLE_RATE)
        end = min(start + count, FRAME_COUNT)
        if end <= start:
            return
        time = np.arange(end - start, dtype=np.float32) / SAMPLE_RATE
        progress = time / max(time[-1], 1.0 / SAMPLE_RATE)
        frequency = 112.0 * np.exp(-5.0 * time) + 43.0
        phase = np.cumsum(frequency, dtype=np.float32) / SAMPLE_RATE
        body = np.sin(math.tau * phase)
        click = np.where(np.mod(phase * 2.0, 1.0) < 0.5, 1.0, -1.0)
        signal = (body * 0.82 + click * 0.18) * (1.0 - progress) ** 3
        signal = np.round(signal * 31.0) / 31.0 * amplitude
        self.mix[0, start:end] += signal * 0.72
        self.mix[1, start:end] += signal * 0.72

    def add_snare(self, start_beat: float, amplitude: float = 0.12) -> None:
        start = round(start_beat * BEAT_SECONDS * SAMPLE_RATE)
        count = round(0.115 * SAMPLE_RATE)
        end = min(start + count, FRAME_COUNT)
        if end <= start:
            return
        time = np.arange(end - start, dtype=np.float32) / SAMPLE_RATE
        noise = self.rng.choice(
            np.array([-1.0, 1.0], dtype=np.float32), size=end - start
        )
        high_noise = noise - np.concatenate(([0.0], noise[:-1])) * 0.72
        tone = np.where(np.mod(178.0 * time, 1.0) < 0.3, 1.0, -1.0)
        signal = (high_noise * 0.72 + tone * 0.28) * np.exp(-34.0 * time)
        signal = np.round(signal * 15.0) / 15.0 * amplitude
        self.mix[0, start:end] += signal * 0.69
        self.mix[1, start:end] += signal * 0.76

    def add_hat(
        self,
        start_beat: float,
        amplitude: float = 0.035,
        pan: float = 0.0,
    ) -> None:
        start = round(start_beat * BEAT_SECONDS * SAMPLE_RATE)
        count = round(0.035 * SAMPLE_RATE)
        end = min(start + count, FRAME_COUNT)
        if end <= start:
            return
        time = np.arange(end - start, dtype=np.float32) / SAMPLE_RATE
        noise = self.rng.choice(
            np.array([-1.0, 1.0], dtype=np.float32), size=end - start
        )
        high_noise = noise - np.concatenate(([0.0], noise[:-1]))
        signal = high_noise * np.exp(-88.0 * time)
        signal = np.round(signal * 7.0) / 7.0 * amplitude
        left, right = self._pan_gains(pan)
        self.mix[0, start:end] += signal * left
        self.mix[1, start:end] += signal * right

    def add_crash(self, start_beat: float, amplitude: float = 0.06) -> None:
        start = round(start_beat * BEAT_SECONDS * SAMPLE_RATE)
        count = round(0.62 * SAMPLE_RATE)
        end = min(start + count, FRAME_COUNT)
        if end <= start:
            return
        time = np.arange(end - start, dtype=np.float32) / SAMPLE_RATE
        noise = self.rng.choice(
            np.array([-1.0, 1.0], dtype=np.float32), size=end - start
        )
        metallic = noise * np.sign(
            np.sin(math.tau * 4_103.0 * time)
            + np.sin(math.tau * 5_119.0 * time)
        )
        signal = metallic * np.exp(-5.8 * time)
        signal = np.round(signal * 11.0) / 11.0 * amplitude
        self.mix[0, start:end] += signal * 0.66
        self.mix[1, start:end] += signal * 0.78


CHORDS: dict[str, tuple[int, tuple[int, ...], tuple[int, ...]]] = {
    "Dm": (38, (50, 53, 57), (62, 65, 69, 74)),
    "Bb": (34, (46, 50, 53), (58, 62, 65, 70)),
    "Gm": (31, (43, 46, 50), (55, 58, 62, 67)),
    "A": (33, (45, 49, 52), (57, 61, 64, 69)),
    "C": (36, (48, 52, 55), (60, 64, 67, 72)),
    "F": (29, (41, 45, 48), (53, 57, 60, 65)),
    "Dm/A": (33, (45, 50, 53), (57, 62, 65, 69)),
}

PROGRESSION = (
    "Dm", "Bb", "Gm", "A", "Dm", "C", "Bb", "A",
    "Dm", "Bb", "F", "C", "Gm", "Dm/A", "Bb", "A",
    "Dm", "C", "Bb", "A", "Dm", "Gm", "Bb", "A",
    "Dm", "Bb", "Gm", "A", "Dm", "C", "Bb", "A",
)


THEME_A = (
    ((0.0, 1.45, 74), (1.5, 0.4, 69), (2.0, 0.9, 70), (3.0, 0.86, 73)),
    ((0.0, 0.9, 74), (1.0, 0.9, 77), (2.0, 0.4, 76), (2.5, 0.4, 74), (3.0, 0.86, 72)),
    ((0.0, 0.9, 70), (1.0, 0.9, 74), (2.0, 1.4, 77), (3.5, 0.36, 74)),
    ((0.0, 0.4, 73), (0.5, 0.4, 76), (1.0, 1.4, 81), (2.5, 0.4, 79), (3.0, 0.86, 76)),
)

THEME_B = (
    ((0.0, 0.9, 69), (1.0, 0.4, 70), (1.5, 0.4, 72), (2.0, 1.4, 74), (3.5, 0.36, 77)),
    ((0.0, 0.9, 74), (1.0, 0.9, 70), (2.0, 0.9, 65), (3.0, 0.86, 70)),
    ((0.0, 1.45, 69), (1.5, 0.4, 72), (2.0, 0.9, 77), (3.0, 0.86, 76)),
    ((0.0, 0.4, 74), (0.5, 0.4, 72), (1.0, 0.9, 67), (2.0, 0.9, 64), (3.0, 0.86, 72)),
)

THEME_REGAL = (
    ((0.0, 0.4, 74), (0.5, 0.4, 77), (1.0, 0.9, 81), (2.0, 0.4, 82), (2.5, 0.4, 81), (3.0, 0.86, 77)),
    ((0.0, 0.9, 76), (1.0, 0.9, 79), (2.0, 1.4, 84), (3.5, 0.36, 79)),
    ((0.0, 0.9, 77), (1.0, 0.4, 74), (1.5, 0.4, 70), (2.0, 0.9, 74), (3.0, 0.86, 77)),
    ((0.0, 0.4, 76), (0.5, 0.4, 73), (1.0, 0.9, 69), (2.0, 0.4, 73), (2.5, 0.4, 76), (3.0, 0.86, 81)),
)


def add_arrangement(synth: ChipSynth) -> None:
    # Harmonic bed, bass march and alternating arpeggio.
    for bar, chord_name in enumerate(PROGRESSION):
        root, pad_notes, arp_notes = CHORDS[chord_name]
        bar_start = bar * BAR_BEATS
        section = bar // 8

        pad_gain = (0.026, 0.031, 0.038, 0.028)[section]
        for index, note in enumerate(pad_notes):
            pan = (-0.46, 0.0, 0.46)[index]
            synth.add_note(
                note,
                bar_start,
                3.82,
                pad_gain,
                waveform="triangle",
                pan=pan,
                attack=0.055,
                release=0.12,
                crush_levels=24,
            )
            synth.add_note(
                note + 12,
                bar_start,
                3.78,
                pad_gain * 0.28,
                waveform="pulse",
                duty=0.125,
                pan=-pan,
                attack=0.07,
                release=0.15,
                vibrato_hz=4.1,
                vibrato_depth=0.0012,
                crush_levels=16,
            )

        bass_pattern = (
            (0.0, 0.82, root),
            (1.5, 0.36, root + 7),
            (2.0, 0.82, root),
            (3.5, 0.34, root + (7 if chord_name != "A" else 12)),
        )
        for offset, length, note in bass_pattern:
            synth.add_note(
                note,
                bar_start + offset,
                length,
                0.105 if section != 2 else 0.12,
                waveform="triangle",
                pan=-0.05,
                attack=0.006,
                release=0.06,
                crush_levels=24,
            )
            synth.add_note(
                note + 12,
                bar_start + offset,
                length * 0.72,
                0.026,
                waveform="pulse",
                duty=0.25,
                pan=0.08,
                attack=0.004,
                release=0.04,
                crush_levels=12,
            )

        arp_gain = (0.032, 0.044, 0.052, 0.038)[section]
        arp_order = (0, 1, 2, 3, 2, 1, 0, 1)
        if bar % 2:
            arp_order = (0, 2, 1, 3, 2, 0, 1, 2)
        for step, tone_index in enumerate(arp_order):
            pan = -0.33 if step % 2 == 0 else 0.33
            synth.add_note(
                arp_notes[tone_index],
                bar_start + step * 0.5,
                0.39,
                arp_gain,
                waveform="pulse",
                duty=0.125,
                pan=pan,
                attack=0.002,
                release=0.035,
                crush_levels=8,
            )

        # Measured, low-intensity lobby percussion that expands in the middle.
        kick_beats = (0.0,) if bar < 4 else (0.0, 2.0)
        if 16 <= bar < 24 and bar % 2 == 1:
            kick_beats += (3.5,)
        for offset in kick_beats:
            synth.add_kick(bar_start + offset)
        if bar >= 4:
            synth.add_snare(bar_start + 1.0, 0.085 if section == 3 else 0.105)
            synth.add_snare(bar_start + 3.0, 0.09 if section == 3 else 0.115)
        hat_stride = 1.0 if bar < 8 or bar >= 28 else 0.5
        for step in np.arange(0.5, 4.0, hat_stride):
            synth.add_hat(
                bar_start + float(step),
                0.021 if section in (0, 3) else 0.03,
                pan=-0.18 if int(step * 2) % 2 == 0 else 0.18,
            )

    for bar in (0, 8, 16, 24):
        synth.add_crash(bar * BAR_BEATS, 0.045 if bar == 0 else 0.062)

    # A distant four-note omen introduces the central melodic interval.
    intro_notes = (
        (0, 0.0, 1.7, 62),
        (1, 2.0, 1.7, 58),
        (2, 0.0, 1.7, 55),
        (3, 2.0, 1.65, 61),
    )
    for bar, offset, length, note in intro_notes:
        synth.add_note(
            note + 12,
            bar * BAR_BEATS + offset,
            length,
            0.062,
            waveform="pulse",
            duty=0.125,
            pan=0.22,
            attack=0.012,
            release=0.18,
            vibrato_hz=4.5,
            vibrato_depth=0.002,
            crush_levels=12,
        )

    def add_theme(
        first_bar: int,
        theme: tuple[tuple[tuple[float, float, int], ...], ...],
        repeats: int,
        gain: float,
        octave_shadow: bool = False,
    ) -> None:
        for repeat in range(repeats):
            for local_bar, notes in enumerate(theme):
                bar_start = (first_bar + repeat * len(theme) + local_bar) * BAR_BEATS
                for offset, length, note in notes:
                    synth.add_note(
                        note,
                        bar_start + offset,
                        length,
                        gain,
                        waveform="pulse",
                        duty=0.25,
                        pan=0.04,
                        attack=0.004,
                        release=0.07,
                        vibrato_hz=5.2,
                        vibrato_depth=0.0014,
                        crush_levels=16,
                    )
                    synth.add_note(
                        note - 12,
                        bar_start + offset + 0.025,
                        max(0.08, length - 0.03),
                        gain * (0.34 if octave_shadow else 0.2),
                        waveform="pulse",
                        duty=0.125,
                        pan=-0.22,
                        attack=0.006,
                        release=0.065,
                        crush_levels=12,
                    )
                    if octave_shadow:
                        synth.add_note(
                            note + 12,
                            bar_start + offset,
                            max(0.08, length - 0.05),
                            gain * 0.24,
                            waveform="pulse",
                            duty=0.125,
                            pan=0.3,
                            attack=0.003,
                            release=0.055,
                            crush_levels=8,
                        )

    add_theme(4, THEME_A, repeats=1, gain=0.092)
    add_theme(8, THEME_B, repeats=2, gain=0.104)
    add_theme(16, THEME_REGAL, repeats=2, gain=0.112, octave_shadow=True)

    # Return to the opening motif, then leave the dominant unresolved so the
    # next loop's D-minor downbeat supplies the resolution.
    add_theme(24, THEME_A, repeats=1, gain=0.087)
    closing = (
        (28, 0.0, 1.45, 74), (28, 1.5, 0.4, 69), (28, 2.0, 1.75, 65),
        (29, 0.0, 0.9, 67), (29, 1.0, 0.9, 64), (29, 2.0, 1.75, 60),
        (30, 0.0, 0.9, 58), (30, 1.0, 0.9, 62), (30, 2.0, 1.75, 65),
        (31, 0.0, 0.9, 69), (31, 1.0, 0.9, 73), (31, 2.0, 1.55, 76),
    )
    for bar, offset, length, note in closing:
        synth.add_note(
            note,
            bar * BAR_BEATS + offset,
            length,
            0.074,
            waveform="pulse",
            duty=0.25,
            pan=0.06,
            attack=0.005,
            release=0.11,
            vibrato_hz=4.7,
            vibrato_depth=0.0012,
            crush_levels=16,
        )


def master_loop(mix: np.ndarray) -> np.ndarray:
    # Circular delays preserve their tails across the clip boundary.
    dry = mix.copy()
    for delay_seconds, gain, crossfeed in (
        (0.137, 0.095, 0.18),
        (0.271, 0.065, 0.24),
        (0.419, 0.042, 0.32),
    ):
        delay = round(delay_seconds * SAMPLE_RATE)
        wrapped = np.roll(dry, delay, axis=1)
        mix[0] += wrapped[0] * gain + wrapped[1] * gain * crossfeed
        mix[1] += wrapped[1] * gain + wrapped[0] * gain * crossfeed

    mix -= np.mean(mix, axis=1, keepdims=True)
    mix = np.tanh(mix * 1.18)
    peak = float(np.max(np.abs(mix)))
    mix *= 0.89 / max(peak, 1.0e-6)
    # Mild master quantization retains chiptune grain without turning the WAV
    # itself into an 8-bit dynamic-range file.
    mix = np.round(mix * 2047.0) / 2047.0

    # Land both sides of the loop on digital zero.  The 18 ms cosine ramps are
    # shorter than a musical event but remove a possible codec/player click.
    seam_frames = round(0.018 * SAMPLE_RATE)
    seam_ramp = np.sin(
        np.linspace(0.0, math.pi / 2.0, seam_frames, dtype=np.float32)
    ) ** 2
    mix[:, :seam_frames] *= seam_ramp
    mix[:, -seam_frames:] *= seam_ramp[::-1]
    return mix


def write_wav(path: pathlib.Path, mix: np.ndarray) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    interleaved = np.clip(mix.T, -1.0, 1.0)
    pcm = (interleaved * 32767.0).astype("<i2", copy=False)
    with wave.open(str(path), "wb") as wav:
        wav.setnchannels(2)
        wav.setsampwidth(2)
        wav.setframerate(SAMPLE_RATE)
        wav.writeframes(pcm.tobytes())


def main() -> None:
    synth = ChipSynth()
    add_arrangement(synth)
    mastered = master_loop(synth.mix)
    write_wav(OUTPUT_PATH, mastered)

    seam_delta = np.abs(mastered[:, 0] - mastered[:, -1])
    rms = np.sqrt(np.mean(mastered * mastered, axis=1))
    print(f"Wrote: {OUTPUT_PATH}")
    print(f"Duration: {FRAME_COUNT / SAMPLE_RATE:.3f}s ({BAR_COUNT} bars at {BPM} BPM)")
    print(f"Stereo RMS: L={rms[0]:.5f}, R={rms[1]:.5f}")
    print(f"Loop seam delta: L={seam_delta[0]:.6f}, R={seam_delta[1]:.6f}")


if __name__ == "__main__":
    main()
