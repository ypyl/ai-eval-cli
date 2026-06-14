# multi-run-aggregation

## Requirements

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
- **WHEN** evaluation completes
- **THEN** the returned object contains both `Scenarios` (all individual `ScenarioSummary`) and `Groups` (all `AggregatedScenario` entries)

### Requirement: CLI --output stats renders aggregated view
The CLI SHALL accept `--output stats` format that renders a human-readable aggregated view grouped by scenario name.

#### Scenario: Stats output format
- **WHEN** the user runs `eval-cli --input runs.json --output stats`
- **THEN** output begins with `Scenario: <name> (n=N)` blocks, each showing per-metric lines formatted as `<Metric>: <Mean> ± <StdDev>  [<Min>–<Max>]`

#### Scenario: Stats output includes failed fraction for metrics with failures
- **WHEN** a metric group has `FailedFraction > 0`
- **THEN** the stats line appends `  (X% failed)` after the range

### Requirement: CLI --output json includes groups
The `--output json` output (default) SHALL include a `groups` array alongside the `scenarios` array.

#### Scenario: JSON output includes groups
- **WHEN** the user runs `eval-cli --input runs.json --output json`
- **THEN** the JSON payload contains both `"scenarios"` (array of `ScenarioSummary`) and `"groups"` (array of `AggregatedScenario`)

### Requirement: JSON source generator includes new types
The `JsonContext` source generator SHALL include `[JsonSerializable]` entries for `AggregatedEvalResult`, `AggregatedScenario`, and `MetricStats` to maintain AOT compatibility.

#### Scenario: AOT-compatible serialization
- **WHEN** the tool is published as AOT
- **THEN** serializing and deserializing `AggregatedEvalResult` succeeds without reflection fallback

### Requirement: Engine persists results to disk after evaluation
The engine SHALL persist evaluation results to disk in a structured folder layout under `{storageRoot}/results/{executionName}/` after aggregation completes.

#### Scenario: Results are persisted automatically
- **WHEN** evaluation and aggregation complete successfully
- **THEN** result files exist on disk under `{storageRoot}/results/{executionName}/`

#### Scenario: Persistence does not require CLI flags
- **WHEN** the user runs `eval-cli --input runs.json` with no output-related flags
- **THEN** results are still persisted to disk in addition to stdout output

### Requirement: Results folder follows execution/scenario hierarchy
The persisted results SHALL follow the folder layout `{storageRoot}/results/{executionName}/{scenarioName}/` where `executionName` is the run identifier and `scenarioName` is the evaluated scenario name.

#### Scenario: Folder hierarchy for multiple scenarios
- **WHEN** an evaluation runs two scenarios named `"qa.water-boil"` and `"qa.moon-distance"` with execution name `"eval-20260614T192748"`
- **THEN** the folder structure is:
  ```
  {storageRoot}/results/eval-20260614T192748/qa.water-boil/
  {storageRoot}/results/eval-20260614T192748/qa.moon-distance/
  ```

#### Scenario: Scenario name with dots creates nested folder
- **WHEN** a scenario is named `"team.feature.test"`
- **THEN** the scenario folder is named `team.feature.test` (dots are preserved, not converted to path separators)

### Requirement: Each scenario folder contains iteration files and stats
Each scenario folder SHALL contain one JSON file per evaluation iteration named `{iteration}.json` AND a `_stats.json` file with aggregated statistics for that scenario.

#### Scenario: Scenario with three iterations
- **WHEN** a scenario `"qa.water-boil"` is evaluated three times (three input entries with the same name)
- **THEN** the scenario folder contains `1.json`, `2.json`, `3.json`, and `_stats.json`

#### Scenario: Iteration JSON contains scenario summary
- **WHEN** `1.json` is read
- **THEN** it contains a `ScenarioSummary` object with `name` and `metrics` (each metric having `value`, `rating`, `failed`, `reason`)

#### Scenario: Stats JSON contains aggregated statistics
- **WHEN** `_stats.json` is read
- **THEN** it contains an `AggregatedScenario` object with `name`, `sampleCount`, and `metrics` (each metric having `mean`, `stdDev`, `min`, `max`, `failedFraction`)

#### Scenario: Single-iteration scenario has stats with n=1
- **WHEN** a scenario has exactly one iteration
- **THEN** `_stats.json` shows `sampleCount: 1`, `stdDev: 0`, and `min == max == mean` for each metric
