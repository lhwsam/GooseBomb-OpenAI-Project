---
name: bombswap-playtest-review
description: Analyze BombSwap prototype playtest notes, interviews, telemetry, session logs, videos, or observations against the documented hypotheses for bombs, two-slot choice, cooldown rotation, hit-count enemies, dungeon exploration, and boss combat. Use for evidence synthesis and keep/change/drop recommendations; do not use for automated correctness verification or gameplay implementation unless separately requested.
---

# BombSwap Playtest Review

Turn qualitative and quantitative playtest evidence into bounded decisions without treating activity metrics as proof of fun.

## Workflow

1. Read `Docs/GameDesign/ProtoType_v0.2.md`, the relevant GDD sections, `Docs/Systems/Telemetry.md`, and `Docs/Development/CurrentState.md`.
2. Identify the tested build/content version, run seed, participant context, test scenario, evidence sources, and missing data. Keep observations separate from interpretations.
3. Map each observation or metric to one documented hypothesis. Do not combine sessions with incompatible rules or telemetry schemas without labeling the difference.
4. Triangulate behavior, telemetry, interview answers, and facilitator observations. Call out contradictions and plausible confounders such as unclear UI, unfamiliar controls, frame drops, or room bias.
5. Rate each hypothesis as `Supported`, `Mixed`, `Not supported`, or `Insufficient evidence`. State confidence and the concrete evidence behind it.
6. Recommend `Keep`, `Modify`, or `Drop` only within the prototype criteria. Prefer one testable change at a time and define the next observation or metric that would distinguish outcomes.
7. Produce a concise report with session scope, evidence quality, hypothesis table, notable moments, decision recommendations, risks, and next playtest design.

## Integrity Rules

- Do not infer intent, enjoyment, or strategy from placement counts alone.
- Do not convert a small sample into statistical certainty.
- Do not hide failed runs or outliers; explain whether they reveal a rule, usability, performance, or data-quality issue.
- Do not update GDD decisions automatically. Propose the change and identify the owning document or ADR.

