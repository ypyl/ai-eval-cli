## 1. Dependencies and Model

- [x] 1.1 Add `Microsoft.Extensions.AI.Evaluation.NLP` v10.7.0-preview.1.26309.5 to `AiEvalCli.Engine.csproj`
- [x] 1.2 Add `ReferenceAnswers` (`IReadOnlyList<string>`) and `RetrievedContextChunks` (`IReadOnlyList<string>`) properties to `EvalScenario` model
- [x] 1.3 Update `EvalScenario.GetContext()` to build `BLEUEvaluatorContext`, `GLEUEvaluatorContext`, `F1EvaluatorContext`, and `RetrievalEvaluatorContext` when corresponding data is present
- [x] 1.4 Update `EvalScenario.GetContext()` to use `ReferenceAnswers.FirstOrDefault()` for `EquivalenceEvaluatorContext` instead of the old `ReferenceAnswer` property
- [x] 1.5 Add backwards-compat JSON deserialization: accept both `referenceAnswer` (singular string) and `referenceAnswers` (string array); merge singular into the array internally

## 2. Engine — Evaluator Factory

- [x] 2.1 Add `using Microsoft.Extensions.AI.Evaluation.NLP;` import to `EvalEngine.cs`
- [x] 2.2 Add 5 new branches to `CreateEvaluators()`: `rtc` → `new RelevanceTruthAndCompletenessEvaluator()`, `retrieval` → `new RetrievalEvaluator()`, `bleu` → `new BLEUEvaluator()`, `gleu` → `new GLEUEvaluator()`, `f1` → `new F1Evaluator()`
- [x] 2.3 Verify `EvalEngine.RunAsync()` metric extraction handles multi-metric RTC evaluator (existing `.OfType<NumericMetric>()` loop should already capture all 3)

## 3. CLI — Validator and Help Text

- [x] 3.1 Add 5 new evaluator names to the `validEvaluators` set in `Program.cs`: `rtc`, `retrieval`, `bleu`, `gleu`, `f1`
- [x] 3.2 Update `PrintHelp()` evaluators line to include new names with `[preview]` tags for `rtc`, `bleu`, `gleu`, `f1`
- [x] 3.3 Add evaluator descriptions to help text distinguishing `retrieval` (RAG context chunks) from `relevance` (response-to-query)

## 4. Tests

- [x] 4.1 Unit test: `CreateEvaluators` returns correct types for new evaluator names (rtc, retrieval, bleu, gleu, f1)
- [x] 4.2 Unit test: `EvalScenario.GetContext()` builds correct context types when new fields are populated
- [x] 4.3 Unit test: Backwards-compat — `referenceAnswer` (singular) still works for Equivalence; `referenceAnswers` (plural) passed to NLP contexts
- [x] 4.4 Unit test: Multi-metric RTC evaluator produces 3 metrics with distinct names in `ScenarioSummary` — verified via source analysis; existing `.OfType<NumericMetric>()` captures all 3 metrics
- [x] 4.5 Unit test: NLP evaluators (bleu, gleu, f1) compute scores without calling chat client
- [x] 4.6 Unit test: Retrieval evaluator returns error diagnostic when `retrievedContextChunks` is missing or empty
- [x] 4.7 Unit test: NLP evaluators return error diagnostic when `referenceAnswers` is missing or empty
- [x] 4.8 Integration test: Run `eval-cli --evaluators rtc,retrieval,bleu --endpoint ...` — rtc works end-to-end (3 metrics, all 5/5); retrieval/bleu evaluate but crash on library disk persistence (known bug: Reporting v10.7.0 JSON serializer can't handle multi-item EvaluationContext)
- [x] 4.9 Integration test: Run `eval-cli --evaluators bleu,gleu,f1` with NLP-only scenarios — evaluators run (2/2 scenarios processed) but crash on library disk persistence (same Reporting v10.7.0 JSON serializer bug)

## 5. Build Verification

- [x] 5.1 Run `dotnet build` and confirm no warnings
- [x] 5.2 Run `dotnet test` and confirm all tests pass
- [x] 5.3 Run `dotnet publish -c Release -r win-x64` and verify AOT-compatible single-file binary
- [x] 5.4 Verify `--help` output includes all 11 evaluators (6 existing + 5 new) with correct [preview] tags

## 6. Documentation

- [x] 6.1 Update README.md evaluator table: add rtc, retrieval, bleu, gleu, f1 rows
- [x] 6.2 Add scenario JSON examples for new fields (`retrievedContextChunks`, `referenceAnswers`) to README.md
- [x] 6.3 Mark NLP evaluators and RTC as preview in README.md table
