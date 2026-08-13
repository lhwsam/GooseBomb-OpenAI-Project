---
name: bombswap-gameplay-change
description: Implement or modify BombSwap gameplay mechanics in this Unity repository, including grid movement, bombs, explosions, weapon slots, cooldowns, damage, enemies, dungeon generation, bosses, input-to-command adapters, and their tests. Use for concrete gameplay code changes; do not use for content-only prefab or ScriptableObject authoring, verification-only WebGL requests, or playtest analysis without implementation.
---

# BombSwap Gameplay Change

Implement the smallest complete gameplay change while preserving the logical-grid and WebGL contracts.

## Workflow

1. Read the root `AGENTS.md`, `Docs/INDEX.md`, `Docs/Development/CurrentState.md`, and the relevant GameDesign, Systems, ADR, and Testing documents.
2. Inspect `git status`, the affected assemblies, existing tests, and the live Unity Console when an Editor connection is available.
3. Define a task contract using `Docs/AI/TaskContract.md`: observable behavior, state owner, invariants, non-goals, affected paths, and required evidence.
4. Put deterministic rules in `BombSwap.Core`. Keep Input System, MonoBehaviour, Transform, physics, authoring, and presentation in `BombSwap.Unity`.
5. Add or update EditMode tests with Core rules. Add PlayMode tests only for Unity lifecycle, scene, prefab, input, or presentation integration.
6. Modify serialized assets only through the Unity Editor or a validated Editor tool. Never perform YAML text replacement.
7. Run `./Tools/Verify.ps1 -Tier Fast` during Core iteration. Run `-Tier Full` before completing a Unity-integrated feature. Use `$bombswap-webgl-verify` when the change affects input, rendering, packages, build behavior, or a milestone.
8. Review the final diff against the root Code Review Rules. Update the owning system document and `CurrentState.md` when behavior or project state changed.
9. Report changed behavior, decisions, exact validation results, unrun checks, risks, and the next safe step using `Docs/AI/HandoffTemplate.md`.

## Stop Conditions

- Ask before making an unresolved choice that changes a public contract, serialized schema, architecture boundary, or player-visible design beyond the GDD.
- Stop and report if the required Unity project is already open but cannot be validated through the connected Editor; do not launch a competing Unity instance.
- Do not claim a prototype hypothesis passed from automated tests alone.

