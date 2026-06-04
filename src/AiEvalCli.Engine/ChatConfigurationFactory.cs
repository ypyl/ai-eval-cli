using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using OpenAI;

namespace AiEvalCli.Engine;

/// <summary>
/// Factory for creating ChatConfiguration instances for various LLM providers.
/// </summary>
public static class ChatConfigurationFactory
{
    /// <summary>
    /// Creates a ChatConfiguration using Azure OpenAI with DefaultAzureCredential.
    /// </summary>
    public static ChatConfiguration CreateAzureOpenAI(
        string endpoint,
        string deploymentName,
        string? tenantId = null)
    {
        var credential = new DefaultAzureCredential(
            new DefaultAzureCredentialOptions
            {
                TenantId = tenantId
            });

        var client = new AzureOpenAIClient(
            new Uri(endpoint),
            credential);

        var chatClient = client
            .GetChatClient(deploymentName)
            .AsIChatClient();

        return new ChatConfiguration(chatClient);
    }

    /// <summary>
    /// Creates a ChatConfiguration using Azure OpenAI with an API key.
    /// </summary>
    public static ChatConfiguration CreateAzureOpenAIWithKey(
        string endpoint,
        string apiKey,
        string deploymentName)
    {
        var client = new AzureOpenAIClient(
            new Uri(endpoint),
            new System.ClientModel.ApiKeyCredential(apiKey));

        var chatClient = client
            .GetChatClient(deploymentName)
            .AsIChatClient();

        return new ChatConfiguration(chatClient);
    }

    /// <summary>
    /// Creates a ChatConfiguration for an OpenAI-compatible API (OpenAI, DeepSeek, etc.).
    /// Uses the standard OpenAI SDK with an API key and custom endpoint.
    /// </summary>
    public static ChatConfiguration CreateOpenAICompatible(
        string endpoint,
        string apiKey,
        string modelName)
    {
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(endpoint)
        };

        var client = new OpenAIClient(
            new System.ClientModel.ApiKeyCredential(apiKey),
            options);

        var chatClient = client
            .GetChatClient(modelName)
            .AsIChatClient();

        return new ChatConfiguration(chatClient);
    }
}
