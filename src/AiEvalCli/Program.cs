using System.Text.Json;
using System.Text.Json.Serialization;
using AiEvalCli;
using AiEvalCli.Engine;

// ---- Usage ----
if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
{
    PrintHelp();
    return 0;
}

var cli = Args.Parse(args);

if (cli.ShowHelp) { PrintHelp(); return 0; }

// Validate evaluators
var validEvaluators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    { "relevance", "coherence", "fluency", "groundedness", "completeness", "equivalence" };
foreach (var name in cli.Evaluators)
{
    if (!validEvaluators.Contains(name))
    {
        Console.Error.WriteLine($"Unknown evaluator '{name}'. Valid: {string.Join(", ", validEvaluators)}");
        return 1;
    }
}

// Load scenarios using source generator (AOT-safe)
List<EvalScenario> scenarios;
if (cli.InputJson is not null)
{
    scenarios = JsonSerializer.Deserialize(cli.InputJson, JsonContext.Default.ListEvalScenario)
                ?? throw new InvalidOperationException("Failed to parse --input-json");
}
else if (cli.InputFile is not null)
{
    var text = await File.ReadAllTextAsync(cli.InputFile);
    scenarios = JsonSerializer.Deserialize(text, JsonContext.Default.ListEvalScenario)
                ?? throw new InvalidOperationException($"Failed to parse {cli.InputFile}");
}
else
{
    var text = await Console.In.ReadToEndAsync();
    scenarios = string.IsNullOrWhiteSpace(text)
        ? []
        : JsonSerializer.Deserialize(text, JsonContext.Default.ListEvalScenario) ?? [];
}

if (scenarios.Count == 0)
{
    Console.Error.WriteLine("No scenarios provided. Use --input, --input-json, or pipe JSON to stdin.");
    return 1;
}

// Build chat configuration based on provider
var chatConfig = cli.Provider switch
{
    "openai" => ChatConfigurationFactory.CreateOpenAICompatible(
        cli.Endpoint!, cli.ApiKey!, cli.Model!),
    "azure" when cli.ApiKey is not null => ChatConfigurationFactory.CreateAzureOpenAIWithKey(
        cli.Endpoint!, cli.ApiKey, cli.Model!),
    _ => ChatConfigurationFactory.CreateAzureOpenAI(
        cli.Endpoint!, cli.Model!)
};

var request = new EvalRequest
{
    ExecutionName = cli.ExecutionName ?? $"eval-{DateTime.Now:yyyyMMddTHHmmss}",
    StorageRootPath = cli.StoragePath,
    EvaluatorNames = new HashSet<string>(cli.Evaluators, StringComparer.OrdinalIgnoreCase),
    ChatConfiguration = chatConfig,
    Scenarios = scenarios,
    EnableResponseCaching = !cli.NoCache,
    MaxConcurrency = cli.Parallel
};

// Run
var console = new ProgressConsole();
var result = await EvalEngine.RunAsync(request, console);

// Always aggregate and persist
var aggregated = EvalEngine.Aggregate(result);
var persistedPath = await EvalEngine.PersistAsync(aggregated, request.StorageRootPath, JsonContext.Default);
Console.WriteLine($"Results saved to: {persistedPath}");

// Output to stdout
var outputText = cli.OutputJson
    ? JsonSerializer.Serialize(aggregated, JsonContext.Default.AggregatedEvalResult)
    : FormatHuman(aggregated);

if (cli.OutputFile is not null)
    await File.WriteAllTextAsync(cli.OutputFile, outputText);
else
    Console.WriteLine(outputText);

return 0;


