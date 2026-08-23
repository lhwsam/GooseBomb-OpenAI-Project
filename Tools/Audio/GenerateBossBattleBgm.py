"""Generate Bomb Swap's loopable, layer-ready 8-bit boss battle theme.

"The Overheated Throne" keeps the project's D-F-A-C# musical identity but
reverses and chromatically distorts it. A 3+3+2 accent grid evokes the boss's
mechanical chase, stereo sweeps suggest parity rows, accelerating ticks recall
the self-destruct fuse, and brief dropouts leave room for overheat recovery.

The full preview and its Base, Grand, and Danger stems share the same sample
count, circular ambience, master gain, and seam ramp. Starting them at the same
DSP time therefore keeps every transient and bar boundary sample-accurate.
"""

from __future__ import annotations

import pathlib

import numpy as np

import GenerateLobbyBgm as chip


BPM = 128
BAR_COUNT = 32
BAR_BEATS = 4

chip.BPM = BPM
chip.BEAT_SECONDS = 60.0 / BPM
chip.BAR_COUNT = BAR_COUNT
chip.BAR_BEATS = BAR_BEATS
chip.TOTAL_BEATS = BAR_COUNT * BAR_BEATS
chip.FRAME_COUNT = round(chip.TOTAL_BEATS * chip.BEAT_SECONDS * chip.SAMPLE_RATE)

PROJECT_ROOT = pathlib.Path(__file__).resolve().parents[2]
OUTPUT_PATH = (
    PROJECT_ROOT
    / "Assets"
    / "Game"
    / "Content"
    / "Audio"
    / "Music"
    / "BGM_BossBattle_OverheatedThrone_8Bit_Loop.wav"
)
BASE_LAYER_PATH = OUTPUT_PATH.with_name(
    "BGM_BossBattle_OverheatedThrone_BaseLayer_8Bit_Loop.wav"
)
GRAND_LAYER_PATH = OUTPUT_PATH.with_name(
    "BGM_BossBattle_OverheatedThrone_GrandLayer_8Bit_Loop.wav"
)
DANGER_LAYER_PATH = OUTPUT_PATH.with_name(
    "BGM_BossBattle_OverheatedThrone_DangerLayer_8Bit_Loop.wav"
)


CHORDS: dict[str, tuple[int, tuple[int, ...], tuple[int, ...]]] = {
    "Dm": (38, (50, 53, 57), (62, 65, 69, 73)),
    "Eb/D": (38, (50, 51, 55, 58), (62, 63, 67, 70)),
    "Bb": (34, (46, 50, 53), (58, 62, 65, 70)),
    "Gm": (31, (43, 46, 50), (55, 58, 62, 67)),
    "A": (33, (45, 49, 52), (57, 61, 64, 69)),
    "C": (36, (48, 52, 55), (60, 64, 67, 72)),
    "C#dim": (37, (49, 52, 55), (61, 64, 67, 73)),
}

PROGRESSION = (
    "Dm", "Eb/D", "Dm", "A", "Dm", "Bb", "Gm", "A",
    "Dm", "Eb/D", "Bb", "A", "Gm", "Dm", "C#dim", "A",
    "Dm", "Eb/D", "Dm", "A", "Bb", "Gm", "C#dim", "A",
    "Dm", "C", "Bb", "A", "Dm", "Eb/D", "C#dim", "A",
)


