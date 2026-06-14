## REMOVED Requirements

### Requirement: CLI --output stats renders aggregated view
**Reason**: Stats view is now part of the default human-readable output — always shown, no flag needed. The combined format shows mean ± std dev when n>1 and compact scores when n=1.
**Migration**: Remove `--output stats` from scripts. Run `eval-cli` with no output flag — the stats view is now the default.

### Requirement: CLI --output json includes groups
**Reason**: JSON output is now toggled with the `--json` boolean flag instead of `--output json`. The `groups` array is always included.
**Migration**: Replace `--output json` or `-o json` with `--json`.

## ADDED Requirements

### Requirement: Default stdout shows combined human-readable view
The default stdout output SHALL show a combined human-readable view that includes aggregated statistics (mean ± std dev, min–max) per group when same-name scenarios exist, and compact per-scenario scores with pass/fail indicators.

#### Scenario: Combined view for multi-run input
- **WHEN** the user runs `eval-cli --input multi-runs.json` with no output flag
- **THEN** stdout shows `Scenario: <name> (n=N)` blocks with per-metric lines formatted as `<Metric>: <Mean> ± <StdDev>  [<Min>–<Max>]  ✅/❌`

#### Scenario: Combined view for single-run input
- **WHEN** the user runs `eval-cli --input scenarios.json` with no output flag and all scenarios have unique names
- **THEN** stdout shows per-scenario scores with pass/fail indicators; no ± or range since n=1

#### Scenario: Failed fraction displayed in combined view
- **WHEN** a metric group has `FailedFraction > 0`
- **THEN** the metric line appends `  (X% failed)` after the range

### Requirement: CLI --json flag produces machine-readable output
The CLI SHALL accept a `--json` flag that, when present, outputs the full `AggregatedEvalResult` as JSON to stdout instead of the default human-readable view.

#### Scenario: JSON output with --json flag
- **WHEN** the user runs `eval-cli --input runs.json --json`
- **THEN** stdout is a JSON payload containing both `"scenarios"` and `"groups"` arrays

#### Scenario: Default output is human-readable
- **WHEN** the user runs `eval-cli --input runs.json` without `--json`
- **THEN** stdout is the combined human-readable view, not JSON

### Requirement: --output flag is removed
The CLI SHALL NOT accept `--output` or `-o` flags. These SHALL be replaced by the `--json` boolean flag for machine-readable output.

#### Scenario: --output flag produces error
- **WHEN** the user runs `eval-cli --output summary`
- **THEN** the tool prints an error indicating `--output` is no longer supported and that `--json` should be used for machine-readable output

#### Scenario: -o flag produces error
- **WHEN** the user runs `eval-cli -o json`
- **THEN** the tool prints an error indicating `-o` is no longer supported and that `--json` should be used for machine-readable output
