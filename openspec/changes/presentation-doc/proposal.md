## Why

The tool needs a living presentation document that demonstrates its capabilities with real execution output — both single-run and multi-run scenarios. This serves as onboarding material for new users, conference talk source, and a self-verifying demo that stays current with the tool's actual behavior.

## What Changes

- Create `PRESENTATION.md` at the repo root — a slide-oriented document where each `##` section becomes a presentation slide and body content serves as speaker notes
- Include real commands and their captured output for both single-run and multi-run evaluation scenarios
- Commands include `rm -rf eval-results` cleanup before each run to ensure fresh, reproducible output
- Structure covers: what the tool is, single scenario evaluation, multi-run aggregation, the results folder layout, and CI integration

## Capabilities

### New Capabilities
- `presentation-documentation`: A PRESENTATION.md file structured for slides, containing live-run commands and output demonstrating the tool's single-run and multi-run evaluation capabilities

### Modified Capabilities
None — no existing spec requirements change.

## Impact

- **New file**: `PRESENTATION.md` in repo root
- **No code changes**: purely documentation artifact
- **Depends on**: `integration/multi-run-scenarios.json` and `integration/scenarios.json` for live-run commands
- **Requires**: `eval-results/` cleanup before each captured run to guarantee fresh output (no cached results)
