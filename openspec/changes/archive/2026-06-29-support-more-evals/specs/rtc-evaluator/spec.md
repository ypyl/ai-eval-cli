## ADDED Requirements

### Requirement: RTC evaluator produces three metrics in one evaluation call
The system SHALL support the RelevanceTruthAndCompletenessEvaluator (`rtc`) that evaluates relevance, truth, and completeness of a response in a single LLM call, producing three distinct metrics: `Relevance (RTC)`, `Truth (RTC)`, and `Completeness (RTC)`.

#### Scenario: RTC evaluation produces all three metrics
- **WHEN** `--evaluators rtc` with a valid scenario
- **THEN** the output includes three metrics: `Relevance (RTC)`, `Truth (RTC)`, and `Completeness (RTC)`, each with a 1–5 score

#### Scenario: RTC combined with standard relevance evaluator
- **WHEN** `--evaluators relevance,rtc`
- **THEN** the output includes both `Relevance` (from RelevanceEvaluator) and `Relevance (RTC)` (from RTC evaluator) as separate metrics with distinct names

#### Scenario: RTC aggregation across multiple runs
- **WHEN** multiple same-name scenarios are evaluated with `--evaluators rtc`
- **THEN** each of the three RTC metrics is aggregated independently with its own mean, std dev, min, and max

#### Scenario: RTC evaluator with empty response
- **WHEN** a scenario has an empty or whitespace-only response and `--evaluators rtc`
- **THEN** all three RTC metrics receive diagnostic errors and are marked as failed

### Requirement: RTC evaluator is marked as experimental
The system SHALL indicate that the RTC evaluator is experimental/preview in help text and documentation with a `[preview]` tag.

#### Scenario: Help text shows preview tag for RTC
- **WHEN** user runs `eval-cli --help`
- **THEN** the evaluators list includes `rtc [preview]`

### Requirement: RTC evaluator requires no additional scenario fields
The RTC evaluator SHALL operate with only the standard scenario fields (`userQuery`, `response`, optional `systemPrompt`) and SHALL NOT require any additional context fields.

#### Scenario: RTC evaluation with minimal scenario
- **WHEN** a scenario contains only `name`, `userQuery`, and `response` with `--evaluators rtc`
- **THEN** the evaluation completes successfully and produces all three RTC metrics
