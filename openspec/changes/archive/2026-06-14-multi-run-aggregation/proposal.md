## Why

LLMs are non-deterministic — a single response evaluated once doesn't reveal reliability. Users need to submit the same prompt as multiple scenarios (same `name`, different `response`) and see aggregated statistics (mean, std dev, min/max, failed fraction) per scenario group. The library (`Microsoft.Extensions.AI.Evaluation`) evaluates one response at a time with no built-in aggregation, so the tool must add this layer itself.

## What Changes

- **Engine**: Add `AggregatedScenario` and `MetricStats` types to `Models.cs`, plus an `EvalEngine.Aggregate()` method that groups `ScenarioSummary` results by name and computes per-metric descriptive statistics (mean, std dev, min, max, failed fraction).
- **Engine**: Add `AggregatedEvalResult` type that carries both individual scenario summaries and aggregated groups in one output.
- **CLI**: Add `--aggregate` flag to enable post-evaluation grouping, and new `--output stats` format to render a human-readable aggregated view with `Scenario: <name> (n=N)` blocks showing per-metric stats with `±` notation.
- **CLI**: `--output json` includes both `scenarios` (individual runs) and `groups` (aggregated stats) when `--aggregate` is active.
- **CLI**: Add `--aggregate` to `Args.cs` and wire it into `Program.cs` processing.

## Capabilities

### New Capabilities

- `multi-run-aggregation`: Group evaluation results by scenario name, compute per-metric descriptive statistics (mean, std dev, min, max, failed fraction), and expose both aggregated and individual views through new CLI flags and output formats.

### Modified Capabilities

<!-- None — this is a greenfield addition. -->

## Impact

- **Affected code**: `Models.cs` (new types), `EvalEngine.cs` (new `Aggregate()` method), `Program.cs` (new output logic and flag wiring), `Args.cs` (new `--aggregate` flag).
- **APIs**: `EvalResult` remains unchanged; `AggregatedEvalResult` is a new return type from an optional `Aggregate()` call. No breaking changes.
- **JSON serialization**: `JsonContext` source generator needs new `[JsonSerializable]` entries for `AggregatedEvalResult`, `AggregatedScenario`, `MetricStats`.
- **Dependencies**: No new packages required — aggregation is pure math on existing numeric results.
