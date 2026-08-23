"""Generate Bomb Swap's layer-ready loopable dungeon chiptune.

The dungeon arrangement reuses the lobby theme's D-F-A-C# identity across a
room-neutral Base, rhythmic Combat, warning-focused Danger, and warm Sanctuary
layer. The stems share one timeline and master gain, so room transitions can
crossfade intensity without restarting the music.
"""

from __future__ import annotations

import pathlib

import numpy as np

import GenerateLobbyBgm as chip


BPM = 116
BAR_COUNT = 32
BAR_BEATS = 4

# ChipSynth is shared with the lobby generator. Its timing constants are module
# globals, so configure this render before constructing the synth.
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
    / "BGM_DungeonCombat_PowderCorridor_8Bit_Loop.wav"
)
BASE_LAYER_PATH = OUTPUT_PATH.with_name(
    "BGM_Dungeon_PowderCorridor_BaseLayer_8Bit_Loop.wav"
)
COMBAT_LAYER_PATH = OUTPUT_PATH.with_name(
    "BGM_Dungeon_PowderCorridor_CombatLayer_8Bit_Loop.wav"
)
DANGER_LAYER_PATH = OUTPUT_PATH.with_name(
    "BGM_Dungeon_PowderCorridor_DangerLayer_8Bit_Loop.wav"
)
SANCTUARY_LAYER_PATH = OUTPUT_PATH.with_name(
    "BGM_Dungeon_PowderCorridor_SanctuaryLayer_8Bit_Loop.wav"
)
RECOVERY_PREVIEW_PATH = OUTPUT_PATH.with_name(
    "BGM_DungeonRecovery_PowderCorridor_8Bit_Loop.wav"
)


# root / low chord / high arpeggio. Dm and A deliberately expose C# as the
# shared lobby motif's dangerous leading tone.
CHORDS: dict[str, tuple[int, tuple[int, ...], tuple[int, ...]]] = {
    "Dm": (38, (50, 53, 57), (62, 65, 69, 73)),
    "Dm/C": (36, (48, 50, 53, 57), (60, 62, 65, 69)),
    "Bb": (34, (46, 50, 53), (58, 62, 65, 70)),
    "A": (33, (45, 49, 52), (57, 61, 64, 69)),
    "Gm": (31, (43, 46, 50), (55, 58, 62, 67)),
    "C": (36, (48, 52, 55), (60, 64, 67, 72)),
    "Eb": (39, (51, 55, 58), (63, 67, 70, 75)),
    "F": (29, (41, 45, 48), (53, 57, 60, 65)),
}

PROGRESSION = (
    "Dm", "Dm/C", "Bb", "A", "Dm", "Eb", "Bb", "A",
    "Gm", "Dm", "Bb", "A", "Dm", "C", "Bb", "A",
    "Dm", "Eb", "Dm/C", "A", "Gm", "Dm", "Bb", "A",
    "Dm", "C", "Bb", "A", "Dm", "Eb", "Bb", "A",
)


WARNING_MOTIF = (
    ((0.0, 0.32, 74), (0.5, 0.32, 77), (1.0, 0.72, 81),
     (2.0, 0.32, 85), (2.5, 0.32, 81), (3.0, 0.72, 77)),
    ((0.0, 0.72, 74), (1.0, 0.32, 72), (1.5, 0.32, 70),
     (2.0, 0.72, 69), (3.0, 0.72, 73)),
)

CHASE_ANSWER = (
    ((0.0, 0.32, 69), (0.5, 0.32, 73), (1.0, 0.72, 76),
     (2.0, 0.32, 77), (2.5, 0.32, 76), (3.0, 0.72, 73)),
    ((0.0, 0.72, 70), (1.0, 0.72, 67),
     (2.0, 0.32, 65), (2.5, 0.32, 64), (3.0, 0.72, 61)),
)


