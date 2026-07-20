namespace ConformanceTests;

internal sealed class ConformanceRunnerOptions
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    public required string DiscoveryUrl { get; init; }
    public required string ExecutionId { get; init; }
    public required Uri RedirectUri { get; init; }
    public string Profile { get; init; } = "basic";
    public string Scope { get; init; } = "openid profile";
    public string? ClientId { get; init; }
    public string? ClientSecret { get; init; }

    public static bool TryParse(
        string[] args,
        out ConformanceRunnerOptions options,
        out string? error)
    {
        options = null!;
        error = null;
        if (args.Any(argument => Comparer.Equals(argument, "--help") || Comparer.Equals(argument, "-h")))
        {
            return false;
        }

        var values = new Dictionary<string, string>(Comparer);
        for (var index = 0; index < args.Length; index += 2)
        {
            if (index + 1 >= args.Length || !args[index].StartsWith("--", StringComparison.Ordinal))
            {
                error = $"Invalid argument near '{args[index]}'. Options must use --name value pairs.";
                return false;
            }

            values[args[index][2..]] = args[index + 1];
        }

        if (!values.TryGetValue("discoveryurl", out var discoveryUrl) ||
            !Uri.TryCreate(discoveryUrl, UriKind.Absolute, out var discoveryUri) ||
            discoveryUri.Scheme != Uri.UriSchemeHttps)
        {
            error = "--discoveryurl must be an absolute HTTPS URL exported by the active OIDF test.";
            return false;
        }

        if (!values.TryGetValue("execution-id", out var executionId) || string.IsNullOrWhiteSpace(executionId))
        {
            error = "--execution-id is required so the manual run can be correlated with the OIDF test execution.";
            return false;
        }

        var profile = values.GetValueOrDefault("profile", "basic");
        if (!Comparer.Equals(profile, "basic"))
        {
            error = "Only the Basic RP profile is automated by this runner. Use --profile basic.";
            return false;
        }

        var redirectValue = values.GetValueOrDefault("redirect-uri", "http://127.0.0.1:7890/");
        if (!Uri.TryCreate(redirectValue, UriKind.Absolute, out var redirectUri) ||
            redirectUri.Scheme != Uri.UriSchemeHttp ||
            !redirectUri.IsLoopback)
        {
            error = "--redirect-uri must be an absolute HTTP loopback URI.";
            return false;
        }

        options = new ConformanceRunnerOptions
        {
            DiscoveryUrl = discoveryUri.AbsoluteUri,
            ExecutionId = executionId,
            RedirectUri = redirectUri,
            Profile = profile,
            Scope = values.GetValueOrDefault("scope", "openid profile"),
            ClientId = values.GetValueOrDefault("client-id"),
            ClientSecret = values.GetValueOrDefault("client-secret")
        };
        return true;
    }

    public static void WriteUsage(TextWriter writer)
    {
        writer.WriteLine("Usage:");
        writer.WriteLine("  dotnet run --project clients/ConformanceTests -- \\");
        writer.WriteLine("    --discoveryurl <OIDF exported discoveryurl> \\");
        writer.WriteLine("    --profile basic \\");
        writer.WriteLine("    --redirect-uri http://127.0.0.1:7890/ \\");
        writer.WriteLine("    --execution-id <OIDF execution URL or ID>");
        writer.WriteLine();
        writer.WriteLine("Optional: --client-id, --client-secret and --scope. Without --client-id, dynamic registration is used.");
    }
}
