using IdentityModel;
using IdentityModel.Client;
using IdentityModel.OidcClient;
using Microsoft.Extensions.Logging;

namespace ConformanceTests;

internal sealed class Helper
{
    private readonly ConformanceRunnerOptions _runnerOptions;

    public Helper(ConformanceRunnerOptions runnerOptions)
    {
        _runnerOptions = runnerOptions;
    }

    public async Task<OidcClientOptions> CreateOidcClientOptionsAsync(CancellationToken cancellationToken = default)
    {
        using var httpClient = new HttpClient();
        var discovery = await httpClient.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
        {
            Address = _runnerOptions.DiscoveryUrl,
            Policy =
            {
                // This preliminary request is only used to locate dynamic registration.
                // The OidcClient performs discovery again with its normal validation policy.
                ValidateEndpoints = false,
                ValidateIssuerName = false
            }
        }, cancellationToken);

        if (discovery.IsError)
        {
            throw new InvalidOperationException($"Unable to load the exported discoveryurl: {discovery.Error}");
        }

        var clientId = _runnerOptions.ClientId;
        var clientSecret = _runnerOptions.ClientSecret;
        if (string.IsNullOrWhiteSpace(clientId))
        {
            if (string.IsNullOrWhiteSpace(discovery.RegistrationEndpoint))
            {
                throw new InvalidOperationException("The test does not expose dynamic registration. Supply --client-id and, when applicable, --client-secret.");
            }

            var registration = await RegisterClientAsync(
                httpClient,
                discovery.RegistrationEndpoint,
                cancellationToken);
            clientId = registration.ClientId;
            clientSecret = registration.ClientSecret;
        }

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddSimpleConsole(options => options.SingleLine = true);
        });

        return new OidcClientOptions
        {
            Authority = _runnerOptions.DiscoveryUrl,
            RedirectUri = _runnerOptions.RedirectUri.AbsoluteUri,
            ClientId = clientId,
            ClientSecret = clientSecret,
            Scope = _runnerOptions.Scope,
            TokenClientCredentialStyle = ClientCredentialStyle.AuthorizationHeader,
            Browser = new SystemBrowser(_runnerOptions.RedirectUri),
            FilterClaims = false,
            LoggerFactory = loggerFactory,
            IdentityTokenValidator = new JwtHandlerIdentityTokenValidator()
        };
    }

    public static void ShowResult(LoginResult result)
    {
        if (result.IsError)
        {
            Console.Error.WriteLine($"Conformance flow rejected: {result.Error}");
            return;
        }

        Console.WriteLine("Conformance flow completed successfully.");
        foreach (var claim in result.User.Claims)
        {
            Console.WriteLine($"claim {claim.Type}: {claim.Value}");
        }

        Console.WriteLine($"identity_token present: {!string.IsNullOrWhiteSpace(result.IdentityToken)}");
        Console.WriteLine($"access_token present: {!string.IsNullOrWhiteSpace(result.AccessToken)}");
        Console.WriteLine($"refresh_token present: {!string.IsNullOrWhiteSpace(result.RefreshToken)}");
    }

    private async Task<DynamicClientRegistrationResponse> RegisterClientAsync(
        HttpClient httpClient,
        string registrationEndpoint,
        CancellationToken cancellationToken)
    {
        var document = new DynamicClientRegistrationDocument
        {
            ApplicationType = "native",
            TokenEndpointAuthenticationMethod = OidcConstants.EndpointAuthenticationMethods.BasicAuthentication
        };
        document.RedirectUris.Add(_runnerOptions.RedirectUri);
        document.ResponseTypes.Add(OidcConstants.ResponseTypes.Code);
        document.GrantTypes.Add(OidcConstants.GrantTypes.AuthorizationCode);

        var registration = await httpClient.RegisterClientAsync(new DynamicClientRegistrationRequest
        {
            Address = registrationEndpoint,
            Document = document
        }, cancellationToken);

        if (registration.IsError)
        {
            throw new InvalidOperationException($"Dynamic client registration failed: {registration.ErrorDescription ?? registration.Error}");
        }

        return registration;
    }
}
