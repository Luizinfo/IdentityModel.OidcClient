// Copyright (c) Duende Software. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.OidcClient.Results;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace IdentityModel.OidcClient
{
    /// <inheritdoc />
    public class JwtHandlerIdentityTokenValidator : IIdentityTokenValidator
    {
        /// <inheritdoc />
#pragma warning disable 1998
        public async Task<IdentityTokenValidationResult> ValidateAsync(string identityToken, OidcClientOptions options, CancellationToken cancellationToken = default)
#pragma warning restore 1998
        {
            var logger = options.LoggerFactory.CreateLogger<JwtHandlerIdentityTokenValidator>();
         
            logger.LogTrace("Validate");
            
            // setup general validation parameters
            var parameters = new TokenValidationParameters
            {
                ValidIssuer = options.ProviderInformation.IssuerName,
                ValidAudience = options.ClientId,
                ValidateIssuer = options.Policy.ValidateTokenIssuerName,
                NameClaimType = JwtClaimTypes.Name,
                RoleClaimType = JwtClaimTypes.Role,

                ClockSkew = options.ClockSkew
            };

            // read the token signing algorithm
            var handler = new JsonWebTokenHandler();
            JsonWebToken jwt;

            try
            {
                jwt = handler.ReadJsonWebToken(identityToken);
            }
            catch (Exception ex)
            {
                return new IdentityTokenValidationResult
                {
                    Error = $"Error validating identity token: {ex.ToString()}"
                };
            }

            var algorithm = jwt.Alg;
            
            // if token is unsigned, and this is allowed, skip signature validation
            if (string.Equals(algorithm, "none", StringComparison.OrdinalIgnoreCase))
            {
                if (options.Policy.RequireIdentityTokenSignature)
                {
                    return new IdentityTokenValidationResult
                    {
                        Error = $"Identity token is not signed. Signatures are required by policy"
                    };
                }
                else
                {
                    logger.LogInformation("Identity token is not signed. This is allowed by configuration.");
                    parameters.RequireSignedTokens = false;
                }
            }
            else
            {
                // check if signature algorithm is allowed by policy
                if (!options.Policy.ValidSignatureAlgorithms.Contains(algorithm))
                {
                    return new IdentityTokenValidationResult
                    {
                        Error = $"Identity token uses invalid algorithm: {algorithm}"
                    };
                };
            }

            var result = await ValidateSignatureAsync(identityToken, handler, parameters, options, logger);
            if (result.IsValid == false)
            {
                if (result.Exception is SecurityTokenSignatureKeyNotFoundException)
                {
                    logger.LogWarning("Key for validating token signature cannot be found. Refreshing keyset.");

                    return new IdentityTokenValidationResult
                    {
                        Error = "invalid_signature"
                    };
                }

                if (result.Exception is SecurityTokenValidationException)
                {
                    return new IdentityTokenValidationResult
                    {
                        Error = "unable_to_validate_token"
                    };
                }

                throw result.Exception;
            }

            var user = new ClaimsPrincipal(result.ClaimsIdentity);
            
            var error = CheckRequiredClaim(user);
            if (error.IsPresent())
            {
                return new IdentityTokenValidationResult
                {
                    Error = error
                };
            }

            return new IdentityTokenValidationResult
            {
                User = user,
                SignatureAlgorithm = algorithm
            };
        }

        private async Task<TokenValidationResult> ValidateSignatureAsync(string identityToken, JsonWebTokenHandler handler, TokenValidationParameters parameters, OidcClientOptions options, ILogger logger)
        {
            if (parameters.RequireSignedTokens)
            {
                // read keys from provider information
                var keys = new List<SecurityKey>();

                foreach (var webKey in options.ProviderInformation.KeySet.Keys)
                {
                    // Encryption keys and keys intended for other uses are not candidates for
                    // identity token signature validation.
                    if (webKey.Use != "sig" && webKey.Use != null)
                    {
                        logger.LogDebug("Key with kid: {kid} is not a signing key and was ignored", webKey.Kid ?? "not set");
                        continue;
                    }

                    if (webKey.E.IsPresent() && webKey.N.IsPresent())
                    {
                        var e = Base64Url.Decode(webKey.E);
                        var n = Base64Url.Decode(webKey.N);

                        var key = new RsaSecurityKey(new RSAParameters { Exponent = e, Modulus = n });
                        key.KeyId = webKey.Kid;

                        keys.Add(key);

                        logger.LogDebug("Added signing key with kid: {kid}", key?.KeyId ?? "not set");
                    }
                    else if (webKey.X.IsPresent() && webKey.Y.IsPresent() && webKey.Crv.IsPresent())
                    {
                        if (!TryGetCurveFromCrvValue(webKey.Crv, out var curve))
                        {
                            logger.LogDebug(
                                "Signing key with kid: {kid} uses unsupported curve {curve} and was ignored",
                                webKey.Kid ?? "not set",
                                webKey.Crv);
                            continue;
                        }

                        try
                        {
                            var ec = ECDsa.Create(new ECParameters
                            {
                                Curve = curve,
                                Q = new ECPoint
                                {
                                    X = Base64Url.Decode(webKey.X),
                                    Y = Base64Url.Decode(webKey.Y)
                                }
                            });

                            var key = new ECDsaSecurityKey(ec);
                            key.KeyId = webKey.Kid;

                            keys.Add(key);

                            logger.LogDebug("Added signing key with kid: {kid}", key?.KeyId ?? "not set");
                        }
                        catch (Exception ex) when (ex is ArgumentException or FormatException or CryptographicException or PlatformNotSupportedException)
                        {
                            logger.LogWarning(
                                ex,
                                "Signing key with kid: {kid} could not be materialized and was ignored",
                                webKey.Kid ?? "not set");
                        }
                    }
                    else
                    {
                        logger.LogDebug("Signing key with kid: {kid} currently not supported", webKey.Kid ?? "not set");
                    }
                }

                parameters.IssuerSigningKeys = keys;
            }
            
            return await handler.ValidateTokenAsync(identityToken, parameters);
        }

        private static string CheckRequiredClaim(ClaimsPrincipal user)
        {
            var requiredClaims = new List<string>
            {
                JwtClaimTypes.Issuer,
                JwtClaimTypes.Subject,
                JwtClaimTypes.IssuedAt,
                JwtClaimTypes.Audience,
                JwtClaimTypes.Expiration,
            };

            foreach (var claimType in requiredClaims)
            {
                var claim = user.FindFirst(claimType);
                if (claim == null)
                {
                    return $"{claimType} claim is missing";
                }
            }

            return null;
        }

        internal static bool TryGetCurveFromCrvValue(string crv, out ECCurve curve)
        {
            switch (crv)
            {
                case JsonWebKeyECTypes.P256:
                    curve = ECCurve.NamedCurves.nistP256;
                    return true;
                case JsonWebKeyECTypes.P384:
                    curve = ECCurve.NamedCurves.nistP384;
                    return true;
                case JsonWebKeyECTypes.P521:
                    curve = ECCurve.NamedCurves.nistP521;
                    return true;
                default:
                    curve = default;
                    return false;
            }
        }
    }
}
