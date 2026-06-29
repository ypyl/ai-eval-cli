## Why

`eval-cli` currently supports 6 of the 23 built-in evaluators in the `Microsoft.Extensions.AI.Evaluation` ecosystem. Teams evaluating RAG pipelines, comparing responses against reference answers with NLP metrics, or needing a single-call relevance/truth/completeness evaluation have no CLI support. Adding the 5 evaluators that only require JSON input (no Azure Foundry, no complex agent traces) significantly expands the tool's utility with minimal architectural change.

## What Changes

- Add `Microsoft.Extensions.AI.Evaluation.NLP` NuGet package (preview) — enables BLEU, GLEU, F1 evaluators
- Add 5 new evaluator flags: `rtc`, `retrieval`, `bleu`, `gleu`, `f1`
- Extend scenario JSON schema with two new fields:
  - `retrievedContextChunks: string[]` — for the Retrieval evaluator (RAG document chunks)
  - `referenceAnswers: string[]` — for NLP evaluators (multiple reference texts; maintains backwards compatibility with singular `referenceAnswer`)
- Support multi-metric evaluators (RTC produces 3 metrics from a single evaluator call)
- Handle NLP evaluators that require no LLM call (pure computation)
- Mark preview evaluators (NLP package, RTC) with `[preview]` tags in help text and documentation

## Capabilities

### New Capabilities

- `nlp-evaluators`: BLEU, GLEU, and F1 evaluators that compare LLM responses to reference answers using traditional NLP metrics. No LLM call required — pure on-device computation.
- `rtc-evaluator`: RelevanceTruthAndCompletenessEvaluator that produces Relevance (RTC), Truth (RTC), and Completeness (RTC) scores in a single LLM evaluation call.
- `retrieval-evaluator`: RetrievalEvaluator that evaluates RAG retrieval quality — how well retrieved context chunks match the query and are correctly ranked.

### Modified Capabilities

None. Existing capabilities (multi-run aggregation, dotnet tool distribution, automated publishing) are unchanged. The new evaluators integrate into the existing evaluation pipeline, aggregation, and reporting without altering existing behavior.

## Impact

- **NuGet packages**: Add `Microsoft.Extensions.AI.Evaluation.NLP` v10.7.0-preview.1 to `AiEvalCli.Engine.csproj`
- **Source files**: `EvalEngine.cs` (CreateEvaluators factory), `Models.cs` (EvalScenario context methods, new fields), `Program.cs` (validator, help text), `Args.cs` (no changes needed beyond existing pattern)
- **CLI flags**: 5 new valid values for `--evaluators` / `-e`; no flag removals or renames
- **JSON schema**: Two new optional fields (`retrievedContextChunks`, `referenceAnswers`); `referenceAnswer` (singular) preserved for backwards compatibility — Equivalence evaluator uses the first element of `referenceAnswers` or the singular field if provided
- **AOT compatibility**: No reflection needed — new fields use `string` and `string[]`, already supported by the source generator
- **Cross-platform**: NLP evaluators are pure .NET computation, no platform-specific code
- **Breaking changes**: None. All new fields are optional. Existing CLI flags and JSON schemas continue to work.
