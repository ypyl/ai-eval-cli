using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.NLP;
using Microsoft.Extensions.AI.Evaluation.Quality;
using AiEvalCli.Engine;

namespace AiEvalCli.Tests;

public class EvalEngineTests
{
    [Fact]
    public void CreateEvaluators_ReturnsCorrectType_ForRTC()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "rtc" };
        var evaluators = EvalEngine.CreateEvaluators(names);
        Assert.Single(evaluators);
        Assert.IsType<RelevanceTruthAndCompletenessEvaluator>(evaluators[0]);
    }

    [Fact]
    public void CreateEvaluators_ExistingEvaluators_StillWork()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "relevance", "coherence", "fluency", "groundedness", "completeness", "equivalence" };

        var evaluators = EvalEngine.CreateEvaluators(names);

        Assert.Equal(6, evaluators.Length);
        Assert.Contains(evaluators, e => e is RelevanceEvaluator);
        Assert.Contains(evaluators, e => e is CoherenceEvaluator);
        Assert.Contains(evaluators, e => e is FluencyEvaluator);
        Assert.Contains(evaluators, e => e is GroundednessEvaluator);
        Assert.Contains(evaluators, e => e is CompletenessEvaluator);
        Assert.Contains(evaluators, e => e is EquivalenceEvaluator);
    }

    [Fact]
    public void CreateEvaluators_MixedNewAndExisting_ReturnsAll()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "relevance", "rtc" };

        var evaluators = EvalEngine.CreateEvaluators(names);

        Assert.Equal(2, evaluators.Length);
        Assert.Contains(evaluators, e => e is RelevanceEvaluator);
        Assert.Contains(evaluators, e => e is RelevanceTruthAndCompletenessEvaluator);
    }
}

public class EvalScenarioContextTests
{
    [Fact]
    public void GetContext_WithReferenceAnswers_BuildsAllContexts()
    {
        var scenario = new EvalScenario
        {
            Name = "test",
            UserQuery = "What is the Moon's distance?",
            Response = "About 238,855 miles.",
            ReferenceAnswers = new List<string> { "238,855 miles", "Approximately 384,400 km" }
        };

        var contexts = scenario.GetContext();

        Assert.Equal(4, contexts.Count);
        Assert.Contains(contexts, c => c is EquivalenceEvaluatorContext);
        Assert.Contains(contexts, c => c is BLEUEvaluatorContext);
        Assert.Contains(contexts, c => c is GLEUEvaluatorContext);
        Assert.Contains(contexts, c => c is F1EvaluatorContext);
    }

    [Fact]
    public void GetContext_WithOnlyGroundedness_BuildsOnlyGroundedness()
    {
        var scenario = new EvalScenario
        {
            Name = "test",
            UserQuery = "What?",
            Response = "Whatever.",
            Context = "Some grounding context"
        };

        var contexts = scenario.GetContext();

        Assert.Single(contexts);
        Assert.IsType<GroundednessEvaluatorContext>(contexts[0]);
    }

    [Fact]
    public void GetContext_BackwardsCompat_ReferenceAnswerSingular()
    {
        var scenario = new EvalScenario
        {
            Name = "test",
            UserQuery = "What is the Moon's distance?",
            Response = "About 238,855 miles.",
            ReferenceAnswer = "238,855 miles"
        };

        var contexts = scenario.GetContext();

        var eqCtx = contexts.OfType<EquivalenceEvaluatorContext>().Single();
        Assert.NotNull(eqCtx);
    }

    [Fact]
    public void GetContext_ReferenceAnswersPlural_TakesPrecedence()
    {
        var scenario = new EvalScenario
        {
            Name = "test",
            UserQuery = "What?",
            Response = "Whatever.",
            ReferenceAnswer = "old reference",
            ReferenceAnswers = new List<string> { "new ref 1", "new ref 2" }
        };

        var contexts = scenario.GetContext();
        var eqCtx = contexts.OfType<EquivalenceEvaluatorContext>().Single();
        Assert.NotNull(eqCtx);
    }

    [Fact]
    public void GetContext_EmptyReferenceAnswers_NoEquivalenceContext()
    {
        var scenario = new EvalScenario
        {
            Name = "test",
            UserQuery = "What?",
            Response = "Whatever.",
            ReferenceAnswers = new List<string>()
        };

        var contexts = scenario.GetContext();
        Assert.DoesNotContain(contexts, c => c is EquivalenceEvaluatorContext);
    }

    [Fact]
    public void GetContext_NoOptionalFields_ReturnsEmptyList()
    {
        var scenario = new EvalScenario
        {
            Name = "test",
            UserQuery = "What?",
            Response = "Whatever."
        };

        var contexts = scenario.GetContext();
        Assert.Empty(contexts);
    }
}