def add_base_layer(synth: chip.ChipSynth) -> None:
    """Add the dark, room-neutral bed used from start to boss ante."""
    for bar, chord_name in enumerate(PROGRESSION):
        root, chord_notes, arp_notes = CHORDS[chord_name]
        bar_start = bar * BAR_BEATS
        section = bar // 8

        # Long low pedals preserve dungeon scale without implying combat.
        pedal_gain = (0.042, 0.046, 0.051, 0.045)[section]
        synth.add_note(
            root - 12,
            bar_start,
            3.88,
            pedal_gain,
            waveform="triangle",
            pan=-0.08,
            attack=0.055,
            release=0.23,
            crush_levels=24,
        )
        synth.add_note(
            root + 7,
            bar_start,
            3.78,
            pedal_gain * 0.36,
            waveform="triangle",
            pan=0.12,
            attack=0.06,
            release=0.21,
            crush_levels=20,
        )

        # A thin, wide chord makes cleared rooms feel inhabited rather than
        # silent. It remains sparse enough for doors, pickups, and footsteps.
        pad_gain = (0.014, 0.017, 0.02, 0.016)[section]
        for note_index, note in enumerate(chord_notes[:3]):
            pan = (-0.56, 0.0, 0.56)[note_index]
            synth.add_note(
                note + 12,
                bar_start,
                3.72,
                pad_gain,
                waveform="triangle",
                pan=pan,
                attack=0.095,
                release=0.27,
                vibrato_hz=3.7,
                vibrato_depth=0.0006,
                crush_levels=22,
            )

        # Two distant sparks per bar keep the common timeline perceptible when
        # combat layers are muted, but avoid an always-running ostinato.
        spark_order = (0, 2) if bar % 2 == 0 else (1, 3)
        for spark_index, tone_index in enumerate(spark_order):
            synth.add_note(
                arp_notes[tone_index],
                bar_start + 1.5 + spark_index * 2.0,
                0.16,
                0.012 if section != 2 else 0.014,
                waveform="pulse",
                duty=0.125,
                pan=-0.46 if spark_index == 0 else 0.46,
                attack=0.002,
                release=0.035,
                crush_levels=6,
            )

    # The goose motif appears as four distant pillars at section boundaries.
    for first_bar in (0, 8, 16, 24):
        start = first_bar * BAR_BEATS
        for note_index, midi in enumerate((62, 65, 69, 73)):
            synth.add_note(
                midi,
                start + note_index,
                0.72,
                0.026,
                waveform="triangle",
                pan=-0.36 + note_index * 0.24,
                attack=0.022,
                release=0.12,
                crush_levels=16,
            )


def add_combat_engine(synth: chip.ChipSynth) -> None:
    for bar, chord_name in enumerate(PROGRESSION):
        root, chord_notes, _ = CHORDS[chord_name]
        bar_start = bar * BAR_BEATS
        section = bar // 8

        # Short, organ-like chord stabs keep the midrange open between beats.
        stab_gain = (0.027, 0.032, 0.038, 0.03)[section]
        for stab_offset in (0.0, 2.0):
            for index, note in enumerate(chord_notes):
                pan_positions = (-0.4, -0.12, 0.22, 0.42)
                pan = pan_positions[index]
                synth.add_note(
                    note,
                    bar_start + stab_offset,
                    1.62,
                    stab_gain,
                    waveform="triangle",
                    pan=pan,
                    attack=0.018,
                    release=0.095,
                    crush_levels=20,
                )
                synth.add_note(
                    note + 12,
                    bar_start + stab_offset,
                    0.72,
                    stab_gain * 0.26,
                    waveform="pulse",
                    duty=0.125,
                    pan=-pan,
                    attack=0.004,
                    release=0.065,
                    crush_levels=8,
                )

        # Eight-step bass ostinato: forward pressure without a dense lead line.
        bass_steps = (
            root, root + 12, root + 7, root + 12,
            root, root + 7, root + 12, root + 7,
        )
        if chord_name == "A":
            bass_steps = (
                root, root + 12, root + 7, root + 12,
                root, root + 4, root + 7, root + 12,
            )
        bass_gain = (0.073, 0.082, 0.09, 0.078)[section]
        for step, note in enumerate(bass_steps):
            synth.add_note(
                note,
                bar_start + step * 0.5,
                0.39,
                bass_gain,
                waveform="triangle",
                pan=-0.07,
                attack=0.004,
                release=0.035,
                crush_levels=20,
            )
            if step in (0, 4):
                synth.add_note(
                    note + 12,
                    bar_start + step * 0.5,
                    0.27,
                    0.025,
                    waveform="pulse",
                    duty=0.25,
                    pan=0.08,
                    attack=0.002,
                    release=0.025,
                    crush_levels=8,
                )



