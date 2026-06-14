## Context

The project has no presentation or demo material. Users discover the tool through the README, but there's no slide-ready showcase demonstrating live behavior. A `PRESENTATION.md` file serves as a self-verifying demo — each section maps to a slide, and real command output is embedded so the document stays truthful to the tool's actual capabilities.

## Goals / Non-Goals

**Goals:**
- Create `PRESENTATION.md` at the repo root with slide-oriented structure (`##` headings = slides)
- Include real commands and captured output for single-run and multi-run evaluation
- Cover: tool overview, single scenario, multi-run aggregation, results folder layout, CI integration
- Make it self-contained — someone clones the repo, follows the commands, gets the same output

**Non-Goals:**
- No code changes
- No build system or CI changes
- No automated slide generation (e.g., Marp, reveal.js) — just Markdown structure

## Decisions

### Decision 1: `##` = slide boundary, body = speaker notes

Each `##` heading becomes a presentation slide. Paragraphs under it are speaker notes. Code blocks are slide content (what the audience sees). This is the simplest mapping that works with most slide tools (Marp, Deckset, Pandoc → PowerPoint).

### Decision 2: Real output, not fabricated examples

All code blocks showing eval-cli output SHALL be captured from actual runs using the integration test scenarios. This guarantees the document never lies about what the tool produces. The implementation step involves running the tool and pasting the exact stdout.

### Decision 3: Two scenario sets — single and multi

Single-run demo uses `integration/scenarios.json` (5 varied scenarios). Multi-run demo uses `integration/multi-run-scenarios.json` (qa.water-boil ×3, qa.moon-distance ×2). Both exist and have been validated.

### Decision 4: Slide order follows a narrative arc

1. What is eval-cli (title/intro)
2. Single scenario evaluation (the core workflow)
3. Multi-run aggregation (why stats matter)
4. The results folder (what's on disk)
5. CI integration (bringing it into pipelines)

## Risks / Trade-offs

- **Risk**: Output becomes stale as tool evolves → **Mitigation**: Run commands during implementation to capture fresh output; document can be regenerated anytime.
- **Risk**: Long output blocks make the file hard to read → **Mitigation**: Use collapsible `<details>` or trim non-essential output. Keep only what the audience needs to see.

## Open Questions

- None. The structure is clear from the user's description.
