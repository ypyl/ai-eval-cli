#:property TargetFramework=net10.0
#:property NoWarn=AIEVAL001
#:package Microsoft.Extensions.AI.Evaluation@10.7.0
#:package Microsoft.Extensions.AI.Evaluation.Quality@10.7.0
#:package Microsoft.Extensions.AI.Evaluation.Reporting@10.7.0
#:package Microsoft.Extensions.AI.OpenAI@10.7.0
#:package OpenAI@2.11.0

using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using OpenAI;

// ── Minimal repro: DiskBasedResultStore crashes serializing EvaluationContext
//    with multiple AIContent items (e.g. RetrievalEvaluatorContext with >1 chunk).
//
// Run: dotnet run --file repro.cs <endpoint> <model> <api-key>
//
// The evaluation itself succeeds. The crash happens when ScenarioRun.DisposeAsync()
// calls DiskBasedResultStore.WriteResultsAsync(), which hits:
//   System.NotSupportedException: JsonTypeInfo metadata for type
//   'IReadOnlyList<AIContent>' was not provided by TypeInfoResolver of type
//   'JsonUtilities+JsonContext'

if (args.Length < 3)
{
    Console.Error.WriteLine("Usage: dotnet run --file repro.cs <endpoint> <model> <api-key>");
    return 1;
}

var endpoint = args[0];
var model = args[1];
var apiKey = args[2];

var client = new OpenAIClient(
    new System.ClientModel.ApiKeyCredential(apiKey),
    new OpenAIClientOptions { Endpoint = new Uri(endpoint) });

var chatClient = client.GetChatClient(model).AsIChatClient();
var chatConfig = new ChatConfiguration(chatClient);

var evaluators = new IEvaluator[] { new RetrievalEvaluator() };

var config = DiskBasedReportingConfiguration.Create(
    storageRootPath: "./repro-results",
    evaluators: evaluators,
    chatConfiguration: chatConfig,
    enableResponseCaching: false,
    executionName: "repro");

// Scenario with 2 retrieved context chunks → RetrievalEvaluatorContext with multiple TextContent items
await using var run = await config.CreateScenarioRunAsync("repro.scenario", iterationName: "1");

var messages = new List<ChatMessage>
{
    new(ChatRole.User, "How many moons does Saturn have?")
};

var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Saturn has 146 confirmed moons."));

var context = new RetrievalEvaluatorContext(
    "Saturn has 146 confirmed moons as of 2023.",
    "Jupiter has 95 known moons.");

Console.WriteLine("Evaluating...");
var result = await run.EvaluateAsync(
    messages,
    response,
    additionalContext: [context]);

var metric = result.Metrics.Values.OfType<NumericMetric>().Single();
Console.WriteLine($"Score: {metric.Value}, Failed: {metric.Interpretation?.Failed}");

// 💥 Crash happens here — DisposeAsync serializes results to disk
Console.WriteLine("Disposing ScenarioRun (this will crash)...");
// await run.DisposeAsync(); // implicit via `await using`

Console.WriteLine("Done (should not reach here)");
return 0;
