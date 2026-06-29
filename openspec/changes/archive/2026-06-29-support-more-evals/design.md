## Context

`eval-cli` wraps `Microsoft.Extensions.AI.Evaluation.Quality` evaluators via a factory method `CreateEvaluators()` in `EvalEngine.cs`. Each evaluator is selected by name, instantiated with `new`, and passed to `DiskBasedReportingConfiguration`. The `EvalScenario` model has a `GetContext()` method that builds `List<EvaluationContext>` — currently `GroundednessEvaluatorContext` and `EquivalenceEvaluatorContext` with single-string values.

The library's `IEvaluator.EvaluateAsync()` receives both `IEnumerable<ChatMessage>` (conversation) and `IEnumerable<EvaluationContext>` (additional context). Different evaluators look for different context types via `.OfType<T>().FirstOrDefault()`.

The NLP package is in preview (`10.7.0-preview.1`). The RTC evaluator is in the existing Quality package but marked `[Experimental("AIEVAL001")]`.

## Goals / Non-Goals

**Goals:**
- Add 5 evaluators (Bleu, Gleu, F1, RTC, Retrieval) with minimal new concepts — follow the existing factory + context pattern
- Extend scenario JSON with `retrievedContextChunks` and `referenceAnswers` fields
- Maintain backwards compatibility: existing `referenceAnswer` (singular) still works for Equivalence
- Keep AOT compatibility — new fields use `string`/`string[]`, already supported by System.Text.Json source generator
- Mark preview evaluators in help text and documentation

**Non-Goals:**
- Agent evaluators (IntentResolution, TaskAdherence, ToolCallAccuracy) — require `AIFunctionDeclaration` + `FunctionCallContent`, need separate design
- Safety evaluators — require Azure Foundry service, entirely different execution model
- Making `--endpoint`/`--model` optional for NLP-only runs (out of scope for this change)
- Group aliases like `--evaluators all-quality` (future UX improvement)

## Decisions

### Decision 1: JSON schema — `referenceAnswers: string[]` with backwards compat

**Chosen:** Accept both `referenceAnswer` (singular string) and `referenceAnswers` (string array) on input. Map singular to first element of an internal list. EquivalenceEvaluator uses the first reference; NLP evaluators receive the full list.

**Alternatives considered:**
- Two separate fields (`referenceAnswer` + `referenceAnswers`) — confusing which evaluator uses which
- Breaking change (singular only → array only) — unnecessary churn for existing users
- New field name for NLP only (`nlpReferences`) — leaks implementation concern into user-facing schema

**Implementation:** `EvalScenario` gains `ReferenceAnswers` property. On deserialization, if `referenceAnswer` (singular) is present and `referenceAnswers` is empty/missing, populate `ReferenceAnswers` with a single-element list. `GetContext()` builds both `EquivalenceEvaluatorContext` (using `ReferenceAnswers.FirstOrDefault()`) and `NLPContext` wrappers accordingly.

### Decision 2: Context type mapping

Each evaluator expects a specific `EvaluationContext` subclass. The mapping:

| Evaluator | Context Type | JSON Field | Notes |
|-----------|-------------|------------|-------|
| Equivalence | `EquivalenceEvaluatorContext` | `referenceAnswers[0]` | Existing |
| Groundedness | `GroundednessEvaluatorContext` | `context` | Existing |
| Retrieval | `RetrievalEvaluatorContext` | `retrievedContextChunks` | **New** |
| BLEU | `BLEUEvaluatorContext` | `referenceAnswers` | **New** — needs `References` (string list) |
| GLEU | `GLEUEvaluatorContext` | `referenceAnswers` | **New** — needs `References` (string list) |
| F1 | `F1EvaluatorContext` | `referenceAnswers` | **New** — needs `References` (string list) |
| RTC | (none) | (none) | No additional context needed |

**Chosen:** Extend `GetContext()` to build the appropriate context type for each selected evaluator. Contexts are only added when the corresponding evaluator is active AND the required data is present in the scenario.

**Why not conditional contexts:** The library's `EvaluateAsync` passes all contexts to all evaluators — each evaluator ignores irrelevant ones. So we can safely build all applicable contexts for a scenario; evaluators that don't need them simply skip them.

