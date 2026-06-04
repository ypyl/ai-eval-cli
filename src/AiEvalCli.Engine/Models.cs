using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
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
/// A single evaluation scenario (prompt + response).
/// </summary>
public class EvalScenario
{
    /// <summary>Unique scenario name for report hierarchy. Use dot notation: "team.feature.scenario".</summary>
    public string Name { get; init; } = "";

    /// <summary>System prompt (optional).</summary>
    public string? SystemPrompt { get; init; }

    /// <summary>User query.</summary>
    public string UserQuery { get; init; } = "";

    /// <summary>Optional grounding context for groundedness evaluation.</summary>
    public string? Context { get; init; }

    /// <summary>Optional reference answer for equivalence evaluation.</summary>
    public string? ReferenceAnswer { get; init; }

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
        if (!string.IsNullOrWhiteSpace(ReferenceAnswer))
            contexts.Add(new EquivalenceEvaluatorContext(ReferenceAnswer));
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
/// Abstracts console output so the engine doesn't depend on System.Console.
/// </summary>
public interface IConsoleWriter
{
    void WriteLine(string message);
    void WriteProgress(int completed, int total, string currentScenario);
}
