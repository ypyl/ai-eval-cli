using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;

namespace AiEvalCli.Engine;

/// <summary>
/// Central evaluation engine. Both CLI and future service call this.
/// </summary>
public static class EvalEngine
{
    /// <summary>
    /// Runs a batch evaluation against the provided scenarios.
    /// </summary>
    public static async Task<EvalResult> RunAsync(
        EvalRequest request,
        IConsoleWriter? console = null,
        CancellationToken cancellationToken = default)
    {
        var config = BuildReportingConfiguration(request);
        var scenarios = new List<ScenarioSummary>();
        var startedAt = DateTime.UtcNow;

        console?.WriteLine($"Starting evaluation: {request.ExecutionName}");
        console?.WriteLine($"Evaluators: {string.Join(", ", request.EvaluatorNames)}");
        console?.WriteLine($"Scenarios: {request.Scenarios.Count}");

        var completed = 0;
        var total = request.Scenarios.Count;

        // Run evaluation only — the response comes from the scenario, not generated here.
        var semaphore = new SemaphoreSlim(request.MaxConcurrency);
        // Per-name iteration counters so same-name scenarios get unique iteration names.
        // This avoids file-locking conflicts in DiskBasedResultStore while keeping the
        // scenario name intact in reports.
        var iterationCounters = new ConcurrentDictionary<string, int>(StringComparer.Ordinal);
        var tasks = request.Scenarios.Select(async scenario =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var iteration = iterationCounters.AddOrUpdate(scenario.Name, 1, (_, n) => n + 1);
                await using var run = await config.CreateScenarioRunAsync(scenario.Name, iterationName: iteration.ToString());

                var messages = scenario.GetChatMessages();
                var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, scenario.Response));

                var result = await run.EvaluateAsync(
                    messages,
                    response,
                    additionalContext: scenario.GetContext(),
                    cancellationToken: cancellationToken);

                var summary = new ScenarioSummary
                {
                    Name = scenario.Name,
                    Metrics = result.Metrics.Values
                        .OfType<NumericMetric>()
                        .ToDictionary(m => m.Name, m => new MetricSummary
                        {
                            Value = m.Value ?? 0,
                            Rating = m.Interpretation?.Rating.ToString() ?? "Unknown",
                            Failed = m.Interpretation?.Failed ?? false,
                            Reason = m.Interpretation?.Reason ?? ""
                        })
                };

                var done = Interlocked.Increment(ref completed);
                console?.WriteProgress(done, total, scenario.Name);

                return summary;
            }
            finally
            {
                semaphore.Release();
            }
        });

        scenarios.AddRange(await Task.WhenAll(tasks));

        console?.WriteLine($"Evaluation complete. {scenarios.Count} scenarios in {(DateTime.UtcNow - startedAt).TotalSeconds:F1}s");

        return new EvalResult
        {
            ExecutionName = request.ExecutionName,
            CompletedAt = DateTime.UtcNow,
            Scenarios = scenarios
        };
    }

    /// <summary>
    /// Groups scenario results by name and computes per-metric descriptive statistics.
    /// </summary>
    public static AggregatedEvalResult Aggregate(EvalResult result)
    {
        var groups = result.Scenarios
            .GroupBy(s => s.Name, StringComparer.Ordinal)
            .Select(g =>
            {
                // Collect all metric names across this group
                var metricNames = g.SelectMany(s => s.Metrics.Keys).Distinct().ToList();

                var metricStats = new Dictionary<string, MetricStats>();

                foreach (var metricName in metricNames)
                {
                    var values = g
                        .Select(s => s.Metrics.TryGetValue(metricName, out var m) ? m : null)
                        .Where(m => m is not null)
                        .Select(m => m!)
                        .ToList();

                    if (values.Count == 0)
                        continue;

                    var numericValues = values.Select(m => m.Value).ToList();
                    var failedCount = values.Count(m => m.Failed);

                    var mean = numericValues.Average();
                    var min = numericValues.Min();
                    var max = numericValues.Max();

                    double stdDev;
                    if (values.Count == 1)
                    {
                        stdDev = 0;
                    }
                    else
                    {
                        var sumSquaredDiffs = numericValues.Sum(v => (v - mean) * (v - mean));
                        stdDev = Math.Sqrt(sumSquaredDiffs / (values.Count - 1)); // sample std dev (N-1)
                    }

                    metricStats[metricName] = new MetricStats
                    {
                        Mean = mean,
                        StdDev = stdDev,
                        Min = min,
                        Max = max,
                        FailedFraction = (double)failedCount / values.Count
                    };
                }

                return new AggregatedScenario
                {
                    Name = g.Key,
                    SampleCount = g.Count(),
                    Metrics = metricStats
                };
            })
            .ToList();

        return new AggregatedEvalResult
        {
            ExecutionName = result.ExecutionName,
            CompletedAt = result.CompletedAt,
            Scenarios = result.Scenarios,
            Groups = groups
        };
    }

    /// <summary>
    /// Persists evaluation results (individual runs and per-scenario stats) to disk.
    /// Returns the path to the execution folder.
    /// </summary>
    public static async Task<string> PersistAsync(
        AggregatedEvalResult result,
        string storageRoot,
        JsonSerializerContext jsonContext)
    {
        var executionDir = Path.Combine(storageRoot, "results", result.ExecutionName);

        // Track iteration numbers per scenario name (same counting logic as RunAsync)
        var iterationCounters = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var scenario in result.Scenarios)
        {
            var iteration = iterationCounters.TryGetValue(scenario.Name, out var count) ? count + 1 : 1;
            iterationCounters[scenario.Name] = iteration;

            var scenarioDir = Path.Combine(executionDir, scenario.Name);
            Directory.CreateDirectory(scenarioDir);

            var iterationPath = Path.Combine(scenarioDir, $"{iteration}.json");
            var iterationJson = JsonSerializer.Serialize(scenario, typeof(ScenarioSummary), jsonContext);
            await File.WriteAllTextAsync(iterationPath, iterationJson);
        }

        // Write _stats.json per aggregated group
        foreach (var group in result.Groups)
        {
            var scenarioDir = Path.Combine(executionDir, group.Name);
            Directory.CreateDirectory(scenarioDir);

            var statsPath = Path.Combine(scenarioDir, "_stats.json");
            var statsJson = JsonSerializer.Serialize(group, typeof(AggregatedScenario), jsonContext);
            await File.WriteAllTextAsync(statsPath, statsJson);
        }

        return executionDir;
    }

    private static ReportingConfiguration BuildReportingConfiguration(EvalRequest request)
    {
        var evaluators = CreateEvaluators(request.EvaluatorNames);

        return DiskBasedReportingConfiguration.Create(
            storageRootPath: request.StorageRootPath,
            evaluators: evaluators,
            chatConfiguration: request.ChatConfiguration,
            enableResponseCaching: request.EnableResponseCaching,
            executionName: request.ExecutionName);
    }

    private static IEvaluator[] CreateEvaluators(IReadOnlySet<string> names)
    {
        var evaluators = new List<IEvaluator>();

        if (names.Contains("relevance"))
            evaluators.Add(new RelevanceEvaluator());
        if (names.Contains("coherence"))
            evaluators.Add(new CoherenceEvaluator());
        if (names.Contains("fluency"))
            evaluators.Add(new FluencyEvaluator());
        if (names.Contains("groundedness"))
            evaluators.Add(new GroundednessEvaluator());
        if (names.Contains("completeness"))
            evaluators.Add(new CompletenessEvaluator());
        if (names.Contains("equivalence"))
            evaluators.Add(new EquivalenceEvaluator());

        return evaluators.ToArray();
    }
}
