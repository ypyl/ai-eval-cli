## Why

The `--aggregate` flag is unnecessary friction. Aggregation is computationally trivial (O(n) over in-memory results), always useful, and harmless in single-run scenarios. Worse, the current design treats aggregation purely as a stdout formatting concern — results never persist to disk automatically, leaving the results folder (`eval-results/`) containing only library-managed cache entries. Every run should produce a self-contained folder with both individual results and per-scenario stats, no flag required.

## What Changes

- **BREAKING**: Remove `--aggregate` CLI flag. Aggregation always runs — the engine computes stats after every evaluation.
- Always persist evaluation results to disk in a structured folder layout under `{storageRoot}/results/{executionName}/{scenarioName}/`.
- Each scenario folder contains individual run JSON files AND a `_stats.json` aggregation file.
- `--output` flag retains its role as a stdout display format choice (`json`, `summary`, `stats`) — now always available since aggregation is always on.
- **BREAKING**: Default `--output json` now produces `AggregatedEvalResult` (adds `groups` array). This is additive and should not break downstream consumers.

## Capabilities

### Modified Capabilities
- `multi-run-aggregation`: The opt-in `--aggregate` flag requirement is removed. Aggregation is now always-on. A new requirement is added for disk persistence of results (individual runs + stats) into a structured folder layout under the storage root.

## Impact

- **CLI** (`src/AiEvalCli/Args.cs`): Remove `Aggregate` property and `--aggregate` parsing; remove `--output stats` requiring `--aggregate` validation
- **CLI** (`src/AiEvalCli/Program.cs`): Always call `EvalEngine.Aggregate()`; always persist results to disk; update help text
- **Engine** (`src/AiEvalCli.Engine/EvalEngine.cs`): Add `PersistAsync()` method to write results to disk
- **Engine** (`src/AiEvalCli.Engine/Models.cs`): No changes required — existing types support the new flow
- **`--output stats`**: Always valid, no longer gated behind `--aggregate`