// ---- Argument parser ----
static void PrintHelp()
{
    Console.WriteLine("""
        eval-cli — Cross-platform AI evaluation CLI

        Usage:
          eval-cli [options]

        Provider options:
          --provider <name>         Provider type: azure or openai (default: azure)
          --endpoint <url>          Endpoint URL (required)
          --model, -m <name>        Model deployment name (Azure) or model ID (OpenAI) (required)
          --api-key <key>           API key (required for openai; optional for azure)

        Azure OpenAI examples:
          eval-cli --endpoint https://my.openai.azure.com --model gpt-4o-mini --input scenarios.json
          eval-cli --provider azure --endpoint https://my.openai.azure.com --model gpt-4o --api-key sk-... --input scenarios.json

        OpenAI-compatible examples (DeepSeek, OpenCode, etc.):
          eval-cli --provider openai --endpoint https://opencode.ai/zen/go/v1 --model deepseek-v4-flash --api-key sk-... --input scenarios.json

        Evaluation options:
          --evaluators, -e <list>   Evaluators: relevance,coherence,fluency,groundedness,completeness,equivalence
                                    (default: relevance,coherence,groundedness)
          --input, -i <file>        Path to JSON file containing scenarios
          --input-json <json>       JSON string containing scenarios (alternative to --input)
          --storage, -s <path>      Storage root path for results and cache (default: ./eval-results)
          --name, -n <name>         Execution name for report grouping (default: timestamp)
          --parallel, -p <n>        Max parallel evaluations (default: 4)
          --no-cache                Disable response caching
          --json                    Output machine-readable JSON instead of human-readable view
          --output-file <file>      Write output to file instead of stdout
          --help, -h                Show this help

        Scenario JSON format:
          [
            {
              "name": "team.feature.scenario",
              "systemPrompt": "You are a helpful assistant.",
              "userQuery": "How far is the Moon from Earth?",
              "response": "The Moon is about 238,855 miles from Earth on average.",
              "context": "The Moon is 225,623 miles at perigee...",
              "referenceAnswer": "225,623 to 252,088 miles"
            }
          ]

        Multi-run aggregation example:
          eval-cli --input multi-runs.json

        The tool only evaluates — responses must be provided in the JSON, not generated.
        """);
}


// ---- Helpers ----
static string FormatHuman(AggregatedEvalResult result)
{
    var lines = new List<string>
    {
        $"{result.ExecutionName} \u2014 {result.Scenarios.Count} scenario{(result.Scenarios.Count == 1 ? "" : "s")}, {result.Groups.Count} group{(result.Groups.Count == 1 ? "" : "s")}",
        ""
    };

    foreach (var g in result.Groups)
    {
        lines.Add($"{g.Name} (n={g.SampleCount})");
        foreach (var (metricName, stats) in g.Metrics)
        {
            var pass = stats.FailedFraction > 0 ? "\u274c" : "\u2705";
            if (g.SampleCount == 1)
            {
                lines.Add($"  {pass} {metricName}: {stats.Mean:F2}");
            }
            else
            {
                var line = $"  {pass} {metricName}: {stats.Mean:F2} \u00b1 {stats.StdDev:F2}  [{stats.Min:F2}\u2013{stats.Max:F2}]";
                if (stats.FailedFraction > 0)
                    line += $"  ({stats.FailedFraction:P0} failed)";
                lines.Add(line);
            }
        }
        lines.Add("");
    }

    return string.Join('\n', lines);
}


file class ProgressConsole : IConsoleWriter
{
    public void WriteLine(string message) => Console.WriteLine(message);
    public void WriteProgress(int completed, int total, string currentScenario)
    {
        var pct = (int)((double)completed / total * 100);
        var bar = new string('#', pct / 5).PadRight(20);
        Console.WriteLine($"  [{bar}] {completed}/{total}  {currentScenario}");
    }
}


// ---- JSON source generator for AOT compatibility ----
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(List<EvalScenario>))]
[JsonSerializable(typeof(EvalScenario))]
[JsonSerializable(typeof(EvalResult))]
[JsonSerializable(typeof(ScenarioSummary))]
[JsonSerializable(typeof(MetricSummary))]
[JsonSerializable(typeof(AggregatedEvalResult))]
[JsonSerializable(typeof(AggregatedScenario))]
[JsonSerializable(typeof(MetricStats))]
internal partial class JsonContext : JsonSerializerContext;