def add_danger_layer(synth: chip.ChipSynth) -> None:
    """Add scalable fuse, telegraph, and high-intensity warning motion."""
    for bar, chord_name in enumerate(PROGRESSION):
        _, _, arp_notes = CHORDS[chord_name]
        bar_start = bar * BAR_BEATS
        section = bar // 8

        # Narrow off-beat pulses suggest a fuse while leaving every downbeat
        # clear for kick, explosion, and enemy telegraph sounds.
        arp_order = (0, 2, 1, 3)
        if bar % 2:
            arp_order = (1, 3, 2, 0)
        for step, tone_index in enumerate(arp_order):
            synth.add_note(
                arp_notes[tone_index],
                bar_start + 0.75 + step,
                0.19,
                (0.031, 0.037, 0.044, 0.035)[section],
                waveform="pulse",
                duty=0.125,
                pan=-0.34 if step % 2 == 0 else 0.34,
                attack=0.0015,
                release=0.025,
                crush_levels=6,
            )

        tick_offsets = (0.5, 1.5, 2.5, 3.5)
        if bar % 4 == 3:
            tick_offsets = (0.5, 1.5, 2.5, 3.0, 3.25, 3.5, 3.75)
        for tick_index, tick_offset in enumerate(tick_offsets):
            synth.add_note(
                93 + tick_index % 2,
                bar_start + tick_offset,
                0.065,
                0.012 if section != 2 else 0.016,
                waveform="pulse",
                duty=0.125,
                pan=0.44 if tick_index % 2 else -0.44,
                attack=0.001,
                release=0.012,
                crush_levels=4,
            )

        # At full layer volume, alternating warning hats and a quiet tritone
        # make elite rooms feel more dangerous without changing the base loop.
        if section >= 1:
            for step in range(8):
                synth.add_hat(
                    bar_start + 0.25 + step * 0.5,
                    0.009 if step % 2 else 0.013,
                    pan=-0.5 if step % 2 == 0 else 0.5,
                )
        if section == 2 and bar % 2 == 1:
            synth.add_note(
                73,
                bar_start + 1.25,
                0.22,
                0.018,
                waveform="pulse",
                duty=0.125,
                pan=-0.62,
                attack=0.001,
                release=0.03,
                crush_levels=4,
            )
            synth.add_note(
                67,
                bar_start + 3.25,
                0.22,
                0.018,
                waveform="pulse",
                duty=0.125,
                pan=0.62,
                attack=0.001,
                release=0.03,
                crush_levels=4,
            )


def add_percussion(synth: chip.ChipSynth) -> None:
    for bar in range(BAR_COUNT):
        bar_start = bar * BAR_BEATS
        section = bar // 8

        kick_offsets = [0.0, 2.0]
        if section == 2 and bar % 2 == 1:
            kick_offsets.append(2.75)
        if bar in (7, 15, 23, 31):
            kick_offsets.append(3.5)
        for offset in kick_offsets:
            synth.add_kick(bar_start + offset, 0.205 if section == 0 else 0.225)

        # Two one-bar breathers avoid an unbroken snare wall while retaining
        # the bass and fuse pulse, so combat never feels as if it has stopped.
        if bar not in (12, 24):
            synth.add_snare(bar_start + 1.0, 0.105 if section == 0 else 0.125)
            synth.add_snare(bar_start + 3.0, 0.115 if section == 0 else 0.135)

        for step in range(8):
            if section == 0 and step in (1, 5):
                continue
            synth.add_hat(
                bar_start + step * 0.5,
                0.022 if step % 2 == 0 else 0.016,
                pan=-0.22 if step % 2 == 0 else 0.22,
            )

    for bar in (0, 8, 16, 24):
        synth.add_crash(bar * BAR_BEATS, 0.052 if bar == 0 else 0.067)