def add_mechanical_core(synth: chip.ChipSynth) -> None:
    for bar, chord_name in enumerate(PROGRESSION):
        root, chord_notes, arp_notes = CHORDS[chord_name]
        bar_start = bar * BAR_BEATS
        section = bar // 8
        recovery_bar = bar in (7, 15, 31)

        # Heavy 3+3+2 accents: beats 0, 1.5 and 3.
        accent_offsets = (0.0, 1.5, 3.0)
        stab_gain = (0.038, 0.043, 0.05, 0.041)[section]
        if recovery_bar:
            stab_gain *= 0.72
        for accent_index, accent in enumerate(accent_offsets):
            length = 0.86 if accent_index < 2 else 0.58
            for note_index, note in enumerate(chord_notes):
                pans = (-0.44, -0.14, 0.18, 0.43)
                pan = pans[note_index]
                synth.add_note(
                    note,
                    bar_start + accent,
                    length,
                    stab_gain,
                    waveform="triangle",
                    pan=pan,
                    attack=0.009,
                    release=0.07,
                    crush_levels=18,
                )
                synth.add_note(
                    note + 12,
                    bar_start + accent,
                    length * 0.58,
                    stab_gain * 0.31,
                    waveform="pulse",
                    duty=0.125,
                    pan=-pan,
                    attack=0.002,
                    release=0.045,
                    crush_levels=7,
                )

        # Repeating eight-step engine with harder accents at 0/3/6.
        bass_steps = (
            root, root + 7, root + 12, root,
            root + 3, root + 7, root + 12, root + 7,
        )
        if chord_name in ("A", "C#dim"):
            bass_steps = (
                root, root + 7, root + 12, root,
                root + 4, root + 7, root + 12, root + 4,
            )
        base_gain = (0.086, 0.094, 0.105, 0.09)[section]
        for step, note in enumerate(bass_steps):
            accent = step in (0, 3, 6)
            gain = base_gain * (1.22 if accent else 0.74)
            synth.add_note(
                note,
                bar_start + step * 0.5,
                0.38,
                gain,
                waveform="triangle",
                pan=-0.06,
                attack=0.003,
                release=0.032,
                crush_levels=16,
            )
            if accent:
                synth.add_note(
                    note + 12,
                    bar_start + step * 0.5,
                    0.23,
                    0.031,
                    waveform="pulse",
                    duty=0.25,
                    pan=0.07,
                    attack=0.002,
                    release=0.025,
                    crush_levels=7,
                )

        # Parity-wave impression: a row of short pulses crosses the stereo
        # field, reversing direction every bar just like alternating parity.
        sweep_order = (0, 1, 2, 3, 2, 1)
        sweep_gain = (0.026, 0.034, 0.043, 0.033)[section]
        for step, tone_index in enumerate(sweep_order):
            direction = step / (len(sweep_order) - 1)
            pan = -0.72 + direction * 1.44
            if bar % 2:
                pan = -pan
            synth.add_note(
                arp_notes[tone_index],
                bar_start + 0.25 + step * 0.625,
                0.16,
                sweep_gain,
                waveform="pulse",
                duty=0.125,
                pan=pan,
                attack=0.001,
                release=0.02,
                crush_levels=5,
            )


def add_boss_percussion(synth: chip.ChipSynth) -> None:
    for bar in range(BAR_COUNT):
        bar_start = bar * BAR_BEATS
        section = bar // 8
        recovery_bar = bar in (7, 15, 31)

        kick_offsets = (0.0, 1.5, 3.0)
        if section == 2 and not recovery_bar:
            kick_offsets += (2.5, 3.5)
        for offset in kick_offsets:
            synth.add_kick(
                bar_start + offset,
                0.215 if recovery_bar else (0.235 if section < 2 else 0.255),
            )

        if not recovery_bar:
            synth.add_snare(bar_start + 1.0, 0.12 if section == 0 else 0.14)
            synth.add_snare(bar_start + 2.5, 0.125 if section == 0 else 0.15)
        else:
            # One restrained hit marks the boss exposing itself in overheat.
            synth.add_snare(bar_start + 2.0, 0.082)

        hat_stride = 0.5 if section < 2 else 0.25
        for offset in np.arange(0.0, 4.0, hat_stride):
            if recovery_bar and offset not in (0.0, 2.0):
                continue
            step = round(float(offset) / hat_stride)
            synth.add_hat(
                bar_start + float(offset),
                0.019 if step % 2 == 0 else 0.013,
                pan=-0.26 if step % 2 == 0 else 0.26,
            )

    for bar in (0, 8, 16, 24):
        synth.add_crash(bar * BAR_BEATS, 0.059 if bar == 0 else 0.074)


def add_fuse_pressure(synth: chip.ChipSynth) -> None:
    # Phase two and last-stand sections receive increasingly fast warning
    # ticks. They stop during recovery bars, leaving gameplay SFX audible.
    for bar in range(8, 24):
        if bar == 15:
            continue
        section = bar // 8
        bar_start = bar * BAR_BEATS
        if section == 1:
            offsets = (0.5, 1.5, 2.5, 3.0, 3.5)
        else:
            offsets = tuple(np.arange(0.25, 4.0, 0.5))
            if bar in (22, 23):
                offsets = tuple(np.arange(0.125, 4.0, 0.25))
        for tick_index, offset in enumerate(offsets):
            synth.add_note(
                93 + tick_index % 3,
                bar_start + float(offset),
                0.055 if section == 1 else 0.04,
                0.013 if section == 1 else 0.017,
                waveform="pulse",
                duty=0.125,
                pan=-0.5 if tick_index % 2 == 0 else 0.5,
                attack=0.001,
                release=0.01,
                crush_levels=4,
            )


