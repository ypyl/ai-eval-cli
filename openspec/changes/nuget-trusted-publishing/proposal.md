## Why

NuGet is deprecating long-lived API keys for automated publishing in favor of Trusted Publishing (OIDC-based short-lived tokens). This change eliminates the credential management burden (no key rotation, no secret leaks) and brings the project's publishing pipeline in line with the recommended security practice. The project currently has no automated publishing — releases are manual.

## What Changes

- Add a GitHub Actions workflow (`publish.yml`) triggered by git tags (`v*`) that automates the full release process
- Use NuGet Trusted Publishing (OIDC token exchange) instead of a long-lived API key for `dotnet nuget push`
- Build and attach native single-file binaries (linux-x64, win-x64, osx-arm64) as GitHub Release assets
- The git tag becomes the single source of truth for version numbers — the workflow injects it into all build steps, overriding the `.csproj` version

## Capabilities

### New Capabilities
- `automated-publishing`: Automated NuGet package publishing via GitHub Actions using OIDC-based Trusted Publishing, plus GitHub Release creation with native binary assets for all supported platforms

### Modified Capabilities
<!-- No existing specs have requirement changes. The dotnet-tool-distribution spec covers the package itself; this change only modifies how it gets published. -->

## Impact

- **New file**: `.github/workflows/publish.yml` — the release workflow
- **Modified file**: `src/AiEvalCli/AiEvalCli.csproj` — remove hardcoded `<Version>` (tag becomes source of truth)
- **NuGet.org configuration**: one-time manual setup of Trusted Publishing policy
- **GitHub configuration**: one-time `NUGET_USERNAME` secret
- **Dependencies**: NuGet's `NuGet/login@v1` action, `softprops/action-gh-release@v2` for releases
- **User workflow change**: publish by pushing a git tag (`git tag v1.3.0 && git push --tags`) instead of manually running commands
