## 1. CLI — remove `--aggregate` flag

- [x] 1.1 Remove `Aggregate` property from `Args.cs`
- [x] 1.2 Remove `--aggregate` case from `Args.Parse()` switch statement
- [x] 1.3 Remove `--output stats requires --aggregate` validation error from `Program.cs`
- [x] 1.4 Update `PrintHelp()` to remove `--aggregate` references and show `--output stats` as always available
- [x] 1.5 Update usage example in help text (remove `--aggregate` from multi-run example)

## 2. Engine — add disk persistence

- [x] 2.1 Add `PersistAsync(AggregatedEvalResult result, string storageRoot)` static method to `EvalEngine`
- [x] 2.2 In `PersistAsync`, create directory `{storageRoot}/results/{executionName}/{scenarioName}/` for each scenario name found in `Scenarios`
- [x] 2.3 Write each `ScenarioSummary` as `{iteration}.json` using iteration counter (reuse existing `iterationCounters` logic to determine iteration number)
- [x] 2.4 Write each `AggregatedScenario` as `_stats.json` in its corresponding scenario folder
- [x] 2.5 Use `JsonSerializer` with `JsonContext` source generator (AOT-safe) for all serialization

## 3. CLI — wire always-on aggregation and persistence

- [x] 3.1 Always call `EvalEngine.Aggregate(result)` after `RunAsync` (remove `if (cli.Aggregate)` branch)
- [x] 3.2 Always call `EvalEngine.PersistAsync(aggregated, storageRoot)` after aggregation
- [x] 3.3 Route `--output json` to always serialize `AggregatedEvalResult` (remove `EvalResult` serialization path)
- [x] 3.4 Route `--output stats` to always use `FormatStats` path (remove `--aggregate` gating)
- [x] 3.5 Route `--output summary` to always use `FormatSummary` path (unchanged, no aggregation needed)
- [x] 3.6 Report persisted folder path to stdout after persistence completes (e.g., "Results saved to: ...")

## 4. Build and verify

- [x] 4.1 Run `dotnet build` to confirm compilation with no errors
- [x] 4.2 Test with a multi-run input: verify folder structure, `{iteration}.json` content, `_stats.json` content
- [x] 4.3 Test with a single-run input: verify folder is created, `_stats.json` shows n=1 with std dev=0
- [x] 4.4 Test `--output stats` works without `--aggregate` flag
- [x] 4.5 Test `--output json` includes `groups` array by default
- [x] 4.6 Test AOT publish compiles successfully (verify `JsonContext` covers all new serialization paths)
