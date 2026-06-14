## Context

The eval-cli uses `Microsoft.Extensions.AI.Evaluation` to run quality evaluators (relevance, coherence, groundedness, etc.) against LLM responses. Each scenario produces one `ScenarioSummary` with per-metric numeric scores. Currently there is no aggregation — users evaluating non-deterministic LLMs must manually group and compute statistics across runs.

The discussion doc (`multi-run-aggregation-discussion.md`) defines the desired data model, output format, and statistical measures. This design translates those notes into an implementation plan within the existing two-project structure (`AiEvalCli` CLI and `AiEvalCli.Engine` library).

## Goals / Non-Goals

**Goals:**
- Add a pure-function `Aggregate()` method to `EvalEngine` that groups `ScenarioSummary` results by name
- Compute descriptive statistics per metric: mean, sample std dev, min, max, failed fraction
- Expose aggregation through a new `AggregatedEvalResult` type
- Wire `--aggregate` CLI flag and `--output stats` format
- Zero new dependencies — aggregation is basic arithmetic

**Non-Goals:**
- No change to how evaluation runs itself (same evaluators, same parallel model)
- No change to `EvalResult` or `ScenarioSummary` — aggregation is additive
- No statistical tests (t-test, ANOVA), confidence intervals, or distribution plots
- No per-metric weighting or composite scores
- No streaming or incremental aggregation — post-hoc only

## Decisions

### Decision 1: Post-hoc aggregation in EvalEngine (not inline during evaluation)

**Choice**: Add a separate `Aggregate(EvalResult)` method that runs after all scenarios complete.

**Alternatives considered**:
- Inline aggregation during evaluation (accumulate as results arrive) — rejected because it adds threading complexity (concurrent writes to group accumulators) and couples aggregation to the evaluation loop. Post-hoc is simpler, trivially testable, and fast (O(n) over already-in-memory results).
- Aggregation in the CLI layer only — rejected because this is domain logic that belongs in the engine library, enabling reuse by future API/service consumers.

### Decision 2: New AggregatedEvalResult type (not in-place modification of EvalResult)

**Choice**: Create `AggregatedEvalResult` that wraps `EvalResult.Scenarios` plus a new `Groups` list.

**Alternatives considered**:
- Add optional `Groups` property to `EvalResult` — rejected because it muddies the existing type with conditional semantics and makes JSON serialization awkward (null vs empty).
- Return just the groups list — rejected because users may want both individual and aggregated data in the same output.

### Decision 3: Sample standard deviation (N-1 denominator)

**Choice**: Use sample std dev (`StdDev = Sqrt(Sum((x - mean)²) / (n - 1))`).

**Rationale**: These runs are samples from the LLM's response distribution; we're estimating population variance. Population std dev (N denominator) would systematically underestimate variance for small n. The difference is negligible at large n but important at n=5-10.

### Decision 4: Group by exact case-sensitive name match

**Choice**: `Aggregate()` groups by `ScenarioSummary.Name` using ordinal string comparison.

**Rationale**: Names are user-defined identifiers following dot notation (`team.feature.scenario`). Case-sensitive matching avoids ambiguity and matches the existing behavior where `Name` is used as-is for reporting.

### Decision 5: No filtering of single-run groups

**Choice**: Single-run groups (n=1) are included with std dev = 0, min = max = mean.

**Rationale**: Simplifies the data model — consumers can filter if desired. A group of n=1 is still valid data, just with no variance information.

## Risks / Trade-offs

- **Risk**: Floating-point precision in std dev for very small differences (e.g., values 4.0, 4.0, 4.0 → std dev should be exactly 0). **Mitigation**: Use double precision; test with exact-zero edge case. Accept that ~1e-15 noise from floating-point is acceptable.
- **Risk**: Failed fraction for n=0 metrics (should never happen — every evaluated scenario has at least one metric). **Mitigation**: Guard with `if (n == 0) return` in aggregate logic; metrics without any values skip stats.
- **Trade-off**: No confidence intervals or distribution visualization. This keeps the change small but users may later request box plots or CI. **Accepted** — can be added later without breaking the existing data model.

## Open Questions

- None at proposal time. The discussion doc resolved all design choices.
