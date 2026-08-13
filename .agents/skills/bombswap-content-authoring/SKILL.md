---
name: bombswap-content-authoring
description: Create, update, or validate BombSwap Unity content such as room prefabs and metadata, bomb or enemy definitions, ScriptableObjects, spawn points, grid cells, authoring bindings, and content validators. Use for content and serialized-asset work; do not use for Core gameplay rule implementation, WebGL verification alone, or playtest evidence analysis.
---

# BombSwap Content Authoring

Author content through safe Unity serialization paths and prove that logical metadata matches the 3D asset.

## Workflow

1. Read the root `AGENTS.md`, `Docs/INDEX.md`, `Docs/Systems/RoomAuthoring.md`, the relevant system document, `Docs/Development/CurrentState.md`, and current authoring code.
2. Inspect the active Unity instance, target scene or asset, existing IDs, references, import state, and Console baseline before editing.
3. Define the content contract: stable ID, type, grid origin and cells, required anchors, references, designer-visible behavior, and validation rules.
4. Use Unity Editor or Unity MCP operations for scenes, prefabs, ScriptableObjects, Input Actions, and ProjectSettings. Do not edit serialized YAML with text replacement.
5. Keep mutable run state out of content assets. Convert validated authoring data to immutable runtime/Core values at the boundary.
6. Add or extend an Editor validator for machine-checkable constraints such as duplicate IDs, out-of-range or overlapping cells, missing references, unreachable exits, and invalid anchors.
7. Re-read the saved asset, inspect the Console, and visually verify representative content. Preserve Undo and multi-object behavior for interactive Editor tools when applicable.
8. Run `./Tools/Verify.ps1 -Tier Full`. Use `$bombswap-webgl-verify` when content affects build inclusion, rendering cost, loading, input, or a milestone.
9. Update the owning Systems document and `CurrentState.md`; report exact validated assets, visual evidence, warnings, and unrun browser checks.

## Safety Boundaries

- Do not edit `Assets/Feel`, `Assets/Plugins`, package cache, generated IDE files, or unrelated vendor content.
- Do not treat a validator pass as proof that a room or bomb is fun, readable, or fair; schedule the relevant playtest.
- If the Unity Editor is open but unavailable to tools, stop before serialized mutations and ask for an Editor connection or manual handoff.

