## 1. Capture real output

- [ ] 1.1 Clean eval-results (`rm -rf eval-results`), then run single-run evaluation with `integration/scenarios.json`, capture default stdout
- [ ] 1.2 Clean eval-results, then run multi-run evaluation with `integration/multi-run-scenarios.json`, capture default stdout
- [ ] 1.3 Inspect the results folder structure from the multi-run test, capture the tree (`find eval-results/results -type f`)
- [ ] 1.4 Clean eval-results, then run single-run with `--json`, capture truncated JSON sample

## 2. Write PRESENTATION.md

- [ ] 2.1 Create `PRESENTATION.md` at repo root with `##` slide structure
- [ ] 2.2 Slide 1: Title / What is eval-cli (intro, supported evaluators, providers)
- [ ] 2.3 Slide 2: Single scenario evaluation (command with cleanup + captured default output)
- [ ] 2.4 Slide 3: Multi-run aggregation (command with cleanup + captured default output + interpretation table)
- [ ] 2.5 Slide 4: Results folder layout (tree diagram + explanation of each file)
- [ ] 2.6 Slide 5: CI integration (GitHub Actions snippet, copy-paste ready)

## 3. Verify

- [ ] 3.1 Verify all code blocks render correctly (no broken escaping, correct line breaks)
- [ ] 3.2 Verify the document is self-contained — commands reference files that exist in the repo
