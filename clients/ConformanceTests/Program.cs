using IdentityModel.OidcClient;

namespace ConformanceTests;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (!ConformanceRunnerOptions.TryParse(args, out var options, out var error))
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                Console.Error.WriteLine(error);
            }

            ConformanceRunnerOptions.WriteUsage(Console.Error);
            return string.IsNullOrWhiteSpace(error) ? 0 : 2;
        }

        Console.WriteLine($"OIDF profile: {options.Profile}");
        Console.WriteLine($"OIDF execution: {options.ExecutionId}");
        Console.WriteLine($"discoveryurl: {options.DiscoveryUrl}");
        Console.WriteLine($"redirect URI: {options.RedirectUri}");

        try
        {
            var helper = new Helper(options);
            var oidcOptions = await helper.CreateOidcClientOptionsAsync();
            var client = new OidcClient(oidcOptions);
            var result = await client.LoginAsync(new LoginRequest());
            Helper.ShowResult(result);
            return result.IsError ? 1 : 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Conformance flow failed: {exception.Message}");
            return 1;
        }
    }
}
