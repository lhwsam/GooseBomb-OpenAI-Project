---
name: bombswap-webgl-verify
description: Validate BombSwap compilation, tests, WebGL build output, and browser behavior for release readiness or regression checks. Use when asked to verify a feature, run Fast/Full/Web checks, build WebGL, inspect browser input or loading, assess WebGL compatibility or performance, or validate package and Unity migrations; do not use to implement unrelated gameplay or author content.
---

# BombSwap WebGL Verify

Run the strongest applicable verification tier and report evidence without upgrading partial checks into a full pass.

## Select a Tier

- `StaticOnly`: use `./Tools/Verify.ps1 -StaticOnly` only to validate harness and repository structure while Unity is unavailable.
- `Fast`: use `./Tools/Verify.ps1 -Tier Fast` for compilation and Core EditMode tests.
- `Full`: use `./Tools/Verify.ps1 -Tier Full` for Fast plus first-party PlayMode tests.
- `Web`: use `./Tools/Verify.ps1 -Tier Web` for Full plus a development WebGL build and browser smoke test.

Do not run Unity verification while another Unity process owns this project. Prefer a connected Editor for interactive checks; otherwise close the Editor before invoking the batch harness.

## Workflow

1. Read the root `AGENTS.md`, `Docs/Testing/VerificationHarness.md`, `Docs/WebGL/`, `Docs/Development/CurrentState.md`, and migration plan when applicable.
2. Record the Git state, Unity version, target tier, browser/OS when relevant, and any pre-existing Console or test failures.
3. Run the chosen tier. Preserve its timestamped `Artifacts/Verification/` directory, `summary.json`, Unity logs, NUnit XML, and build report.
4. For Web verification, require a successful browser smoke covering load, canvas focus, movement, bomb placement, swap, pause/resume, audio unlock, resize, and browser Console. `-SkipBrowserSmoke` is a deliberate partial build check and must be reported as partial.
5. Compare failures with the baseline and identify whether they are introduced, pre-existing, infrastructure, or unsupported-environment failures.
6. Inspect WebGL build size and warnings. Profile representative combat before making performance claims.
7. Report tier, exact passed/failed/skipped steps, artifact path, build/browser environment, and remaining risk. Update `CurrentState.md` only when the project state or known validation result changed.

## Integrity Rules

- Never report `StaticOnly` as Fast, Fast as Full, or a skipped browser smoke as Web passed.
- Do not delete logs needed to reproduce a failure.
- Do not change product code merely to make a test green until the failure is understood and the fix is within the user's request.