def add_melodic_warnings(synth: chip.ChipSynth) -> None:
    def add_phrase(
        first_bar: int,
        phrase: tuple[tuple[tuple[float, float, int], ...], ...],
        *,
        gain: float,
        octave_layer: bool,
    ) -> None:
        for local_bar, notes in enumerate(phrase):
            bar_start = (first_bar + local_bar) * BAR_BEATS
            for offset, length, note in notes:
                synth.add_note(
                    note,
                    bar_start + offset,
                    length,
                    gain,
                    waveform="pulse",
                    duty=0.25,
                    pan=0.05,
                    attack=0.003,
                    release=0.052,
                    vibrato_hz=5.5,
                    vibrato_depth=0.0011,
                    crush_levels=12,
                )
                synth.add_note(
                    note - 12,
                    bar_start + offset + 0.02,
                    max(0.08, length - 0.03),
                    gain * 0.31,
                    waveform="pulse",
                    duty=0.125,
                    pan=-0.24,
                    attack=0.004,
                    release=0.045,
                    crush_levels=8,
                )
                if octave_layer:
                    synth.add_note(
                        note + 12,
                        bar_start + offset,
                        max(0.06, length - 0.06),
                        gain * 0.19,
                        waveform="pulse",
                        duty=0.125,
                        pan=0.28,
                        attack=0.002,
                        release=0.04,
                        crush_levels=6,
                    )

    # The first eight bars establish movement using rhythm alone. Thereafter
    # two-bar warnings alternate with two-bar gaps for gameplay readability.
    add_phrase(8, WARNING_MOTIF, gain=0.088, octave_layer=False)
    add_phrase(12, CHASE_ANSWER, gain=0.082, octave_layer=False)
    add_phrase(16, WARNING_MOTIF, gain=0.099, octave_layer=True)
    add_phrase(20, CHASE_ANSWER, gain=0.094, octave_layer=True)
    add_phrase(24, WARNING_MOTIF, gain=0.078, octave_layer=False)

    # Final four-bar descent leaves A major unresolved; the loop's opening Dm
    # supplies the release exactly as it does in the lobby theme.
    closing = (
        (28, 0.0, 0.7, 74), (28, 1.0, 0.7, 69), (28, 2.0, 1.55, 65),
        (29, 0.0, 0.7, 75), (29, 1.0, 0.7, 70), (29, 2.0, 1.55, 67),
        (30, 0.0, 0.7, 70), (30, 1.0, 0.7, 65), (30, 2.0, 1.55, 62),
        (31, 0.0, 0.7, 69), (31, 1.0, 0.7, 73), (31, 2.0, 1.45, 76),
    )
    for bar, offset, length, note in closing:
        synth.add_note(
            note,
            bar * BAR_BEATS + offset,
            length,
            0.071,
            waveform="pulse",
            duty=0.25,
            pan=0.04,
            attack=0.003,
            release=0.08,
            vibrato_hz=5.0,
            vibrato_depth=0.001,
            crush_levels=12,
        )


