## 1. CLI — replace --output with --json

- [x] 1.1 In `Args.cs`: remove `OutputFormat` string property, add `OutputJson` boolean property (default `false`)
- [x] 1.2 In `Args.cs`: remove `--output`/`-o` case from switch; add `--json` case (boolean flag, no value)
- [x] 1.3 In `Args.cs`: add `--output`/`-o` to switch with error message directing users to `--json`

## 2. CLI — replace format functions with single FormatHuman

- [x] 2.1 Remove `FormatSummary()` function from `Program.cs`
- [x] 2.2 Remove `FormatStats()` function from `Program.cs`
- [x] 2.3 Add `FormatHuman(AggregatedEvalResult result)` function — per-group stats with ✅/❌, header showing execution name and counts

## 3. CLI — simplify output routing

- [x] 3.1 Replace `cli.OutputFormat switch` block with `cli.OutputJson` boolean branch (human vs JSON)
- [x] 3.2 Update `PrintHelp()`: remove `--output` line, add `--json` line, update multi-run example
- [x] 3.3 Update help text to remove `--output` from Azure DevOps and other CI examples

## 4. Documentation

- [x] 4.1 Update README: replace `--output` references with `--json` flag, update output examples to show default human format
- [x] 4.2 Update integration scripts (`run-test.ps1`, `run-test.sh`): replace `--output summary` with no output flag

## 5. Build and verify

- [x] 5.1 Run `dotnet build` to confirm compilation
- [x] 5.2 Test default output (no flags) with multi-run input — verify combined format
- [x] 5.3 Test `--json` flag — verify JSON includes `scenarios` and `groups`
- [x] 5.4 Test `--output summary` produces helpful error message
- [x] 5.5 Test `--output-file` works with both default and `--json` modes
- [x] 5.6 Test single-run input produces clean output without ± and range clutter
