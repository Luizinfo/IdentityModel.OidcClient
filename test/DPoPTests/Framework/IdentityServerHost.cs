// Copyright (c) Duende Software. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using ApiHost;
using IdentityModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DPoPTests;

public class IdentityServerHost : GenericHost
{
    private const string TokenEndpointScheme = "token-endpoint";
    private readonly RsaSecurityKey _signingKey;

    public IdentityServerHost(string baseAddress = "https://identityserver") 
        : base(baseAddress)
    {
        _signingKey = new RsaSecurityKey(RSA.Create(2048))
        {
            KeyId = "dpop-tests-signing-key"
        };

        OnConfigureServices += ConfigureServices;
        OnConfigure += Configure;
    }

    public TestClient Client { get; } = new()
    {
        ClientId = "client_credentials_client",
        ClientSecret = "secret",
        Scope = "scope1"
    };

    public string Issuer => Url().TrimEnd('/');
    public string Audience => Url("/resources");
    public SecurityKey SigningKey => _signingKey;
    public int TokenEndpointInvocationCount { get; private set; }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();
        services.AddDataProtection();
        services.ConfigureDPoPTokensForScheme(TokenEndpointScheme, options =>
        {
            options.ClientClockSkew = TimeSpan.FromMinutes(1);
        });
    }

    private void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints => endpoints.MapPost("/connect/token", ProcessTokenRequestAsync));
    }

    private async Task ProcessTokenRequestAsync(HttpContext context)
    {
        TokenEndpointInvocationCount++;

        if (!context.Request.HasFormContentType)
        {
            await WriteTokenErrorAsync(context, "invalid_request", StatusCodes.Status400BadRequest);
            return;
        }

        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        var (clientId, clientSecret) = ReadClientCredentials(context, form);
        var grantType = form[OidcConstants.TokenRequest.GrantType].ToString();
        var requestedScope = form[OidcConstants.TokenRequest.Scope].ToString();

        if (!string.Equals(grantType, OidcConstants.GrantTypes.ClientCredentials, StringComparison.Ordinal) ||
            !string.Equals(clientId, Client.ClientId, StringComparison.Ordinal) ||
            !string.Equals(clientSecret, Client.ClientSecret, StringComparison.Ordinal) ||
            (!string.IsNullOrWhiteSpace(requestedScope) && !string.Equals(requestedScope, Client.Scope, StringComparison.Ordinal)))
        {
            await WriteTokenErrorAsync(context, "invalid_client", StatusCodes.Status401Unauthorized);
            return;
        }

        var proof = context.Request.Headers[OidcConstants.HttpHeaders.DPoP].ToString();
        var validator = context.RequestServices.GetRequiredService<DPoPProofValidator>();
        var validation = await validator.ValidateAsync(new DPoPProofValidatonContext
        {
            Scheme = TokenEndpointScheme,
            ProofToken = proof,
            Method = context.Request.Method,
            Url = Url("/connect/token"),
            ValidateNonce = Client.RequireNonce
        });

        if (validation.IsError)
        {
            if (!string.IsNullOrWhiteSpace(validation.ServerIssuedNonce))
            {
                context.Response.Headers[OidcConstants.HttpHeaders.DPoPNonce] = validation.ServerIssuedNonce;
            }

            await WriteTokenErrorAsync(context, validation.Error ?? OidcConstants.TokenErrors.InvalidDPoPProof, StatusCodes.Status400BadRequest);
            return;
        }

        var accessToken = CreateAccessToken(validation.JsonWebKeyThumbprint);
        context.Response.StatusCode = StatusCodes.Status200OK;
        await context.Response.WriteAsJsonAsync(new
        {
            access_token = accessToken,
            expires_in = 300,
            scope = Client.Scope,
            token_type = OidcConstants.AuthenticationSchemes.AuthorizationHeaderDPoP
        });
    }

    private string CreateAccessToken(string thumbprint)
    {
        var now = DateTimeOffset.UtcNow;
        var payload = new Dictionary<string, object>
        {
            [JwtClaimTypes.Issuer] = Issuer,
            [JwtClaimTypes.Audience] = Audience,
            [JwtClaimTypes.Expiration] = now.AddMinutes(5).ToUnixTimeSeconds(),
            [JwtClaimTypes.IssuedAt] = now.ToUnixTimeSeconds(),
            [JwtClaimTypes.NotBefore] = now.ToUnixTimeSeconds(),
            [JwtClaimTypes.JwtId] = CryptoRandom.CreateUniqueId(),
            [JwtClaimTypes.ClientId] = Client.ClientId,
            [JwtClaimTypes.Scope] = Client.Scope,
            [JwtClaimTypes.Confirmation] = new Dictionary<string, string>
            {
                [JwtClaimTypes.ConfirmationMethods.JwkThumbprint] = thumbprint
            }
        };
        var header = new Dictionary<string, object> { ["typ"] = "at+jwt" };
        var handler = new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = false };
        return handler.CreateToken(
            JsonSerializer.Serialize(payload),
            new SigningCredentials(_signingKey, SecurityAlgorithms.RsaSha256),
            header);
    }

    private static (string ClientId, string ClientSecret) ReadClientCredentials(HttpContext context, IFormCollection form)
    {
        if (AuthenticationHeaderValue.TryParse(context.Request.Headers.Authorization, out var authorization) &&
            string.Equals(authorization.Scheme, "Basic", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(authorization.Parameter))
        {
            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(authorization.Parameter));
                var separator = decoded.IndexOf(':');
                if (separator >= 0)
                {
                    return (decoded[..separator], decoded[(separator + 1)..]);
                }
            }
            catch (FormatException)
            {
                // Invalid credentials are handled by the caller.
            }
        }

        return (form[OidcConstants.TokenRequest.ClientId].ToString(), form[OidcConstants.TokenRequest.ClientSecret].ToString());
    }

    private static Task WriteTokenErrorAsync(HttpContext context, string error, int statusCode)
    {
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(new { error });
    }
}

public sealed class TestClient
{
    public string ClientId { get; init; } = default!;
    public string ClientSecret { get; init; } = default!;
    public string Scope { get; init; } = default!;
    public bool RequireNonce { get; set; }
}
