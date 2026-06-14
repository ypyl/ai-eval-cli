# eval-cli

Cross-platform AI evaluation CLI wrapping [Microsoft.Extensions.AI.Evaluation](https://www.nuget.org/packages/Microsoft.Extensions.AI.Evaluation). Runs quality evaluators (Relevance, Coherence, Groundedness, etc.) against LLM responses. Available as a native self-contained binary (no .NET runtime required) or as a dotnet tool for teams that already have .NET installed.

## Why

Different teams in an organization use different languages (Python, R, .NET, shell scripts). They all need to evaluate their LLM outputs, but they shouldn't each configure their own evaluator pipeline. `eval-cli` provides a single, shared evaluation tool that any language can invoke — via subprocess, stdin pipe, or JSON file.

Evaluators, caching, and storage are standardized by the tool. Teams provide scenarios; the CLI does the rest.

## Quick Start

### Install

**Option 1: Native binary** (no .NET runtime required)

Download the native binary for your platform from releases, or build from source:

```bash
# Clone and build (requires .NET 10 SDK)
git clone <repo-url> && cd ai-eval-cli
dotnet publish src/AiEvalCli -c Release -r win-x64 -o ./out   # Windows
dotnet publish src/AiEvalCli -c Release -r linux-x64 -o ./out  # Linux
dotnet publish src/AiEvalCli -c Release -r osx-arm64 -o ./out  # macOS
```

**Option 2: Dotnet tool** (requires .NET 10 SDK or runtime)

```bash
dotnet tool install --global eval-cli
```

For repo-local use with a tool manifest:

```bash
dotnet new tool-manifest
dotnet tool install eval-cli
```

### Run

**Azure OpenAI (default):**

```bash
# With DefaultAzureCredential (az login first)
echo '[{"name":"test.qa.moon","userQuery":"How far is the Moon?"}]' \
  | eval-cli --endpoint "https://my.openai.azure.com" --model "gpt-4o-mini"

# With API key
eval-cli --provider azure --endpoint "https://my.openai.azure.com" \
  --model "gpt-4o" --api-key "sk-..." --input ./scenarios.json
```

**OpenAI-compatible (DeepSeek, OpenCode, etc.):**

```bash
eval-cli --provider openai \
  --endpoint "https://opencode.ai/zen/go/v1" \
  --model "deepseek-v4-flash" \
  --api-key "sk-..." \
  --input ./scenarios.json --output summary
```

## Usage

```
eval-cli [options]

Provider options:
  --provider <name>         azure or openai (default: azure)
  --endpoint <url>          Endpoint URL (required)
  --model, -m <name>        Model name (Azure deployment or OpenAI model ID; required)
  --deployment, -d <name>   Alias for --model
  --api-key <key>           API key (required for openai; optional for azure)

Evaluation options:
  --evaluators, -e <list>   Evaluators: relevance,coherence,fluency,groundedness,completeness,equivalence
                            (default: relevance,coherence,groundedness)
  --input, -i <file>        Path to JSON file containing scenarios
  --input-json <json>       JSON string containing scenarios
  --storage, -s <path>      Storage root for results and cache (default: ./eval-results)
  --name, -n <name>         Execution name for report grouping (default: timestamp)
  --parallel, -p <n>        Max parallel evaluations (default: 4)
  --no-cache                Disable response caching
  --output, -o <fmt>        Output format: json, summary, or stats (default: json)
  --output-file <file>      Write output to file instead of stdout
  --help, -h                Show this help

If no --input or --input-json is provided, scenarios are read from stdin.
```

### Supported Providers

| Provider | Auth | Example |
|---|---|---|
| `azure` (default) | `DefaultAzureCredential` (`az login`) or `--api-key` | `--endpoint https://my.openai.azure.com --model gpt-4o-mini` |
| `openai` | `--api-key` (required) | `--provider openai --endpoint https://api.openai.com/v1 --model gpt-4o --api-key sk-...` |
| Any OpenAI-compatible | `--api-key` (required) | `--provider openai --endpoint https://opencode.ai/zen/go/v1 --model deepseek-v4-flash --api-key sk-...` |

## Scenario Format

Scenarios are provided as a JSON array:

```json
[
  {
    "name": "team.feature.scenario-name",
    "systemPrompt": "You are a helpful astronomy assistant.",
    "userQuery": "How far is the Moon from Earth at its closest point?",
    "response": "The Moon is approximately 225,623 miles from Earth at perigee.",
    "context": "The Moon's orbit is elliptical. At perigee, it is about 225,623 miles from Earth.",
    "referenceAnswer": "Approximately 225,623 miles at perigee."
  }
]
```

| Field | Required | Description |
|---|---|---|
| `name` | Yes | Unique scenario name. Use dot notation (`team.feature.scenario`) for report hierarchy. |
| `userQuery` | Yes | The prompt that produced the response. |
| `response` | Yes | The LLM response to evaluate. |
| `systemPrompt` | No | System message prepended to the conversation. |
| `context` | No | Grounding text for the `groundedness` evaluator. |
| `referenceAnswer` | No | Expected answer for the `equivalence` evaluator. |

## Multi-Run Aggregation

LLMs are non-deterministic — a single response evaluated once doesn't reveal reliability. `eval-cli` **always** groups scenarios with the same `name` and computes per-metric statistics, then persists everything to disk alongside the response cache.

### How it works

1. Provide multiple scenarios with the **same `name`** but **different `response`** values — each representing a different LLM output for the same prompt
2. The tool evaluates each individually (in parallel, using unique iteration names for disk storage)
3. Results are grouped by `name` and per-metric statistics (mean, std dev, min, max, failed fraction) are computed automatically
4. Both individual runs and stats are saved to `{storage}/results/{executionName}/{scenarioName}/`

### Folder layout

Each run produces a structured results folder:

```
eval-results/
  cache/                              ← library-managed response cache
  results/
    eval-20260614T192748/             ← execution-scoped folder
      qa.water-boil/
        1.json                        ← individual run 1
        2.json                        ← individual run 2
        3.json                        ← individual run 3
        _stats.json                   ← mean ± std across all runs
      qa.moon-distance/
        1.json
        2.json
        _stats.json
```

Each `{iteration}.json` contains a full `ScenarioSummary` (per-metric scores). `_stats.json` contains the `AggregatedScenario` (mean, std dev, min, max, failed fraction per metric).

### What the stats tell you

| Pattern | Meaning |
|---------|---------|
| High mean, low std dev | Consistently good |
| High mean, high std dev | Usually good, occasionally bad — flaky |
| Low mean, low std dev | Consistently bad — prompt is a weakness |
| Low mean, high std dev | Inconsistent — sometimes ok, mostly not |

**Std dev is arguably more important than mean.** A model with mean 4.0 ± 0.2 is more *reliable* than one with mean 4.5 ± 1.5.

### Example

```json
[
  { "name": "qa.moon-distance", "userQuery": "How far is the Moon?", "response": "About 238,855 miles on average." },
  { "name": "qa.moon-distance", "userQuery": "How far is the Moon?", "response": "The Moon is ~384,400 km from Earth." },
  { "name": "qa.moon-distance", "userQuery": "How far is the Moon?", "response": "It varies — 225k to 252k miles." },
  { "name": "qa.moon-distance", "userQuery": "How far is the Moon?", "response": "Approximately 1.3 light-seconds away." },
  { "name": "qa.moon-distance", "userQuery": "How far is the Moon?", "response": "Around 240,000 miles, give or take." }
]
```

```bash
eval-cli --input multi-runs.json --output stats
```

### Caching behavior

Response caching is **enabled by default**. Each unique response gets its own cache entry (the cache key includes response text), so re-running the same data gives consistent results. `--no-cache` is only needed when measuring the *judge LLM's* non-determinism (evaluating the same response multiple times).

### Reporting

Each same-name run gets a unique iteration name (`1`, `2`, `3`, …) in disk storage. The official `aieval` report groups them under the same scenario, showing iteration-level detail with built-in aggregation across iterations.

## Output

### JSON (default)

```json
{
  "executionName": "eval-20260604T120000",
  "completedAt": "2026-06-04T12:05:23Z",
  "scenarios": [
    {
      "name": "test.qa.moon-distance",
      "metrics": {
        "Relevance": {
          "value": 4.0,
          "rating": "Good",
          "failed": false,
          "reason": "The response directly addresses the question about the Moon's distance."
        },
        "Coherence": {
          "value": 5.0,
          "rating": "Exceptional",
          "failed": false,
          "reason": "The response is logically structured and easy to follow."
        }
      }
    }
  ],
  "groups": [
    {
      "name": "test.qa.moon-distance",
      "sampleCount": 1,
      "metrics": {
        "Relevance": { "mean": 4.0, "stdDev": 0.0, "min": 4.0, "max": 4.0, "failedFraction": 0.0 },
        "Coherence": { "mean": 5.0, "stdDev": 0.0, "min": 5.0, "max": 5.0, "failedFraction": 0.0 }
      }
    }
  ]
}
```

### Summary (`--output summary`)

```
Execution: eval-20260604T120000
Completed: 2026-06-04T12:05:23Z
Scenarios: 1

  test.qa.moon-distance
    ✅ Relevance: 4.00 (Good) — The response directly addresses the question...
    ✅ Coherence: 5.00 (Exceptional) — The response is logically structured...
```

### Stats (`--output stats`)

```
Execution: eval-20260604T120000
Completed: 2026-06-04T12:05:23Z
Scenarios: 5
Groups: 1

Scenario: qa.moon-distance (n=5)
  Relevance:    4.60 ± 0.55  [4.00–5.00]
  Coherence:    4.00 ± 0.00  [4.00–4.00]
  Fluency:      3.20 ± 0.45  [3.00–4.00]  (80% failed)
```

Each line shows: **mean ± std dev  [min–max]** with an optional failure percentage when any runs in the group failed evaluation.

## Integration Examples

### Python

```python
import subprocess, json

scenarios = [
    {
        "name": "myapp.qa.baseline",
        "userQuery": "What is the capital of France?",
        "referenceAnswer": "Paris"
    }
]

result = subprocess.run(
    ["eval-cli", "--endpoint", endpoint, "--model", model, "-o", "json"],
    input=json.dumps(scenarios),
    capture_output=True, text=True
)
scores = json.loads(result.stdout)
for s in scores["scenarios"]:
    relevance = s["metrics"]["Relevance"]["value"]
    assert relevance >= 3, f"Relevance too low: {relevance}"
```

### R

```r
library(jsonlite)

scenarios <- toJSON(list(
  list(name = "mymodel.qa.test", userQuery = "What is 2+2?", response = "4")
), auto_unbox = TRUE)

result <- system2("eval-cli",
  args = c("--endpoint", endpoint, "--model", model, "-o", "json"),
  input = scenarios, stdout = TRUE
)
scores <- fromJSON(result)
```

### GitHub Actions

```yaml
- name: Run AI Quality Evaluation
  run: |
    eval-cli \
      --endpoint "${{ secrets.AZURE_OPENAI_ENDPOINT }}" \
      --model "${{ vars.AZURE_OPENAI_DEPLOYMENT }}" \
      --input ./scenarios.json \
      --output summary
  timeout-minutes: 15
```

### Azure DevOps Pipeline

#### Classic pipeline (YAML)

```yaml
# azure-pipelines.yml
trigger:
  branches:
    include:
      - main
  paths:
    include:
      - prompts/**
      - scenarios.json

pool:
  vmImage: ubuntu-latest

variables:
  - group: ai-eval-secrets        # Variable group with AZURE_OPENAI_ENDPOINT

steps:
  - task: Bash@3
    displayName: 'Run AI Quality Evaluation'
    inputs:
      targetType: inline
      script: |
        eval-cli \
          --endpoint "$(AZURE_OPENAI_ENDPOINT)" \
          --model "$(AZURE_OPENAI_DEPLOYMENT)" \
          --input ./scenarios.json \
          --name "$(Build.BuildNumber)" \
          --output summary \
          --output-file "$(Build.ArtifactStagingDirectory)/eval-report.json"
    timeoutInMinutes: 15

  - task: PublishPipelineArtifact@1
    displayName: 'Publish evaluation report'
    inputs:
      targetPath: '$(Build.ArtifactStagingDirectory)/eval-report.json'
      artifact: 'ai-eval-report'
```

#### Using the eval-cli binary from a tool feed

If you publish `eval-cli` as a pipeline artifact or a Universal Package in Azure Artifacts, teams can consume it without building:

```yaml
# azure-pipelines.yml (consuming team)
pool:
  vmImage: ubuntu-latest

resources:
  pipelines:
    - pipeline: eval-cli-build
      source: 'eval-cli CI'         # Pipeline that builds and publishes the binary

steps:
  # Download the pre-built eval-cli binary
  - download: eval-cli-build
    artifact: eval-cli-linux-x64

  - script: |
      chmod +x $(Pipeline.Workspace)/eval-cli-build/eval-cli-linux-x64/eval-cli
      sudo mv $(Pipeline.Workspace)/eval-cli-build/eval-cli-linux-x64/eval-cli /usr/local/bin/
    displayName: 'Install eval-cli'

  - script: |
      eval-cli \
        --endpoint "$(AZURE_OPENAI_ENDPOINT)" \
        --model "$(AZURE_OPENAI_DEPLOYMENT)" \
        --input ./scenarios.json \
        --output summary
    displayName: 'Run AI evaluation'
    timeoutInMinutes: 15
```

#### PR quality gate (block merge on regressions)

```yaml
# azure-pipelines.yml — branch policy validation
steps:
  - script: |
      eval-cli \
        --endpoint "$(AZURE_OPENAI_ENDPOINT)" \
        --model "$(AZURE_OPENAI_DEPLOYMENT)" \
        --input ./scenarios.json \
        --output json \
        --output-file eval-results.json
    displayName: 'Run AI evaluation'
    timeoutInMinutes: 15

  - script: |
      # Fail if any scenario has a failed metric
      failed=$(jq '[.scenarios[].metrics[] | select(.failed == true)] | length' eval-results.json)
      if [ "$failed" -gt 0 ]; then
        echo "##vso[task.logissue type=error]${failed} evaluation metrics failed"
        jq '.scenarios[] | select(.metrics[].failed == true)' eval-results.json
        exit 1
      fi
      echo "All evaluation metrics passed."
    displayName: 'Validate evaluation results'
```

#### Build and publish eval-cli itself (your team's pipeline)

```yaml
# azure-pipelines.yml — build and publish the eval-cli binary
parameters:
  - name: enableAot
    displayName: 'Enable Native AOT'
    type: boolean
    default: false

trigger:
  branches:
    include:
      - main
  paths:
    include:
      - src/**

stages:
  - stage: Build
    displayName: 'Build eval-cli'
    jobs:
      - job: BuildMultiPlatform
        strategy:
          matrix:
            linux-x64:
              vmImage: ubuntu-latest
              rid: linux-x64
            win-x64:
              vmImage: windows-latest
              rid: win-x64
        pool:
          vmImage: $(vmImage)
        steps:
          - task: UseDotNet@2
            inputs:
              version: '10.x'

          - script: |
              dotnet publish src/AiEvalCli -c Release \
                -r $(rid) \
                ${{ if eq(parameters.enableAot, true) }}:-p:PublishAot=true \
                --output $(Build.ArtifactStagingDirectory)
            displayName: 'Publish eval-cli'

          - task: PublishPipelineArtifact@1
            inputs:
              targetPath: '$(Build.ArtifactStagingDirectory)'
              artifact: 'eval-cli-$(rid)'
```

## Evaluators

| Evaluator | Flag | Description |
|---|---|---|
| `relevance` | `-e relevance` | How relevant the response is to the query |
| `coherence` | `-e coherence` | Logical flow and orderly presentation |
| `fluency` | `-e fluency` | Grammar, vocabulary, readability |
| `groundedness` | `-e groundedness` | Alignment with provided context |
| `completeness` | `-e completeness` | Comprehensiveness and accuracy |
| `equivalence` | `-e equivalence` | Similarity to a reference answer |

All evaluators score on a 1–5 scale. Scores are mapped to ratings: `Unacceptable` → `Poor` → `Average` → `Good` → `Exceptional`.

## Caching

By default, judge LLM responses are cached on disk under `--storage`. The cache key includes the full prompt and response text, so scenarios with the same name but different responses still get cached independently. Re-running the same data gives identical results without additional LLM calls. Cache expires after 14 days. Use `--no-cache` to disable.

For shared caching across teams, point `--storage` at a shared network path or configure Azure Storage in the source.

## Reports

`eval-cli` persists evaluation results to the `--storage` directory in two locations:

- **`results/`** — JSON files written by `eval-cli`: per-iteration `{iteration}.json` and `_stats.json` per scenario folder
- **`cache/`** — response cache managed by `Microsoft.Extensions.AI.Evaluation.Reporting`

The cache directory is in the format used by the official `aieval` CLI, which can generate rich HTML reports from the same data — no extra export step needed.

Same-name scenarios are stored as iterations (`1.json`, `2.json`, …) under the same scenario directory. The official report groups them naturally under one scenario name and can show iteration-level detail or aggregate view.

```bash
# 1. Run evaluation (results automatically saved to storage path)
eval-cli --endpoint "..." --model "gpt-4o-mini" \
  --input scenarios.json --storage ./eval-results --name "baseline-$(date +%Y%m%d)"

# 2. Generate HTML report from the same storage path
dotnet tool install Microsoft.Extensions.AI.Evaluation.Console --create-manifest-if-needed
dotnet aieval report -p ./eval-results -o report.html --open
```

The `--name` flag sets the execution name, which `aieval` uses for run grouping and trend comparison. Run multiple evaluations with different names and `aieval` shows side-by-side trends.

### Generate HTML reports in CI

**Azure DevOps:**

```yaml
steps:
  - script: |
      eval-cli \
        --endpoint "$(AZURE_OPENAI_ENDPOINT)" \
        --model "$(AZURE_OPENAI_DEPLOYMENT)" \
        --input ./scenarios.json \
        --storage ./eval-results \
        --name "$(Build.BuildNumber)"
    displayName: 'Run AI evaluation'

  - script: |
      dotnet tool install Microsoft.Extensions.AI.Evaluation.Console --create-manifest-if-needed
      dotnet aieval report -p ./eval-results -o report.html
    displayName: 'Generate HTML report'

  - task: PublishPipelineArtifact@1
    inputs:
      targetPath: 'report.html'
      artifact: 'ai-eval-html-report'
```

**GitHub Actions:**

```yaml
- run: eval-cli --endpoint "${{ secrets.AZURE_OPENAI_ENDPOINT }}" --model "${{ vars.DEPLOYMENT }}" --input scenarios.json --storage ./eval-results
- run: |
    dotnet tool install Microsoft.Extensions.AI.Evaluation.Console --create-manifest-if-needed
    dotnet aieval report -p ./eval-results -o report.html
- uses: actions/upload-artifact@v4
  with:
    name: ai-eval-html-report
    path: report.html
```

## Architecture

```
src/
├── AiEvalCli.Engine/     Shared library — evaluation pipeline
│   ├── EvalEngine.cs      Core: RunAsync() + Aggregate() + PersistAsync() — parallel execution, statistical grouping, disk persistence
│   ├── Models.cs          EvalRequest, EvalScenario, EvalResult, AggregatedEvalResult types
│   └── ChatConfigurationFactory.cs  Azure OpenAI credential setup
│
└── AiEvalCli/            Console application
    ├── Program.cs         CLI entry point, JSON output formatting
    └── Args.cs            Zero-dependency argument parser
```

The engine library is designed to be reused by a future REST API service — same `EvalEngine.RunAsync()` can power both the CLI and an HTTP endpoint.

## Native AOT

The project is configured for single-file self-contained deployment by default. To enable Native AOT (faster startup, smaller binary), uncomment these lines in `src/AiEvalCli/AiEvalCli.csproj`:

```xml
<PublishAot>true</PublishAot>
<InvariantGlobalization>true</InvariantGlobalization>
```

Then publish:

```bash
dotnet publish src/AiEvalCli -c Release -r win-x64
dotnet publish src/AiEvalCli -c Release -r linux-x64
dotnet publish src/AiEvalCli -c Release -r osx-arm64
```

Note: Native AOT requires cross-compilation tooling on the build machine (C++ toolchain, clang). Cross-OS compilation is not supported — build each target on its native OS or use Docker.

## Authentication

Uses `DefaultAzureCredential` from `Azure.Identity`. Authentication methods tried in order:

1. Environment variables (`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_CLIENT_SECRET`)
2. Azure CLI (`az login`)
3. Managed Identity (in Azure-hosted environments)
4. Visual Studio / VS Code credentials

Ensure you're authenticated before running:

```bash
az login
```

## License

MIT
