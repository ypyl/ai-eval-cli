## Why

Three `--output` values (`json`, `summary`, `stats`) fragment what should be one view. `summary` shows per-run detail, `stats` shows aggregates — but they're two lenses on the same data. Since results are always persisted to disk with full detail, stdout should be a single, smart human-readable view that works for both single-run and multi-run scenarios. A `--json` boolean flag covers machine consumers. Three string values → two modes.

## What Changes

- **BREAKING**: Replace `--output <fmt>` (string: json/summary/stats) with `--json` (boolean flag)
- Default stdout becomes a single combined format: aggregated stats when same-name scenarios exist, compact per-scenario scores otherwise
- Remove `FormatSummary()` and `FormatStats()` functions; replace with a single `FormatHuman()` function
- `--output-file` behavior unchanged — works with both human-readable and JSON output
- Update `Args.cs`: remove `OutputFormat` string property, add `OutputJson` boolean; remove `--output`/`-o` parsing, add `--json` flag parsing

## Capabilities

### Modified Capabilities
- `multi-run-aggregation`: Requirements for `--output stats` and `--output json` change — the stats view is now always-on as part of the default human-readable output, and JSON output is toggled with `--json` instead of `--output json`

## Impact

- **CLI** (`Args.cs`): Replace `OutputFormat` string with `OutputJson` bool; swap `--output`/`-o` for `--json`
- **CLI** (`Program.cs`): Replace two format functions with one `FormatHuman()`; simplify output routing to two branches (human vs JSON)
- **Help text**: Remove `--output` documentation, add `--json` flag
- **README**: Update output examples and usage section
- **Integration examples**: Update to use `--json` flag instead of `-o json`
