// Copyright (c) Duende Software. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;
using ApiHost;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace DPoPTests;

public class ApiHost : GenericHost
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly SecurityKey _signingKey;

    public bool ValidateNonce { get; }

    public event Action<Microsoft.AspNetCore.Http.HttpContext> ApiInvoked = ctx => { };
        
    public ApiHost(
        string issuer,
        string audience,
        SecurityKey signingKey,
        bool validateNonce,
        string baseAddress = "https://api")
        : base(baseAddress)
    {
        _issuer = issuer;
        _audience = audience;
        _signingKey = signingKey;
        ValidateNonce = validateNonce;

        OnConfigureServices += ConfigureServices;
        OnConfigure += Configure;
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();
        services.AddAuthorization();

        services.AddAuthentication("token")
            .AddJwtBearer("token", options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = _signingKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });
        services.ConfigureDPoPTokensForScheme("token", options =>
        {
            options.ClientClockSkew = TimeSpan.FromMinutes(1);
            options.ValidateNonce = ValidateNonce;
        });
    }

    private void Configure(IApplicationBuilder app)
    {
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(ep => {
            ep.MapGet("/api", ctx =>
            {
                ApiInvoked.Invoke(ctx);
                ctx.Response.StatusCode = 200;
                return Task.CompletedTask;
            })
            .RequireAuthorization();
        });
    }
}
