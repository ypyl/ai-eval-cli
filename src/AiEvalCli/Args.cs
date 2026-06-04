namespace AiEvalCli;

/// <summary>
/// Simple manual argument parser. No dependencies, AOT-friendly.
/// </summary>
internal sealed class Args
{
    public bool ShowHelp { get; private set; }

    /// <summary>Provider: "azure" or "openai".</summary>
    public string Provider { get; private set; } = "azure";

    /// <summary>Endpoint URL (required).</summary>
    public string? Endpoint { get; private set; }

    /// <summary>Model name (Azure deployment name or OpenAI model ID).</summary>
    public string? Model { get; private set; }

    /// <summary>API key (required for openai, optional for azure).</summary>
    public string? ApiKey { get; private set; }

    public string[] Evaluators { get; private set; } = ["relevance", "coherence", "groundedness"];
    public string? InputFile { get; private set; }
    public string? InputJson { get; private set; }
    public string StoragePath { get; private set; } = "./eval-results";
    public string? ExecutionName { get; private set; }
    public int Parallel { get; private set; } = 4;
    public bool NoCache { get; private set; }
    public string OutputFormat { get; private set; } = "json";
    public string? OutputFile { get; private set; }

    public static Args Parse(string[] args)
    {
        var result = new Args();

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            var next = i + 1 < args.Length ? args[i + 1] : null;

            switch (arg)
            {
                case "--help" or "-h":
                    result.ShowHelp = true;
                    break;
                case "--provider":
                    result.Provider = RequireValue(arg, next, ref i).ToLowerInvariant();
                    break;
                case "--endpoint":
                    result.Endpoint = RequireValue(arg, next, ref i);
                    break;
                case "--model" or "-m" or "--deployment" or "-d":
                    result.Model = RequireValue(arg, next, ref i);
                    break;
                case "--api-key":
                    result.ApiKey = RequireValue(arg, next, ref i);
                    break;
                case "--evaluators" or "-e":
                    result.Evaluators = RequireValue(arg, next, ref i).Split(',', StringSplitOptions.TrimEntries);
                    break;
                case "--input" or "-i":
                    result.InputFile = RequireValue(arg, next, ref i);
                    break;
                case "--input-json":
                    result.InputJson = RequireValue(arg, next, ref i);
                    break;
                case "--storage" or "-s":
                    result.StoragePath = RequireValue(arg, next, ref i);
                    break;
                case "--name" or "-n":
                    result.ExecutionName = RequireValue(arg, next, ref i);
                    break;
                case "--parallel" or "-p":
                    result.Parallel = int.Parse(RequireValue(arg, next, ref i));
                    break;
                case "--no-cache":
                    result.NoCache = true;
                    break;
                case "--output" or "-o":
                    result.OutputFormat = RequireValue(arg, next, ref i);
                    break;
                case "--output-file":
                    result.OutputFile = RequireValue(arg, next, ref i);
                    break;
                default:
                    Console.Error.WriteLine($"Unknown option: {arg}");
                    break;
            }
        }

        return result;
    }

    private static string RequireValue(string arg, string? next, ref int i)
    {
        if (next is null || next.StartsWith('-'))
            throw new ArgumentException($"Option '{arg}' requires a value.");
        i++;
        return next;
    }
}
