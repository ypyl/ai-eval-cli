## Context

The CLI currently has three output formats controlled by `--output <fmt>`: `json`, `summary`, and `stats`. Both `summary` and `stats` are human-readable views of the same underlying data — one shows individual runs with ratings, the other shows aggregated statistics. Since results are always persisted to disk (`results/` folder with per-iteration JSON and `_stats.json`), the detailed per-run view is always available on disk. Stdout should be a single, smart view that works for both single-run and multi-run scenarios.

## Goals / Non-Goals

**Goals:**
- Replace `--output <fmt>` (3 string values) with `--json` (boolean flag)
- Default stdout shows a combined human-readable view: aggregated stats when same-name scenarios exist, compact per-scenario scores otherwise
- Keep `--output-file` working with both human and JSON output
- Zero code duplication — one `FormatHuman()` function replaces `FormatSummary()` + `FormatStats()`

**Non-Goals:**
- No change to disk persistence behavior
- No change to `AggregatedEvalResult` or `EvalResult` types
- No change to `EvalEngine` — this is purely a CLI presentation change

## Decisions

### Decision 1: Combined format + `--json` flag, not three output modes

**Choice**: Default stdout is a single human-readable view. `--json` flag toggles machine output.

**Why**: Two human-readable formats (`summary` and `stats`) answer the same question ("how did my scenarios do?") from different angles. A combined view shows stats when n>1 and compact scores otherwise — no user decision needed. The `--json` flag is a clear machine/human toggle.

**Alternatives considered**:
- Keep `--output` with two values (`human`, `json`) — rejected. Adds a string parameter where a boolean suffices.
- Keep all three current formats — rejected. The explore discussion confirmed `summary` and `stats` are two lenses on the same data.

### Decision 2: Combined format structure

**Choice**: Show execution header, then per-group stats with pass/fail indicators:

```
eval-20260614T192748 — 5 scenarios, 2 groups

qa.water-boil (n=3)
  Coherence:  4.00 ± 0.00  [4.00–4.00]  ✅
  Relevance:  4.33 ± 0.58  [4.00–5.00]  ✅

qa.moon-distance (n=2)
  Relevance:  4.00 ± 0.00  [4.00–4.00]  ✅
  Coherence:  4.00 ± 0.00  [4.00–4.00]  ✅

Results saved to: ./eval-results/results/eval-20260614T192748
```

When all groups are n=1, the format omits the ± and range, showing just the score.

**Why**: This format answers both "how consistent?" (stats view) and "did it pass?" (summary view) in one glance. The ✅/❌ per metric replaces the per-run emoji from the old summary format.

**Alternatives considered**:
- Show all individual runs below the stats — rejected. Too verbose. For 5 scenarios × 5 runs = 25 lines. If users need per-run detail, the persisted JSON files have it.
- Keep the old stats format unchanged and just make it the default — rejected. Doesn't show pass/fail, which is the main thing summary users want.

### Decision 3: `--output` flag removal is breaking, no deprecation period

**Choice**: Remove `--output`/`-o` entirely. Users must switch to `--json` or no flag.

**Why**: The tool is pre-1.0 in practice (v1.1.0 was just published today). No established user base to migrate. The sooner this is simplified, the less documentation debt.

### Decision 4: `--output-file` unchanged

**Choice**: `--output-file <file>` works identically — writes the current stdout content (human or JSON) to the specified file.

**Why**: No reason to change this. It's orthogonal to the format choice.

## Risks / Trade-offs

- **Risk**: Users who relied on per-run reason text in stdout lose it → **Mitigation**: Per-run detail is on disk in `{iteration}.json`. Users who need reasons can `cat` the files or use `--json` + `jq`.
- **Risk**: Breaking change for scripts using `-o json` → **Mitigation**: Update to `--json`. Single flag change, easy to grep/replace.
- **Trade-off**: Less visual detail in stdout (no reason text) → gain cleaner, more scannable output. Accepted.

## Open Questions

- None. The explore discussion resolved all design choices.
