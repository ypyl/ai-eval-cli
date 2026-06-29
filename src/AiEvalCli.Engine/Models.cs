using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.NLP;
using Microsoft.Extensions.AI.Evaluation.Quality;

namespace AiEvalCli.Engine;

/// <summary>
/// Evaluation request containing scenarios and configuration.
/// </summary>
public class EvalRequest
{
    /// <summary>Unique name for this evaluation run (used for report grouping).</summary>
    public string ExecutionName { get; init; } = $"eval-{DateTime.Now:yyyyMMddTHHmmss}";

    /// <summary>Path to store cached responses and results.</summary>
    public string StorageRootPath { get; init; } = "./eval-results";

    /// <summary>Evaluator names to run. Valid: relevance, coherence, fluency, groundedness, completeness, equivalence.</summary>
    public HashSet<string> EvaluatorNames { get; init; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "relevance", "coherence", "groundedness"
    };

    /// <summary>Chat configuration for the LLM endpoint.</summary>
    public ChatConfiguration ChatConfiguration { get; init; } = null!;

    /// <summary>Scenarios to evaluate.</summary>
    public List<EvalScenario> Scenarios { get; init; } = [];

    /// <summary>Enable LLM response caching (reuses cached responses for identical prompts).</summary>
    public bool EnableResponseCaching { get; init; } = true;

    /// <summary>Max parallel scenario evaluations.</summary>
    public int MaxConcurrency { get; init; } = 4;
}

/// <summary>
/// A single evaluation scenario — the LLM response to evaluate.
/// </summary>
public class EvalScenario
{
    /// <summary>Unique scenario name for report hierarchy. Use dot notation: "team.feature.scenario".</summary>
    public string Name { get; init; } = "";

    /// <summary>System prompt used to generate the response (optional).</summary>
    public string? SystemPrompt { get; init; }

    /// <summary>User query that produced the response.</summary>
    public string UserQuery { get; init; } = "";

    /// <summary>The LLM response to evaluate.</summary>
    public string Response { get; init; } = "";

    /// <summary>Optional grounding context for groundedness evaluation.</summary>
    public string? Context { get; init; }

    /// <summary>Optional reference answer for equivalence evaluation (deprecated — use ReferenceAnswers).</summary>
    public string? ReferenceAnswer { get; init; }

    /// <summary>Optional reference answers for NLP evaluators (BLEU, GLEU, F1) and Equivalence evaluator.
    /// Equivalence uses the first element; NLP evaluators use all elements.</summary>
    public IReadOnlyList<string> ReferenceAnswers { get; init; } = [];

    /// <summary>Optional retrieved context chunks for retrieval evaluator (RAG scenarios).</summary>
    public IReadOnlyList<string> RetrievedContextChunks { get; init; } = [];

    internal List<ChatMessage> GetChatMessages()
    {
        var messages = new List<ChatMessage>();
        if (!string.IsNullOrWhiteSpace(SystemPrompt))
            messages.Add(new ChatMessage(ChatRole.System, SystemPrompt));
        messages.Add(new ChatMessage(ChatRole.User, UserQuery));
        return messages;
    }

    internal List<EvaluationContext> GetContext()
    {
        var contexts = new List<EvaluationContext>();

        if (!string.IsNullOrWhiteSpace(Context))
            contexts.Add(new GroundednessEvaluatorContext(Context));

        // ponytail: inlined GetEffectiveReferenceAnswers — single call site, 5 lines
        var refs = ReferenceAnswers is { Count: > 0 }
            ? ReferenceAnswers
            : !string.IsNullOrWhiteSpace(ReferenceAnswer) ? new[] { ReferenceAnswer } : [];
        if (refs.Count > 0)
        {
            contexts.Add(new EquivalenceEvaluatorContext(refs[0]));
            contexts.Add(new BLEUEvaluatorContext(refs));
            contexts.Add(new GLEUEvaluatorContext(refs));
            contexts.Add(new F1EvaluatorContext(refs[0]));
        }

        if (RetrievedContextChunks is { Count: > 0 })
            contexts.Add(new RetrievalEvaluatorContext(RetrievedContextChunks));

        return contexts;
    }
}

/// <summary>
/// Evaluation result containing all scenario metrics.
/// </summary>
public class EvalResult
{
    public string ExecutionName { get; init; } = "";
    public DateTime CompletedAt { get; init; }
    public List<ScenarioSummary> Scenarios { get; init; } = [];
}

/// <summary>
/// Summary of a single evaluated scenario.
/// </summary>
public class ScenarioSummary
{
    public string Name { get; init; } = "";
    public Dictionary<string, MetricSummary> Metrics { get; init; } = [];
}

/// <summary>
/// Numeric metric summary for a scenario.
/// </summary>
public class MetricSummary
{
    public double Value { get; init; }
    public string Rating { get; init; } = "";
    public bool Failed { get; init; }
    public string Reason { get; init; } = "";
}

/// <summary>
/// Per-metric descriptive statistics computed across multiple runs.
/// </summary>
public record MetricStats
{
    public double Mean { get; init; }
    public double StdDev { get; init; }
    public double Min { get; init; }
    public double Max { get; init; }
    public double FailedFraction { get; init; }
}

/// <summary>
/// Aggregated statistics for a single scenario name across multiple evaluation runs.
/// </summary>
public record AggregatedScenario
{
    public string Name { get; init; } = "";
    public int SampleCount { get; init; }
    public Dictionary<string, MetricStats> Metrics { get; init; } = [];
}

/// <summary>
/// Evaluation result that includes both individual scenarios and aggregated groups.
/// </summary>
public class AggregatedEvalResult
{
    public string ExecutionName { get; init; } = "";
    public DateTime CompletedAt { get; init; }
    public List<ScenarioSummary> Scenarios { get; init; } = [];
    public List<AggregatedScenario> Groups { get; init; } = [];
}

/// <summary>
/// Console output delegate so the engine doesn't depend on System.Console.
/// </summary>
/// <param name="kind">"line" or "progress"</param>
/// <param name="message">For "line": the message. For "progress": formatted progress string.</param>
public delegate void ConsoleWriter(string kind, string? message = null, int completed = 0, int total = 0, string? currentScenario = null);
