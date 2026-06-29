## Issue: `DiskBasedResultStore` crashes when serializing `EvaluationContext` with multiple `AIContent` items

### Description

`DiskBasedResultStore.WriteResultsAsync()` crashes with `System.NotSupportedException` when an `EvaluationContext` contains multiple `AIContent` items in its `Contents` property (e.g., `RetrievalEvaluatorContext` with multiple chunks, `BLEUEvaluatorContext` with multiple references).

The root cause: `JsonUtilities+JsonContext` (the source-generated JSON context in `Microsoft.Extensions.AI.Evaluation.Reporting`) only registers `ScenarioRunResult`, `Dataset`, and `CacheEntry` as serializable types. When the custom `EvaluationContextConverter` tries to serialize the inherited `Contents` property (type `IReadOnlyList<AIContent>`), the source-generated context has no metadata for that type, causing the crash.

### Affected packages and versions

| Package | Version |
|---------|---------|
| `Microsoft.Extensions.AI.Evaluation.Reporting` | 10.7.0 |
| `Microsoft.Extensions.AI.Evaluation.Quality` | 10.7.0 |

### Stack trace

```
Unhandled exception. System.NotSupportedException: JsonTypeInfo metadata for type 'System.Collections.Generic.IReadOnlyList`1[Microsoft.Extensions.AI.AIContent]' was not provided by TypeInfoResolver of type '[Microsoft.Extensions.AI.Evaluation.Reporting.JsonSerialization.JsonUtilities+JsonContext, Microsoft.Extensions.AI.AIJsonUtilities+JsonContext]'. If using source generation, ensure that all root types passed to the serializer have been annotated with 'JsonSerializableAttribute', along with any types that might be serialized polymorphically. The unsupported member type is located on type 'Microsoft.Extensions.AI.Evaluation.EvaluationContext'. Path: $.EvaluationResult.Metrics.Context.

   at System.Text.Json.ThrowHelper.ThrowNotSupportedException_NoMetadataForType(...)
   at System.Text.Json.JsonSerializerOptions.GetTypeInfoInternal(...)
   at System.Text.Json.JsonSerializerOptions.GetTypeInfo(Type type)
   at Microsoft.Extensions.AI.Evaluation.Reporting.JsonSerialization.EvaluationContextConverter.Write(Utf8JsonWriter writer, EvaluationContext value, JsonSerializerOptions options)
   ...
   at Microsoft.Extensions.AI.Evaluation.Reporting.Storage.DiskBasedResultStore.WriteResultsAsync(IEnumerable`1 results, CancellationToken cancellationToken)
   at Microsoft.Extensions.AI.Evaluation.Reporting.ScenarioRun.DisposeAsync()
```

### Evaluators affected

| Evaluator | Context type | Works? |
|-----------|-------------|--------|
| `GroundednessEvaluator` | single string → single `TextContent` | ✅ |
| `EquivalenceEvaluator` | single string → single `TextContent` | ✅ |
| `RetrievalEvaluator` | multiple strings → multiple `TextContent` | ❌ crashes |
| `BLEUEvaluator` | multiple strings → multiple `TextContent` | ❌ crashes |
| `GLEUEvaluator` | multiple strings → multiple `TextContent` | ❌ crashes |
| `RelevanceTruthAndCompletenessEvaluator` | no context | ✅ |

### Repro

File-based .NET app (single `.cs` file, no `.csproj` needed). Requires .NET 10 SDK.

```bash
dotnet run --file repro.cs <openai-endpoint> <model> <api-key>
```

<details>
<summary>repro.cs (click to expand)</summary>

```csharp
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
```

</details>

### Expected behavior

`DiskBasedResultStore` should successfully serialize `EvaluationContext` objects regardless of the number of `AIContent` items in the `Contents` list, or at least provide `JsonTypeInfo` metadata for `IReadOnlyList<AIContent>` in the source-generated JSON context.

### Possible fix

Add `[JsonSerializable(typeof(IReadOnlyList<AIContent>))]` (or `[JsonSerializable(typeof(List<AIContent>))]`) to the `JsonContext` in `src/Libraries/Microsoft.Extensions.AI.Evaluation.Reporting/CSharp/JsonSerialization/JsonUtilities.cs`, or update `EvaluationContextConverter.Write()` to handle multi-item `Contents` without relying on the source-generated context.
