# Changelog

## Unreleased

- Pin local builds and CI to .NET SDK 10.0.401 through global.json.
- Validate pull requests on Windows, Linux, and macOS without package publication.
- Use the patched SourceLink implementation supplied by the SDK instead of the vulnerable Microsoft.Build.Tasks.Git 8.0.0 dependency.

## 10.0.0

- Continued independently under the `ZinfoFramework.IdentityModel.OidcClient*` package IDs.
- Targeted .NET 10 exclusively.
- Preserved the historical `IdentityModel.OidcClient*` namespaces, assembly names, and public API surface.
- Updated the base dependency to `ZinfoFramework.IdentityModel` 10.0.0.
- Retained Apache-2.0 licensing and documented upstream attribution.
