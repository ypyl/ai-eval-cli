using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;

namespace AiEvalCli.Engine;

/// <summary>
/// Wraps <see cref="DiskBasedResultStore"/> with a fallback for library bug #7592:
/// multi-item EvaluationContext serialization crashes the source-generated JSON serializer.
/// Falls back to reflection-based serialization with compatible converters for aieval report interop.
/// </summary>
internal sealed class FixedDiskResultStore : IEvaluationResultStore
{
    private readonly DiskBasedResultStore _inner;
    private readonly string _storageRootPath;

    private static readonly JsonSerializerOptions FallbackOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver(),
        Converters =
        {
            new TimeSpanSecondsConverter(),
            new JsonStringEnumConverter<EvaluationDiagnosticSeverity>(JsonNamingPolicy.CamelCase),
            new JsonStringEnumConverter<EvaluationRating>(JsonNamingPolicy.CamelCase)
        }
    };

    public FixedDiskResultStore(string storageRootPath)
    {
        _storageRootPath = storageRootPath;
        _inner = new DiskBasedResultStore(storageRootPath);
    }

    public async ValueTask WriteResultsAsync(
        IEnumerable<ScenarioRunResult> results,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _inner.WriteResultsAsync(results, cancellationToken);
        }
        catch (NotSupportedException)
        {
            // dotnet/extensions#7592: source-gen JSON lacks IReadOnlyList<AIContent> metadata.
            // Fall back to reflection-based System.Text.Json which handles all types.
            // Includes TimeSpan/enum converters for aieval report compatibility.
            foreach (var result in results)
            {
                var path = GetResultPath(result);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                var json = JsonSerializer.Serialize(result, FallbackOptions);
                await File.WriteAllTextAsync(path, json, cancellationToken);
            }
        }
    }

    public async IAsyncEnumerable<ScenarioRunResult> ReadResultsAsync(
        string? executionName = null,
        string? scenarioName = null,
        string? iterationName = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var r in _inner.ReadResultsAsync(executionName, scenarioName, iterationName, cancellationToken))
            yield return r;
    }

    public async ValueTask DeleteResultsAsync(
        string? executionName = null,
        string? scenarioName = null,
        string? iterationName = null,
        CancellationToken cancellationToken = default)
        => await _inner.DeleteResultsAsync(executionName, scenarioName, iterationName, cancellationToken);

    public async IAsyncEnumerable<string> GetLatestExecutionNamesAsync(
        int? maxCount = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var name in _inner.GetLatestExecutionNamesAsync(maxCount, cancellationToken))
            yield return name;
    }

    public async IAsyncEnumerable<string> GetScenarioNamesAsync(
        string executionName,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var name in _inner.GetScenarioNamesAsync(executionName, cancellationToken))
            yield return name;
    }

    public async IAsyncEnumerable<string> GetIterationNamesAsync(
        string executionName,
        string scenarioName,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var name in _inner.GetIterationNamesAsync(executionName, scenarioName, cancellationToken))
            yield return name;
    }

    private string GetResultPath(ScenarioRunResult result)
    {
        return Path.Combine(
            _storageRootPath,
            "results",
            result.ExecutionName ?? "default",
            result.ScenarioName ?? "unknown",
            $"{result.IterationName ?? "1"}.json");
    }
}

/// <summary>
/// Replicates the library's internal TimeSpanConverter: serializes <see cref="TimeSpan"/> as total seconds (double).
/// Required for aieval report interop.
/// </summary>
internal sealed class TimeSpanSecondsConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => TimeSpan.FromSeconds(reader.GetDouble());

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value.TotalSeconds);
}
