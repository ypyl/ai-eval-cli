## Context

The `multi-run-aggregation` change added `EvalEngine.Aggregate()` and `AggregatedEvalResult` — pure in-memory computation and a CLI flag to opt into it. Results are only emitted to stdout or `--output-file`; nothing persists automatically to the results folder.

This design refines that change: remove the flag, always aggregate, and always persist results to disk in a structured folder layout alongside the library-managed cache. The library (`DiskBasedReportingConfiguration`) handles its own `cache/` directory — our persistence lives in a separate `results/` directory under the same storage root, avoiding conflicts.

## Goals / Non-Goals

**Goals:**
- Remove `--aggregate` CLI flag; aggregation is always-on
- Always persist evaluation results (individual runs + per-scenario stats) to disk
- Organize output as `{storageRoot}/results/{executionName}/{scenarioName}/` with `{iteration}.json` and `_stats.json`
- `--output` remains a stdout display format choice (`json`, `summary`, `stats`) — all always valid

**Non-Goals:**
- No change to the `Aggregate()` algorithm or `AggregatedEvalResult` type
- No change to `DiskBasedReportingConfiguration` or its `cache/` output
- No per-scenario folder nesting beyond one level deep
- No cross-execution comparison or historical stats — single execution only

## Decisions

### Decision 1: Remove `--aggregate` (not deprecate)

**Choice**: Delete the flag entirely from `Args.cs` and `Program.cs`.

**Alternatives considered**:
- Deprecate with a warning and no-op — rejected. The flag was just added; there are no downstream consumers to worry about. Clean removal is simpler and avoids cruft.
- Keep as hidden no-op for backward compat — rejected. Same reason: newly added, no consumers.

### Decision 2: Persist per-iteration JSON files, not a single array

**Choice**: Each iteration gets its own file: `1.json`, `2.json`, etc. using the iteration number as filename.

```
results/eval-20260614T192748/qa.water-boil/
├── 1.json          ← { name, metrics: { relevance: { value, rating, failed, reason }, ... } }
├── 2.json
├── 3.json
└── _stats.json     ← { name, sampleCount, metrics: { relevance: { mean, stdDev, min, max, failedFraction }, ... } }
```

**Alternatives considered**:
- Single `runs.json` array — rejected. Less inspectable, harder to diff individual runs, requires reading entire array to find one run. Per-file is also naturally parallel-safe (no file locking between concurrent writes).
- Include both individual runs AND stats in `_stats.json` — rejected. Redundant and bloated. `_stats.json` is the aggregated view; individual runs are already in their own files.

### Decision 3: `_stats.json` per scenario folder, not a single execution-level file

**Choice**: Each scenario folder contains its own `_stats.json`.

**Rationale**: The scenario folder is the atomic unit. A `qa.water-boil/` folder is self-contained — grab it and you have all runs plus stats for that scenario. This matches the aggregation model: stats are computed per scenario name, not across scenarios.

**Alternatives considered**:
- Single `_aggregation.json` at the execution level with all scenarios' stats — rejected. Breaks the self-contained folder model. You'd need the execution-level file to understand any single scenario.

### Decision 4: Persistence in the Engine, not the CLI

**Choice**: Add a `PersistAsync(AggregatedEvalResult, storageRoot)` method to `EvalEngine`.

**Rationale**: The engine already owns the data model and storage root concept. Keeping persistence logic in the library enables reuse by future consumers (API, service) that aren't the CLI. The CLI only calls it.

**Alternatives considered**:
- Persistence in `Program.cs` — rejected. Duplication risk for non-CLI consumers, and the CLI should be thin.

### Decision 5: Iteration numbers use the same counter as `DiskBasedReportingConfiguration`

**Choice**: Reuse the `iterationCounters` `ConcurrentDictionary` already in `RunAsync()` to determine the filename. Iteration 1 → `1.json`, iteration 2 → `2.json`.

**Rationale**: This already works correctly for same-name-scenarios and avoids a separate numbering scheme. The iteration counter is per-scenario-name, which matches the folder-per-scenario layout.

### Decision 6: File naming — `_stats.json` with underscore prefix

**Choice**: The stats file is named `_stats.json`.

**Rationale**: The underscore prefix sorts it above numbered files in directory listings, keeping it visually prominent. Alternative names considered: `stats.json` (sorts after `1.json`), `_aggregation.json` (longer, `_stats` is more concise).

## Risks / Trade-offs

- **Risk**: Disk I/O during evaluation adds latency. **Mitigation**: Writes are small JSON files (~100-500 bytes each). Write per-iteration as results complete (already parallel via `Task.WhenAll`). Stats file is a single write after aggregation. Negligible compared to LLM call latency.
- **Risk**: Existing `eval-results/` folder has only `cache/` — adding `results/` sibling is safe but users might have scripts expecting only `cache/`. **Mitigation**: New directory is additive, not destructive. Existing `cache/` is untouched.
- **Risk**: Re-running the same execution name overwrites previous run's files. **Mitigation**: This is intentional — execution name is a user-chosen identifier. Timestamp-based default names prevent accidental overlap. Document in help text.
- **Trade-off**: No `_run.json` manifest at the execution level. The filesystem is the manifest — `ls results/eval-20260614T192748/` lists all scenarios. **Accepted** — can be added later if needed without breaking the folder model.

## Open Questions

- None. The explore discussion resolved all design choices.
