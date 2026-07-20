# OpenID Connect RP conformance runner

This .NET 10 runner validates the packaged `ZinfoFramework.IdentityModel`,
`ZinfoFramework.IdentityModel.OidcClient`, and
`ZinfoFramework.IdentityModel.OidcClient.DPoP`, and
`ZinfoFramework.IdentityModel.OidcClient.IdentityTokenValidator` version `10.0.0`. It intentionally has no
`ProjectReference` to the libraries under test and is restored only after the three OidcClient
packages have been produced in the local staging feed.

## Restore, build, and callback tests

From the repository root in PowerShell:

```powershell
$env:NUGET_PACKAGES = 'C:\Projetos\Identity\IdentityModel.OidcClient\.packages\conformance'
$config = '<path-to-a-NuGet.Config-with-the-local-package-feed-and-NuGet.org>'
dotnet restore .\clients\ConformanceTests\ConformanceTests.csproj --configfile $config
dotnet build .\clients\ConformanceTests\ConformanceTests.csproj -c Release --no-restore
dotnet test .\clients\ConformanceTests\ConformanceTests.csproj -c Release --no-build --no-restore
```

The automated tests cover both GET/query and `form_post` callbacks on an HTTP loopback redirect URI.

## Manual Basic RP conformance testing

External execution is deliberately manual and was not performed as part of the local migration.

Passing a Basic RP plan provides conformance-test evidence; it does not by itself certify this independent package. Formal certification is a separate OpenID Foundation process.

1. Open the [OpenID Foundation production conformance suite](https://www.certification.openid.net/), sign in, and create a new test plan.
2. Select `OpenID Connect Core: Basic Certification Profile Relying Party Tests`, choose the request type and dynamic or static client registration, and configure a unique alias.
3. Configure the runner redirect URI, for example `http://127.0.0.1:7890/`, in the plan.
4. Start one test. While it is `WAITING`, copy its exported `discoveryurl` and the execution URL or ID.
5. Run the client and complete the browser interaction:

```powershell
dotnet run --project .\clients\ConformanceTests\ConformanceTests.csproj -c Release --no-restore -- `
  --discoveryurl 'https://www.certification.openid.net/test/a/.../.well-known/openid-configuration' `
  --profile basic `
  --redirect-uri 'http://127.0.0.1:7890/' `
  --execution-id '<OIDF execution URL or ID>'
```

If the plan uses static registration, also pass `--client-id` and, when applicable,
`--client-secret`. Otherwise, the runner uses the dynamic registration endpoint exported by the
test. The runner configures `JwtHandlerIdentityTokenValidator`, so signed ID tokens are validated
with the packaged JWT validator.

Wait for negative tests to leave `WAITING` and reach their terminal state before starting the next
test. Record the execution URL or ID and preserve one log or screenshot per test for the later manual
gate.

## References

This adaptation was reviewed against [OpenID Conformance Suite release-v5.1.45 (2026-06-17)](https://gitlab.com/openid/conformance-suite/-/releases/release-v5.1.45).

- [Conformance Testing for OpenID Connect RPs](https://openid.net/certification/connect_rp_testing/)
- [OpenID certification instructions](https://openid.net/certification/how-to-certify-your-implementation/)
- [OpenID Conformance Suite project and production/staging endpoints](https://openid.net/certification/about-conformance-suite/)
- [OpenID Conformance Suite releases](https://gitlab.com/openid/conformance-suite/-/releases)
