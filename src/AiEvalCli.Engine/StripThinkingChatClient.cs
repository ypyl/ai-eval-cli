using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace AiEvalCli.Engine;

/// <summary>
/// Middleware that strips thinking/reasoning tokens from model responses.
/// Thinking models (DeepSeek v4, o1, o3-mini, etc.) emit &lt;thinking&gt;...&lt;/thinking&gt; blocks
/// before the final answer. Evaluators need clean text — raw reasoning confuses scoring.
/// </summary>
public partial class StripThinkingChatClient : IChatClient
{
    private readonly IChatClient _inner;

    [GeneratedRegex(@"<thinking>[\s\S]*?</thinking>\s*")]
    private static partial Regex ThinkingPattern();

    public StripThinkingChatClient(IChatClient inner) => _inner = inner;

    public void Dispose() => _inner.Dispose();

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await _inner.GetResponseAsync(messages, options, cancellationToken);

        var cleaned = response.Messages.Select(m =>
                new ChatMessage(m.Role, string.IsNullOrEmpty(m.Text)
                    ? m.Contents
                    : [new TextContent(ThinkingPattern().Replace(m.Text, "").Trim())]))
            .ToList();

        return new ChatResponse(cleaned)
        {
            ResponseId = response.ResponseId,
            ModelId = response.ModelId,
            FinishReason = response.FinishReason,
            Usage = response.Usage,
            AdditionalProperties = response.AdditionalProperties
        };
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var update in _inner.GetStreamingResponseAsync(messages, options, cancellationToken))
            yield return update;
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
        => _inner.GetService(serviceType, serviceKey);
}
