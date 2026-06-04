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

        // Process in parallel with controlled concurrency
        var semaphore = new SemaphoreSlim(request.MaxConcurrency);
        var tasks = request.Scenarios.Select(async scenario =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                await using var run = await config.CreateScenarioRunAsync(scenario.Name);

                var messages = scenario.GetChatMessages();
                var response = await run.ChatConfiguration!.ChatClient
                    .GetResponseAsync(messages, cancellationToken: cancellationToken);

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