def add_sanctuary_layer(synth: chip.ChipSynth) -> None:
    """Add warm bells and soft upper harmony for recovery and reward rooms."""
    for bar, chord_name in enumerate(PROGRESSION):
        _, chord_notes, arp_notes = CHORDS[chord_name]
        bar_start = bar * BAR_BEATS
        section = bar // 8

        # Sine-like upper voices brighten the same progression instead of
        # replacing it, so crossfades never create a harmonic jump.
        pad_gain = (0.022, 0.025, 0.029, 0.024)[section]
        for note_index, note in enumerate(chord_notes[:3]):
            pan = (-0.48, 0.0, 0.48)[note_index]
            synth.add_note(
                note + 12,
                bar_start,
                3.76,
                pad_gain,
                waveform="sine",
                pan=pan,
                attack=0.12,
                release=0.32,
                vibrato_hz=3.2,
                vibrato_depth=0.0007,
                crush_levels=32,
            )

        # Bell answers arrive only every other bar, leaving pickup and chest
        # SFX room. The final C# still resolves into the loop's opening D.
        if bar % 2 == 0:
            bell_order = (0, 1, 2) if bar % 4 == 0 else (2, 1, 3)
            for bell_index, tone_index in enumerate(bell_order):
                midi = arp_notes[tone_index] + 12
                start = bar_start + 0.5 + bell_index * 1.25
                gain = 0.04 if bell_index == 0 else 0.032
                synth.add_note(
                    midi,
                    start,
                    0.48,
                    gain,
                    waveform="sine",
                    pan=-0.42 + bell_index * 0.42,
                    attack=0.003,
                    release=0.18,
                    crush_levels=24,
                )
                synth.add_note(
                    midi + 12,
                    start,
                    0.28,
                    gain * 0.23,
                    waveform="pulse",
                    duty=0.125,
                    pan=0.42 - bell_index * 0.32,
                    attack=0.002,
                    release=0.11,
                    crush_levels=7,
                )

    # Four slow D-F-A-C# chimes identify a safe special room while retaining
    # the project's uneasy final leading tone.
    for first_bar in (2, 10, 18, 26):
        start = first_bar * BAR_BEATS
        for note_index, midi in enumerate((74, 77, 81, 73)):
            synth.add_note(
                midi,
                start + note_index * 0.75,
                0.66,
                0.049,
                waveform="sine",
                pan=-0.54 + note_index * 0.36,
                attack=0.005,
                release=0.22,
                crush_levels=28,
            )


def add_circular_space(mix: np.ndarray) -> np.ndarray:
    """Apply identical linear, loop-safe ambience to one stem."""
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
) -> tuple[np.ndarray, ...]:
    """Use one gain and seam ramp for every adaptive-music layer."""
    spaced = tuple(add_circular_space(stem) for stem in stems)
    base, combat, danger, sanctuary = spaced
    loudest_profiles = (
        base + combat + danger,
        base + combat + danger * 0.45,
        base * 0.75 + sanctuary,
        base * 0.85 + sanctuary * 0.6,
    )
    peak = max(float(np.max(np.abs(profile))) for profile in loudest_profiles)
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
    return tuple(mastered)


def build_preview(
    stems: tuple[np.ndarray, ...],
    weights: tuple[float, ...],
) -> np.ndarray:
    preview = np.zeros_like(stems[0])
    for stem, weight in zip(stems, weights, strict=True):
        preview += stem * weight
    preview = np.round(preview * 2047.0) / 2047.0
    return np.clip(preview, -0.98, 0.98).astype(np.float32, copy=False)


def main() -> None:
    base = chip.ChipSynth()
    add_base_layer(base)

    combat = chip.ChipSynth()
    add_combat_engine(combat)
    add_percussion(combat)
    add_melodic_warnings(combat)

    danger = chip.ChipSynth()
    add_danger_layer(danger)

    sanctuary = chip.ChipSynth()
    add_sanctuary_layer(sanctuary)

    mastered = master_aligned_stems(
        (base.mix, combat.mix, danger.mix, sanctuary.mix)
    )
    combat_preview = build_preview(mastered, (1.0, 1.0, 0.45, 0.0))
    recovery_preview = build_preview(mastered, (0.75, 0.0, 0.0, 1.0))

    outputs = (
        (BASE_LAYER_PATH, mastered[0]),
        (COMBAT_LAYER_PATH, mastered[1]),
        (DANGER_LAYER_PATH, mastered[2]),
        (SANCTUARY_LAYER_PATH, mastered[3]),
        (RECOVERY_PREVIEW_PATH, recovery_preview),
        (OUTPUT_PATH, combat_preview),
    )
    for path, mix in outputs:
        chip.write_wav(path, mix)
        seam_delta = np.abs(mix[:, 0] - mix[:, -1])
        rms = np.sqrt(np.mean(mix * mix, axis=1))
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