def add_distorted_theme(synth: chip.ChipSynth) -> None:
    def note(
        midi: int,
        beat: float,
        length: float,
        gain: float,
        *,
        octave: bool = False,
        pan: float = 0.03,
    ) -> None:
        synth.add_note(
            midi,
            beat,
            length,
            gain,
            waveform="pulse",
            duty=0.25,
            pan=pan,
            attack=0.003,
            release=0.055,
            vibrato_hz=5.8,
            vibrato_depth=0.0013,
            crush_levels=11,
        )
        synth.add_note(
            midi - 12,
            beat + 0.018,
            max(0.07, length - 0.025),
            gain * 0.34,
            waveform="pulse",
            duty=0.125,
            pan=-0.25,
            attack=0.003,
            release=0.045,
            crush_levels=7,
        )
        if octave:
            synth.add_note(
                midi + 12,
                beat,
                max(0.06, length - 0.045),
                gain * 0.2,
                waveform="pulse",
                duty=0.125,
                pan=0.3,
                attack=0.002,
                release=0.038,
                crush_levels=5,
            )

    # Phase one: the lobby identity appears as massive warning pillars.
    phase_one = (
        (4, 0.0, 0.72, 74), (4, 1.0, 0.72, 77),
        (4, 2.0, 0.72, 81), (4, 3.0, 0.72, 85),
        (5, 0.0, 1.2, 81), (5, 1.5, 0.45, 77),
        (5, 2.0, 0.45, 75), (5, 3.0, 0.72, 73),
        (6, 0.0, 0.72, 74), (6, 1.0, 0.72, 69),
        (6, 2.0, 1.55, 65),
    )
    for bar, offset, length, midi in phase_one:
        note(midi, bar * BAR_BEATS + offset, length, 0.09)

    # Phase two: reverse C#-A-F-D and insert Eb to make it feel corrupted.
    phase_two = (
        (12, 0.0, 0.42, 85), (12, 0.75, 0.42, 81),
        (12, 1.5, 0.42, 77), (12, 2.25, 0.42, 74),
        (12, 3.0, 0.72, 75),
        (13, 0.0, 0.72, 74), (13, 1.0, 0.72, 77),
        (13, 2.0, 0.42, 73), (13, 2.75, 0.42, 70),
        (13, 3.5, 0.32, 69),
        (14, 0.0, 0.72, 67), (14, 1.0, 0.72, 70),
        (14, 2.0, 1.55, 73),
    )
    for bar, offset, length, midi in phase_two:
        note(midi, bar * BAR_BEATS + offset, length, 0.096)

    # Last stand: compact syncopated fragments, doubled at the octave.
    last_stand = (
        (20, 0.0, 0.3, 74), (20, 0.5, 0.3, 77),
        (20, 1.0, 0.3, 81), (20, 1.5, 0.3, 85),
        (20, 2.0, 0.3, 86), (20, 2.5, 0.3, 85),
        (20, 3.0, 0.3, 81), (20, 3.5, 0.3, 77),
        (21, 0.0, 0.72, 74), (21, 1.0, 0.3, 73),
        (21, 1.5, 0.3, 70), (21, 2.0, 0.72, 67),
        (21, 3.0, 0.72, 73),
        (22, 0.0, 0.3, 76), (22, 0.5, 0.3, 77),
        (22, 1.0, 0.72, 81), (22, 2.0, 0.3, 85),
        (22, 2.5, 0.3, 81), (22, 3.0, 0.72, 76),
    )
    for bar, offset, length, midi in last_stand:
        note(midi, bar * BAR_BEATS + offset, length, 0.105, octave=True)

    # Final descent returns to the dominant, allowing the loop's first Dm to
    # land like the boss beginning another attack cycle.
    ending = (
        (28, 0.0, 0.72, 74), (28, 1.0, 0.72, 69), (28, 2.0, 1.55, 65),
        (29, 0.0, 0.72, 75), (29, 1.0, 0.72, 70), (29, 2.0, 1.55, 67),
        (30, 0.0, 0.72, 73), (30, 1.0, 0.72, 67), (30, 2.0, 1.55, 64),
        (31, 0.0, 0.72, 69), (31, 1.0, 0.72, 73), (31, 2.0, 1.42, 76),
    )
    for bar, offset, length, midi in ending:
        note(midi, bar * BAR_BEATS + offset, length, 0.077)


