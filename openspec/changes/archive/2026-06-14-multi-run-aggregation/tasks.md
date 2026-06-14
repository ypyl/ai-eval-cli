## 1. Data model — new types

- [x] 1.1 Add `MetricStats` record to `Models.cs`: `Mean`, `StdDev`, `Min`, `Max`, `FailedFraction` (all `double`)
- [x] 1.2 Add `AggregatedScenario` record to `Models.cs`: `Name` (string), `SampleCount` (int), `Metrics` (Dictionary<string, MetricStats>)
- [x] 1.3 Add `AggregatedEvalResult` class to `Models.cs`: `ExecutionName`, `CompletedAt`, `Scenarios` (List<ScenarioSummary>), `Groups` (List<AggregatedScenario>)

## 2. Engine — aggregation logic

- [x] 2.1 Add `Aggregate(EvalResult result)` static method to `EvalEngine`: groups `ScenarioSummary` by `Name`, computes per-metric mean, sample std dev, min, max, and failed fraction
- [x] 2.2 Handle edge cases: single-sample groups (std dev = 0, min = max = mean), empty groups, empty metrics
- [x] 2.3 Return `AggregatedEvalResult` containing both `result.Scenarios` and the computed groups

## 3. CLI — argument parsing

- [x] 3.1 Add `bool Aggregate` property to `Args.cs` (default `false`)
- [x] 3.2 Parse `--aggregate` flag in `Args.Parse()` switch statement (no value required)

## 4. CLI — program wiring and output

- [x] 4.1 In `Program.cs`, when `--aggregate` is present, call `EvalEngine.Aggregate(result)` after evaluation
- [x] 4.2 Implement `FormatStats(AggregatedEvalResult)` method: renders `Scenario: <name> (n=N)` blocks with per-metric `<Name>: <Mean> ± <StdDev>  [<Min>–<Max>]` lines; append `(X% failed)` when `FailedFraction > 0`
- [x] 4.3 Route `--output stats` to `FormatStats()` when aggregating; route `--output json` (default) to serialize `AggregatedEvalResult`
- [x] 4.4 Update `--output` validation: accept `"stats"` as a valid format (alongside `"json"` and `"summary"`); `"stats"` without `--aggregate` is an error

## 5. CLI — JSON source generator

- [x] 5.1 Add `[JsonSerializable(typeof(AggregatedEvalResult))]` to `JsonContext`
- [x] 5.2 Add `[JsonSerializable(typeof(AggregatedScenario))]` to `JsonContext`
- [x] 5.3 Add `[JsonSerializable(typeof(MetricStats))]` to `JsonContext`

## 6. CLI — help and documentation

- [x] 6.1 Add `--aggregate` and `--output stats` to `PrintHelp()` text
- [x] 6.2 Update usage examples in help to show `--aggregate --output stats` pattern

## 7. Build and verify

- [x] 7.1 Run `dotnet build` to confirm compilation with no errors
- [x] 7.2 Manually verify with a small multi-run input: confirm stats output format, JSON output includes both scenarios and groups, and single-run groups show std dev = 0
