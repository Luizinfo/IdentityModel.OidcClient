// Copyright (c) Duende Software. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using FluentAssertions;
using IdentityModel.Client;
using IdentityModel.OidcClient.DPoP;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace DPoPTests;

public class DPoPTest : IntegrationTestBase
{
    private static readonly string _jwkJson;

    static DPoPTest()
    {
        _jwkJson = CreateJwkJson();
    }

    [Fact]
    public async Task dpop_tokens_should_be_passed_to_token_endpoint()
    {
        var handler = new ProofTokenMessageHandler(_jwkJson, IdentityServerHost.Server.CreateHandler());
        var client = new HttpClient(handler);
        
        var tokenResponse = await RequestTokenAsync(client);

        tokenResponse.HttpStatusCode.Should().Be(HttpStatusCode.OK, tokenResponse.Error);
        tokenResponse.TokenType.Should().Be("DPoP");
        tokenResponse.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task when_nonce_required_nonce_should_be_used_for_token_endpoint()
    {
        IdentityServerHost.Client.RequireNonce = true;

        var handler = new ProofTokenMessageHandler(_jwkJson, IdentityServerHost.Server.CreateHandler());
        var client = new HttpClient(handler);

        var tokenResponse = await RequestTokenAsync(client);

        tokenResponse.HttpStatusCode.Should().Be(HttpStatusCode.OK, tokenResponse.Error);
        tokenResponse.TokenType.Should().Be("DPoP");
        IdentityServerHost.TokenEndpointInvocationCount.Should().Be(2);
    }

    [Fact]
    public async Task dpop_tokens_should_be_passed_to_api()
    {
        var tokenHandler = new ProofTokenMessageHandler(_jwkJson, IdentityServerHost.Server.CreateHandler());
        var tokenClient = new HttpClient(tokenHandler);

        var tokenResponse = await RequestTokenAsync(tokenClient);

        var apiHandler = new ProofTokenMessageHandler(_jwkJson, ApiHost.Server.CreateHandler());
        var apiClient = new HttpClient(apiHandler);
        apiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("DPoP", tokenResponse.AccessToken);

        ApiHost.ApiInvoked += ctx =>
        {
            ctx.User.Identity!.IsAuthenticated.Should().BeTrue();
        };

        var apiResponse = await apiClient.GetAsync(ApiHost.Url("/api"));
        apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task when_nonce_required_nonce_should_be_used_for_api_endpoint()
    {
        await ApiHost.DisposeAsync();
        ApiHost = CreateApiHost(validateNonce: true);
        await ApiHost.InitializeAsync();

        var tokenHandler = new ProofTokenMessageHandler(_jwkJson, IdentityServerHost.Server.CreateHandler());
        var tokenClient = new HttpClient(tokenHandler);

        var tokenResponse = await RequestTokenAsync(tokenClient);

        var apiHandler = new ProofTokenMessageHandler(_jwkJson, ApiHost.Server.CreateHandler());
        var apiClient = new HttpClient(apiHandler);
        apiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("DPoP", tokenResponse.AccessToken);

        ApiHost.ApiInvoked += ctx =>
        {
            ctx.User.Identity!.IsAuthenticated.Should().BeTrue();
        };

        var apiResponse = await apiClient.GetAsync(ApiHost.Url("/api"));
        apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task api_should_reject_proof_signed_with_different_key()
    {
        var tokenClient = new HttpClient(new ProofTokenMessageHandler(_jwkJson, IdentityServerHost.Server.CreateHandler()));
        var tokenResponse = await RequestTokenAsync(tokenClient);

        var differentKey = CreateJwkJson();
        var apiClient = new HttpClient(new ProofTokenMessageHandler(differentKey, ApiHost.Server.CreateHandler()));
        apiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("DPoP", tokenResponse.AccessToken);

        var apiResponse = await apiClient.GetAsync(ApiHost.Url("/api"));

        apiResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task api_should_reject_proof_with_divergent_ath()
    {
        var tokenClient = new HttpClient(new ProofTokenMessageHandler(_jwkJson, IdentityServerHost.Server.CreateHandler()));
        var tokenResponse = await RequestTokenAsync(tokenClient);
        var proofFactory = new DPoPProofTokenFactory(_jwkJson);
        var proof = proofFactory.CreateProofToken(new DPoPProofRequest
        {
            Method = HttpMethod.Get.Method,
            Url = ApiHost.Url("/api"),
            AccessToken = "different-access-token"
        });
        using var request = new HttpRequestMessage(HttpMethod.Get, ApiHost.Url("/api"));
        request.Headers.Authorization = new AuthenticationHeaderValue("DPoP", tokenResponse.AccessToken);
        request.Headers.TryAddWithoutValidation("DPoP", proof.ProofToken);
        using var apiClient = ApiHost.Server.CreateClient();

        var apiResponse = await apiClient.SendAsync(request);

        apiResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private Task<TokenResponse> RequestTokenAsync(HttpClient client)
    {
        return client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = IdentityServerHost.Url("/connect/token"),
            ClientId = IdentityServerHost.Client.ClientId,
            ClientSecret = IdentityServerHost.Client.ClientSecret,
            Scope = IdentityServerHost.Client.Scope
        });
    }

    private static string CreateJwkJson()
    {
        var key = new RsaSecurityKey(RSA.Create(2048));
        var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(key);
        jwk.Alg = SecurityAlgorithms.RsaSha256;
        return JsonSerializer.Serialize(jwk);
    }
}