def add_grand_layer(synth: chip.ChipSynth) -> None:
    """Add low organ, chip choir, heraldic horns, and war-drum impacts."""
    for bar, chord_name in enumerate(PROGRESSION):
        root, chord_notes, _ = CHORDS[chord_name]
        bar_start = bar * BAR_BEATS
        section = bar // 8
        recovery_bar = bar in (7, 15, 31)

        # A cathedral-scale pedal made from two low, softly crushed voices.
        # Rearticulating at the half-bar keeps it legible beneath combat SFX.
        pedal_gain = (0.052, 0.06, 0.069, 0.064)[section]
        for offset in (0.0, 2.0):
            synth.add_note(
                root - 12,
                bar_start + offset,
                1.92,
                pedal_gain,
                waveform="triangle",
                pan=-0.08,
                attack=0.035,
                release=0.16,
                crush_levels=24,
            )
            synth.add_note(
                root,
                bar_start + offset,
                1.84,
                pedal_gain * 0.43,
                waveform="pulse",
                duty=0.5,
                pan=0.1,
                attack=0.028,
                release=0.14,
                crush_levels=13,
            )

        # Wide three-voice chip choir. Recovery bars thin out to preserve the
        # vulnerable window while still carrying the harmonic scale.
        choir_gain = (0.025, 0.033, 0.041, 0.036)[section]
        if recovery_bar:
            choir_gain *= 0.55
        for note_index, chord_note in enumerate(chord_notes[:3]):
            pan = (-0.62, 0.0, 0.62)[note_index]
            synth.add_note(
                chord_note + 12,
                bar_start,
                3.82,
                choir_gain,
                waveform="triangle",
                pan=pan,
                attack=0.075,
                release=0.24,
                vibrato_hz=4.2,
                vibrato_depth=0.0008,
                crush_levels=20,
            )
            synth.add_note(
                chord_note + 24,
                bar_start + 0.012 * note_index,
                3.68,
                choir_gain * 0.24,
                waveform="pulse",
                duty=0.25,
                pan=-pan,
                attack=0.06,
                release=0.22,
                crush_levels=9,
            )

    # D-F-A-C# becomes a boss fanfare. Each return gains a higher octave and
    # denser answer, so the final cycle feels larger without changing tempo.
    fanfare = ((0.0, 62), (0.75, 65), (1.5, 69), (2.25, 73))
    for section_bar in (0, 8, 16, 24):
        section = section_bar // 8
        start = section_bar * BAR_BEATS
        for note_index, (offset, midi) in enumerate(fanfare):
            gain = 0.058 + section * 0.008
            synth.add_note(
                midi,
                start + offset,
                0.62,
                gain,
                waveform="pulse",
                duty=0.25,
                pan=-0.42 + note_index * 0.28,
                attack=0.008,
                release=0.1,
                vibrato_hz=5.1,
                vibrato_depth=0.001,
                crush_levels=13,
            )
            synth.add_note(
                midi + 12,
                start + offset,
                0.56,
                gain * (0.28 + section * 0.04),
                waveform="pulse",
                duty=0.125,
                pan=0.42 - note_index * 0.22,
                attack=0.006,
                release=0.085,
                crush_levels=7,
            )

        # Half-time war drums make the section entrances land like boss phases.
        synth.add_kick(start, 0.31)
        synth.add_kick(start + 2.0, 0.245)
        synth.add_crash(start, 0.086)
        if section > 0:
            synth.add_snare(start + 3.0, 0.13 + section * 0.012)


def add_danger_layer(synth: chip.ChipSynth) -> None:
    """Add fuse ticks, alarms, parity sweeps, and last-stand percussion."""
    add_fuse_pressure(synth)

    # A C#-G tritone flashes like an 8-bit warning siren. It enters softly in
    # phase two and becomes an octave alarm during the final quarter.
    for bar in range(8, 31):
        if bar in (15, 31):
            continue
        bar_start = bar * BAR_BEATS
        last_stand = bar >= 24
        offsets = (1.0, 3.0) if not last_stand else (0.5, 1.5, 2.5, 3.5)
        for alarm_index, offset in enumerate(offsets):
            midi = (73, 67)[alarm_index % 2]
            synth.add_note(
                midi + (12 if last_stand else 0),
                bar_start + offset,
                0.18 if last_stand else 0.27,
                0.022 if last_stand else 0.014,
                waveform="pulse",
                duty=0.125,
                pan=-0.66 if alarm_index % 2 == 0 else 0.66,
                attack=0.001,
                release=0.025,
                crush_levels=4,
            )

    # Double-time metallic movement is reserved for the last stand, making the
    # layer useful as a clean gameplay-state intensity control.
    for bar in range(24, 31):
        bar_start = bar * BAR_BEATS
        for step, offset in enumerate(np.arange(0.0, 4.0, 0.25)):
            synth.add_hat(
                bar_start + float(offset),
                0.017 if step % 4 == 0 else 0.01,
                pan=-0.58 if step % 2 == 0 else 0.58,
            )
        for offset in (0.75, 2.25, 3.25):
            synth.add_snare(bar_start + offset, 0.058)

        # Rising parity pulses sweep across the field on alternating bars.
        for step in range(8):
            pan = -0.82 + step * (1.64 / 7.0)
            if bar % 2:
                pan = -pan
            synth.add_note(
                86 + step,
                bar_start + step * 0.5,
                0.09,
                0.018 + step * 0.0012,
                waveform="pulse",
                duty=0.125,
                pan=pan,
                attack=0.001,
                release=0.014,
                crush_levels=4,
            )


