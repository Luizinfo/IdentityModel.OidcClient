# ZinfoFramework IdentityModel.OidcClient

> [!IMPORTANT]
> This repository is an independent ZinfoFramework continuation maintained by Luiz Antonio. It is not the upstream IdentityModel project and is not affiliated with Duende Software.

OpenID Connect and OAuth 2.0 client libraries for native applications, maintained for **.NET 10 (`net10.0`) only**. Existing namespaces and assembly names under `IdentityModel.OidcClient*` are intentionally preserved for source and binary identity continuity.

| Historical package ID | Current package ID |
| --- | --- |
| `IdentityModel.OidcClient` | `ZinfoFramework.IdentityModel.OidcClient` |
| `IdentityModel.OidcClient.DPoP` | `ZinfoFramework.IdentityModel.OidcClient.DPoP` |
| `IdentityModel.OidcClient.IdentityTokenValidator` | `ZinfoFramework.IdentityModel.OidcClient.IdentityTokenValidator` |

```shell
dotnet add package ZinfoFramework.IdentityModel.OidcClient --version 10.0.0
```

To migrate, replace the package ID (and the base dependency `IdentityModel` with `ZinfoFramework.IdentityModel`) and set the version to `10.0.0`. Do not rename `using IdentityModel.OidcClient...` directives: the historical namespaces and assemblies remain unchanged.

The upstream Basic RP test history is evidence that the implementation was conformance-tested; it is not a claim that this independent package is officially certified by the OpenID Foundation.

See [contributing](https://github.com/Luizinfo/IdentityModel.OidcClient/blob/main/CONTRIBUTING.md), [security](https://github.com/Luizinfo/IdentityModel.OidcClient/blob/main/SECURITY.md), [support](https://github.com/Luizinfo/IdentityModel.OidcClient/blob/main/SUPPORT.md), [license](https://github.com/Luizinfo/IdentityModel.OidcClient/blob/main/LICENSE), [notice](https://github.com/Luizinfo/IdentityModel.OidcClient/blob/main/NOTICE.md), and [changelog](https://github.com/Luizinfo/IdentityModel.OidcClient/blob/main/CHANGELOG.md).

Historical upstream: [IdentityModel/IdentityModel.OidcClient](https://github.com/IdentityModel/IdentityModel.OidcClient), created by Dominick Baier and Brock Allen with contributions from the upstream community.
