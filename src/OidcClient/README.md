# ZinfoFramework.IdentityModel.OidcClient

> Independent ZinfoFramework continuation maintained by Luiz Antonio; not the upstream IdentityModel project and not affiliated with Duende Software.

This `net10.0` package supplies the OpenID Connect client primitives and high-level login, logout, user-info, and token-refresh flows for native applications.

| Historical package ID | Current package ID |
| --- | --- |
| `IdentityModel.OidcClient` | `ZinfoFramework.IdentityModel.OidcClient` |

```shell
dotnet add package ZinfoFramework.IdentityModel.OidcClient --version 10.0.0
```

Migration requires changing the package reference from `IdentityModel.OidcClient` to `ZinfoFramework.IdentityModel.OidcClient` and the base dependency from `IdentityModel` to `ZinfoFramework.IdentityModel`. Namespaces and the `IdentityModel.OidcClient` assembly name are preserved; do not rename existing `using IdentityModel.OidcClient...` directives.

Related packages: [DPoP](https://www.nuget.org/packages/ZinfoFramework.IdentityModel.OidcClient.DPoP) and [IdentityTokenValidator](https://www.nuget.org/packages/ZinfoFramework.IdentityModel.OidcClient.IdentityTokenValidator).

The historical upstream Basic RP approval is evidence of conformance testing, not official OpenID Foundation certification of this independent package.

[Project](https://github.com/Luizinfo/IdentityModel.OidcClient) · [License](https://github.com/Luizinfo/IdentityModel.OidcClient/blob/main/LICENSE) · [Notice](https://github.com/Luizinfo/IdentityModel.OidcClient/blob/main/NOTICE.md) · [Changelog](https://github.com/Luizinfo/IdentityModel.OidcClient/blob/main/CHANGELOG.md) · [Security](https://github.com/Luizinfo/IdentityModel.OidcClient/blob/main/SECURITY.md) · [Support](https://github.com/Luizinfo/IdentityModel.OidcClient/blob/main/SUPPORT.md)

Historical upstream: [IdentityModel/IdentityModel.OidcClient](https://github.com/IdentityModel/IdentityModel.OidcClient), by Dominick Baier, Brock Allen, and contributors.