### Decision 3: NLP evaluators and ChatConfiguration

**Problem:** NLP evaluators don't use the LLM at all (they're pure CPU computation). But `DiskBasedReportingConfiguration.Create()` requires a `ChatConfiguration` parameter.

**Chosen:** Continue requiring `ChatConfiguration` for the `ReportingConfiguration`. The library handles evaluators that don't use it gracefully — `BLEUEvaluator.EvaluateAsync()` ignores `chatConfiguration` entirely. No changes needed on our side.

**Implication for users:** Running `--evaluators bleu,gleu,f1` still requires `--endpoint` and `--model` (used to construct ChatConfiguration). The LLM is never called for these evaluators, but the configuration must be valid. Making endpoint/model optional for NLP-only runs is deferred to a future change.

### Decision 4: CLI flag names

| Evaluator | Flag | Rationale |
|-----------|------|-----------|
| RelevanceTruthAndCompletenessEvaluator | `rtc` | Short, distinct from `relevance`; documented in help text |
| RetrievalEvaluator | `retrieval` | Natural name, distinct from `relevance` |
| BLEUEvaluator | `bleu` | Standard acronym, lowercase |
| GLEUEvaluator | `gleu` | Standard acronym, lowercase |
| F1Evaluator | `f1` | Standard name |

**Alternatives considered:**
- `relevancetruthcompleteness` for RTC — too verbose, poor UX
- `retrieval-quality` — redundant, "retrieval" is the metric name in the library

### Decision 5: Preview marking

**Chosen:** Tag NLP evaluators and RTC as `[preview]` in help text. Implementation: append `[preview]` to the evaluator list in help text and add a note that preview evaluators use experimental/not-yet-stable APIs.

**Why:** The NLP package is versioned as preview (`10.7.0-preview.1`). RTC is marked `[Experimental("AIEVAL001")]`. Users should know these may change. If/when packages go stable, remove the tags.

### Decision 6: Multi-metric handling (RTC)

**Problem:** `RelevanceTruthAndCompletenessEvaluator` returns 3 metrics from one call: `Relevance (RTC)`, `Truth (RTC)`, `Completeness (RTC)`. The existing metric extraction in `EvalEngine.RunAsync()` iterates `result.Metrics.Values.OfType<NumericMetric>()` — which already handles multiple metrics per evaluator.

**Chosen:** No code change needed for multi-metric support. The existing `ToDictionary()` call naturally captures all 3 metrics. Aggregation also works — `Aggregate()` groups by metric name across scenarios, so each RTC sub-metric is aggregated independently.

**Verified:** The current code does not assume 1 evaluator = 1 metric. It iterates `result.Metrics.Values`, which is a flat collection of all metrics regardless of which evaluator produced them.

## Risks / Trade-offs

| Risk | Impact | Mitigation |
|------|--------|------------|
| **NLP package is preview** — API may change between versions | Breaking at NuGet update | Pin to specific preview version (`10.7.0-preview.1.26309.5`). Document as `[preview]`. |
| **RTC evaluator is experimental** — metric semantics may change | Scores shift between library versions | Document with `[preview]` tag. RTC metrics have distinct names (`Relevance (RTC)`) so won't be confused with existing `Relevance`. |
| **Multiple reference answers for different evaluators** — user confusion about which evaluator uses which references | Users provide `referenceAnswers` expecting Equivalence to use all of them | Equivalence only uses the first. Document clearly: "Equivalence uses the first reference answer; NLP evaluators (BLEU, GLEU, F1) use all reference answers." |
| **NLP evaluators produce 0.0–1.0 scores, quality evaluators produce 1–5** — aggregation math works but interpretation differs | Users may misinterpret low NLP scores as "bad" when 0.7 is actually good | Output the numeric value as-is. The `Failed` flag per metric (already present) correctly reflects each evaluator's own pass/fail threshold. |

## Open Questions

- Should `--endpoint`/`--model` become optional when only NLP evaluators are selected? Deferred to future UX change.
- Should we add a `--evaluators all` or group shortcuts? Deferred. Users can specify the full comma-separated list.
