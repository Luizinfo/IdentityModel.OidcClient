// Copyright (c) Duende Software. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace DPoPTests;

public class IntegrationTestBase : IAsyncLifetime
{
    protected readonly IdentityServerHost IdentityServerHost;
    protected ApiHost ApiHost;

    public IntegrationTestBase()
    {
        IdentityServerHost = new IdentityServerHost();
        ApiHost = CreateApiHost(validateNonce: false);
    }

    protected ApiHost CreateApiHost(bool validateNonce)
    {
        return new ApiHost(
            IdentityServerHost.Issuer,
            IdentityServerHost.Audience,
            IdentityServerHost.SigningKey,
            validateNonce);
    }

    public virtual async Task InitializeAsync()
    {
        await IdentityServerHost.InitializeAsync();
        await ApiHost.InitializeAsync();
    }

    public virtual async Task DisposeAsync()
    {
        await ApiHost.DisposeAsync();
        await IdentityServerHost.DisposeAsync();
    }
}
