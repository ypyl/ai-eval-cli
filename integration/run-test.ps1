# PowerShell integration test script
$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectDir = Split-Path -Parent $ScriptDir
$KeyFile = Join-Path $ScriptDir "key.txt"
$ScenariosFile = Join-Path $ScriptDir "scenarios.json"

$Endpoint = if ($env:EVAL_ENDPOINT) { $env:EVAL_ENDPOINT } else { "https://opencode.ai/zen/go/v1" }
$Model    = if ($env:EVAL_MODEL)    { $env:EVAL_MODEL }    else { "deepseek-v4-pro" }

# Read API key
if (-not (Test-Path $KeyFile)) {
    Write-Error "key.txt not found at $KeyFile. Create it with your API key."
    exit 1
}
$ApiKey = (Get-Content $KeyFile -Head 1).Trim()

Write-Host "========================================="
Write-Host "  eval-cli Integration Test"
Write-Host "========================================="
Write-Host "  Endpoint : $Endpoint"
Write-Host "  Model    : $Model"
Write-Host "========================================="
Write-Host ""

dotnet run --project "$ProjectDir/src/AiEvalCli" -c Release -- `
    --provider openai `
    --endpoint $Endpoint `
    --model $Model `
    --api-key $ApiKey `
    --input $ScenariosFile `
    --evaluators relevance,coherence,fluency `
    --parallel 3 `
    --output summary
