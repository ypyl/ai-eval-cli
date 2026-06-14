## ADDED Requirements

### Requirement: Engine aggregates scenarios by name
The engine SHALL provide an `Aggregate()` method that groups `ScenarioSummary` results by their `Name` property and computes per-metric descriptive statistics for each group.

#### Scenario: Identical names produce one aggregated group
- **WHEN** five scenarios all have `name: "qa.moon-distance"` with varying numeric metric values
- **THEN** `Aggregate()` returns exactly one `AggregatedScenario` with `Name = "qa.moon-distance"` and `SampleCount = 5`

#### Scenario: Mixed names produce separate groups
- **WHEN** scenarios have names `"qa.moon"`, `"qa.mars"`, and `"qa.moon"`
- **THEN** `Aggregate()` returns exactly two groups: `"qa.moon"` (n=2) and `"qa.mars"` (n=1)

#### Scenario: Single-run scenario is aggregated as n=1
- **WHEN** a scenario name appears exactly once
- **THEN** `SampleCount = 1`, `StdDev = 0`, `Min == Max == Mean`, and `FailedFraction` is 0 or 1 based on the single run

### Requirement: Metric statistics computed correctly
For each metric (e.g., relevance, coherence) within a group, the system SHALL compute mean, sample standard deviation, minimum, maximum, and failed fraction.

#### Scenario: Statistics for a metric with varied values
- **WHEN** relevance values are [3.0, 4.0, 5.0] across three runs
- **THEN** mean ≈ 4.0, std dev ≈ 1.0, min = 3.0, max = 5.0

#### Scenario: Failed fraction calculation
- **WHEN** two out of three runs have `Failed = true` for coherence
- **THEN** `FailedFraction` ≈ 0.6667

#### Scenario: Std dev with one sample
- **WHEN** a group has exactly one sample for a metric
- **THEN** `StdDev = 0`

### Requirement: AggregatedEvalResult carries both individual and aggregated data
The system SHALL provide an `AggregatedEvalResult` type containing the original `Scenarios` list AND the computed `Groups` list in a single output object.

#### Scenario: Full result object
- **WHEN** evaluation completes with `--aggregate`
- **THEN** the returned object contains both `Scenarios` (all individual `ScenarioSummary`) and `Groups` (all `AggregatedScenario` entries)

### Requirement: CLI --aggregate flag triggers aggregation
The CLI SHALL accept an `--aggregate` flag that, when present, calls `EvalEngine.Aggregate()` on the `EvalResult` after evaluation completes.

#### Scenario: --aggregate present
- **WHEN** the user runs `eval-cli --input runs.json --aggregate`
- **THEN** the engine runs evaluation, then aggregation, and the output includes aggregated data

#### Scenario: --aggregate absent (default)
- **WHEN** the user runs `eval-cli --input runs.json` without `--aggregate`
- **THEN** behavior is unchanged from current — only individual scenario results are output

### Requirement: CLI --output stats renders aggregated view
The CLI SHALL accept `--output stats` format that renders a human-readable aggregated view grouped by scenario name.

#### Scenario: Stats output format
- **WHEN** the user runs `eval-cli --input runs.json --aggregate --output stats`
- **THEN** output begins with `Scenario: <name> (n=N)` blocks, each showing per-metric lines formatted as `<Metric>: <Mean> ± <StdDev>  [<Min>–<Max>]`

#### Scenario: Stats output includes failed fraction for metrics with failures
- **WHEN** a metric group has `FailedFraction > 0`
- **THEN** the stats line appends `  (X% failed)` after the range

### Requirement: CLI --output json includes groups when aggregating
When `--aggregate` is active and `--output json` is used (the default), the JSON output SHALL include a `groups` array alongside the existing `scenarios` array.

#### Scenario: JSON output with aggregation
- **WHEN** the user runs `eval-cli --input runs.json --aggregate --output json`
- **THEN** the JSON payload contains both `"scenarios"` (array of `ScenarioSummary`) and `"groups"` (array of `AggregatedScenario`)

### Requirement: JSON source generator includes new types
The `JsonContext` source generator SHALL include `[JsonSerializable]` entries for `AggregatedEvalResult`, `AggregatedScenario`, and `MetricStats` to maintain AOT compatibility.

#### Scenario: AOT-compatible serialization
- **WHEN** the tool is published as AOT
- **THEN** serializing and deserializing `AggregatedEvalResult` succeeds without reflection fallback