def add_circular_space(mix: np.ndarray) -> np.ndarray:
    """Apply the same linear, loop-safe ambience to an individual stem."""
    dry = mix.copy()
    spaced = mix.copy()
    for delay_seconds, gain, crossfeed in (
        (0.137, 0.095, 0.18),
        (0.271, 0.065, 0.24),
        (0.419, 0.042, 0.32),
    ):
        delay = round(delay_seconds * chip.SAMPLE_RATE)
        wrapped = np.roll(dry, delay, axis=1)
        spaced[0] += wrapped[0] * gain + wrapped[1] * gain * crossfeed
        spaced[1] += wrapped[1] * gain + wrapped[0] * gain * crossfeed
    spaced -= np.mean(spaced, axis=1, keepdims=True)
    return spaced


def master_aligned_stems(
    stems: tuple[np.ndarray, ...],
) -> tuple[tuple[np.ndarray, ...], np.ndarray]:
    """Master stems linearly so their unity sum matches the preview mix."""
    spaced = tuple(add_circular_space(stem) for stem in stems)
    composite = np.sum(np.stack(spaced, axis=0), axis=0)
    peak = float(np.max(np.abs(composite)))
    shared_gain = 0.89 / max(peak, 1.0e-6)

    seam_frames = round(0.018 * chip.SAMPLE_RATE)
    seam_ramp = np.sin(
        np.linspace(0.0, np.pi / 2.0, seam_frames, dtype=np.float32)
    ) ** 2

    mastered: list[np.ndarray] = []
    for stem in spaced:
        result = stem * shared_gain
        result[:, :seam_frames] *= seam_ramp
        result[:, -seam_frames:] *= seam_ramp[::-1]
        result = np.round(result * 4095.0) / 4095.0
        mastered.append(result.astype(np.float32, copy=False))

    full_mix = np.sum(np.stack(mastered, axis=0), axis=0)
    full_mix = np.round(full_mix * 2047.0) / 2047.0
    full_mix = np.clip(full_mix, -0.98, 0.98).astype(np.float32, copy=False)
    return tuple(mastered), full_mix


def main() -> None:
    base = chip.ChipSynth()
    add_mechanical_core(base)
    add_boss_percussion(base)
    add_distorted_theme(base)

    grand = chip.ChipSynth()
    add_grand_layer(grand)

    danger = chip.ChipSynth()
    add_danger_layer(danger)

    mastered_stems, full_mix = master_aligned_stems(
        (base.mix, grand.mix, danger.mix)
    )
    outputs = (
        (BASE_LAYER_PATH, mastered_stems[0]),
        (GRAND_LAYER_PATH, mastered_stems[1]),
        (DANGER_LAYER_PATH, mastered_stems[2]),
        (OUTPUT_PATH, full_mix),
    )
    for path, mastered in outputs:
        chip.write_wav(path, mastered)
        seam_delta = np.abs(mastered[:, 0] - mastered[:, -1])
        rms = np.sqrt(np.mean(mastered * mastered, axis=1))
        print(f"Wrote: {path}")
        print(f"  Stereo RMS: L={rms[0]:.5f}, R={rms[1]:.5f}")
        print(
            f"  Loop seam delta: L={seam_delta[0]:.6f}, "
            f"R={seam_delta[1]:.6f}"
        )

    print(
        f"Duration: {chip.FRAME_COUNT / chip.SAMPLE_RATE:.3f}s "
        f"({BAR_COUNT} bars at {BPM} BPM)"
    )


if __name__ == "__main__":
    main()
