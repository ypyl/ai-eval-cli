## ADDED Requirements

### Requirement: Retrieval evaluator assesses RAG context relevance
The system SHALL support the RetrievalEvaluator (`retrieval`) that evaluates how well retrieved context chunks match a user query and are correctly ranked, producing a single `Retrieval` metric on a 1–5 scale.

#### Scenario: Retrieval evaluation with context chunks
- **WHEN** a scenario includes `retrievedContextChunks: ["Mars has two moons.", "Jupiter has 95 moons."]` and `--evaluators retrieval`
- **THEN** the system evaluates the response against the provided context chunks and produces a `Retrieval` metric

#### Scenario: Retrieval evaluator without context chunks
- **WHEN** a scenario lacks `retrievedContextChunks` and `--evaluators retrieval`
- **THEN** the evaluator returns a diagnostic error indicating missing context chunks, and the metric is marked as failed

#### Scenario: Retrieval evaluator with empty context chunks array
- **WHEN** a scenario has `retrievedContextChunks: []` and `--evaluators retrieval`
- **THEN** the evaluator returns a diagnostic error indicating no context chunks were provided

#### Scenario: Retrieval evaluator with single context chunk
- **WHEN** a scenario includes `retrievedContextChunks: ["Jupiter has 95 known moons."]` with a single entry
- **THEN** the retrieval evaluation completes normally, evaluating relevance and ranking (trivially, with only one chunk)

#### Scenario: Retrieval evaluation combined with groundedness
- **WHEN** `--evaluators retrieval,groundedness` with a scenario containing both `retrievedContextChunks` and `context`
- **THEN** both evaluators run independently — retrieval uses `retrievedContextChunks`, groundedness uses `context`

#### Scenario: Aggregated retrieval scores across multiple runs
- **WHEN** multiple same-name scenarios are evaluated with `--evaluators retrieval`
- **THEN** the aggregated output includes `✅ Retrieval: 4.20 ± 0.45 [3.00–5.00]`

### Requirement: Scenario JSON schema supports retrievedContextChunks
The system SHALL accept an optional `retrievedContextChunks` field in scenario JSON as an array of strings, and SHALL pass its values to the RetrievalEvaluator.

#### Scenario: JSON deserialization of retrievedContextChunks
- **WHEN** a scenario JSON includes `"retrievedContextChunks": ["chunk1", "chunk2"]`
- **THEN** the `EvalScenario.RetrievedContextChunks` property contains both strings

#### Scenario: JSON deserialization without retrievedContextChunks
- **WHEN** a scenario JSON omits `retrievedContextChunks`
- **THEN** the `EvalScenario.RetrievedContextChunks` property is an empty list

### Requirement: Retrieval evaluator documentation is clear
The system SHALL clearly distinguish the `retrieval` evaluator from the `relevance` evaluator in help text and documentation.

#### Scenario: Help text distinguishes retrieval from relevance
- **WHEN** user runs `eval-cli --help`
- **THEN** `retrieval` is listed with a description mentioning RAG context chunks, distinct from `relevance` which describes response-to-query relevance
