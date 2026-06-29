## ADDED Requirements

### Requirement: NLP evaluators compare responses to reference answers
The system SHALL support BLEU, GLEU, and F1 evaluators that compare an LLM response to one or more reference answers using traditional NLP metrics without requiring an LLM call.

#### Scenario: BLEU evaluation with multiple reference answers
- **WHEN** a scenario includes `referenceAnswers: ["Saturn has 146 moons.", "Saturn, with 146 confirmed moons."]` and `--evaluators bleu`
- **THEN** the system computes a BLEU score between 0.0 and 1.0 by comparing the response against all reference answers, and the score is included in the output

#### Scenario: GLEU evaluation
- **WHEN** a scenario includes `referenceAnswers` and `--evaluators gleu`
- **THEN** the system computes a GLEU score and includes it in the output

#### Scenario: F1 evaluation
- **WHEN** a scenario includes `referenceAnswers` and `--evaluators f1`
- **THEN** the system computes an F1 score and includes it in the output

#### Scenario: NLP evaluator without reference answers
- **WHEN** a scenario lacks `referenceAnswers` and an NLP evaluator is selected
- **THEN** the evaluator returns a diagnostic error indicating missing reference answers, and the metric is marked as failed

#### Scenario: NLP evaluator with empty reference answers array
- **WHEN** a scenario has `referenceAnswers: []` and an NLP evaluator is selected
- **THEN** the evaluator returns a diagnostic error indicating no references were provided

#### Scenario: Combined NLP and quality evaluators in one run
- **WHEN** `--evaluators bleu,relevance,coherence` with scenarios containing both `referenceAnswers` and standard fields
- **THEN** all evaluators run on each scenario; NLP evaluators compute scores without LLM calls, quality evaluators use the LLM as normal

### Requirement: NLP evaluators are marked as preview
The system SHALL indicate that NLP evaluators (BLEU, GLEU, F1) are in preview status in help text and documentation, with a `[preview]` tag.

#### Scenario: Help text shows preview tag for NLP evaluators
- **WHEN** user runs `eval-cli --help`
- **THEN** the evaluators list includes `bleu [preview]`, `gleu [preview]`, `f1 [preview]`

#### Scenario: Unknown evaluator error does not suggest preview status
- **WHEN** user provides an unknown evaluator name
- **THEN** the error message lists all valid evaluator names including preview-tagged ones, so the user sees what's available

### Requirement: NLP evaluators produce scores on a 0.0–1.0 scale
The system SHALL report NLP evaluator scores as-is on the 0.0–1.0 scale, distinct from the 1–5 scale used by quality evaluators.

#### Scenario: BLEU score appears in JSON output
- **WHEN** `--evaluators bleu` with `--json`
- **THEN** the `metrics` for the scenario includes `"BLEU": { "value": 0.85, ... }` — the raw 0.0–1.0 value

#### Scenario: BLEU score appears in human-readable output
- **WHEN** `--evaluators bleu` with single scenario
- **THEN** the output includes `✅ BLEU: 0.85` with pass/fail based on the built-in interpretation threshold

#### Scenario: Aggregated NLP scores with statistics
- **WHEN** multiple same-name scenarios are evaluated with `--evaluators bleu`
- **THEN** the aggregated output includes `✅ BLEU: 0.82 ± 0.10 [0.70–0.95]` with mean, std dev, min, max computed as normal
