## ADDED Requirements

### Requirement: PRESENTATION.md exists at repo root
The project SHALL contain a `PRESENTATION.md` file at the repository root that serves as slide-oriented documentation with real execution output.

#### Scenario: File is present
- **WHEN** the repository is cloned
- **THEN** `PRESENTATION.md` exists in the root directory

### Requirement: Slide structure uses ## headings
Each `##` heading in `PRESENTATION.md` SHALL represent one presentation slide. Body content under the heading SHALL serve as speaker notes.

#### Scenario: Slide boundaries
- **WHEN** the file is parsed by a slide tool (Marp, Deckset, Pandoc)
- **THEN** each `##` section renders as a separate slide with body text as notes

### Requirement: Single-run evaluation slide with real output
The presentation SHALL include a slide demonstrating single-scenario evaluation using actual `eval-cli` output captured from a real run.

#### Scenario: Single-run slide content
- **WHEN** the single-run slide is viewed
- **THEN** it contains the exact command used and its stdout output, showing individual scenario metrics with ratings

### Requirement: Multi-run aggregation slide with real output
The presentation SHALL include a slide demonstrating multi-run aggregation using actual `eval-cli --output stats` output captured from a real run.

#### Scenario: Multi-run slide content
- **WHEN** the multi-run slide is viewed
- **THEN** it contains the exact command used and its stdout output, showing `Scenario: <name> (n=N)` blocks with mean ± std dev per metric

### Requirement: Results folder layout slide
The presentation SHALL include a slide showing the disk layout produced after evaluation, with the `results/` and `cache/` directory structure.

#### Scenario: Folder layout slide
- **WHEN** the folder layout slide is viewed
- **THEN** it shows the tree structure of `eval-results/results/{executionName}/{scenarioName}/` with `{iteration}.json` and `_stats.json` files

### Requirement: CI integration slide
The presentation SHALL include a slide demonstrating how eval-cli fits into a CI pipeline, with a minimal but functional example.

#### Scenario: CI slide content
- **WHEN** the CI slide is viewed
- **THEN** it contains a CI configuration snippet (GitHub Actions or Azure DevOps) showing eval-cli invoked with `--input` and `--output`
