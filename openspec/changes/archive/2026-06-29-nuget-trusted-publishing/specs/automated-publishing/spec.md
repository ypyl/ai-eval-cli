## ADDED Requirements

### Requirement: Automated NuGet publishing via OIDC
The project SHALL automate NuGet package publishing using Trusted Publishing (OIDC token exchange) so that no long-lived API key is required.

#### Scenario: Publish triggered by git tag
- **WHEN** a git tag matching `v*` (e.g., `v1.2.0`) is pushed to the repository
- **THEN** a GitHub Actions workflow builds and pushes the dotnet tool package to NuGet.org using a short-lived OIDC token

#### Scenario: OIDC token exchange succeeds
- **WHEN** the workflow reaches the publishing step
- **THEN** it exchanges a GitHub OIDC token for a temporary NuGet API key via `NuGet/login@v1`, and uses that key to push the `.nupkg`

#### Scenario: Publishing fails without manual secrets
- **WHEN** the workflow runs
- **THEN** it does NOT require a `NUGET_API_KEY` secret — authentication is handled entirely via OIDC

### Requirement: Git tag is the version source of truth
The git tag SHALL be the single deterministic source of the package version, overriding any version value in `.csproj`.

#### Scenario: Version extracted from tag
- **WHEN** a tag `v1.2.0` triggers the workflow
- **THEN** the version `1.2.0` is injected into `dotnet pack` and `dotnet publish` via `-p:Version=1.2.0`

#### Scenario: Semver prerelease tags supported
- **WHEN** a tag `v1.3.0-beta.1` triggers the workflow
- **THEN** the version `1.3.0-beta.1` is used for both the NuGet package and GitHub Release

### Requirement: GitHub Release with native binaries
The workflow SHALL create a GitHub Release and attach native single-file binaries for all supported platforms.

#### Scenario: Release created from tag
- **WHEN** a version tag is pushed
- **THEN** a GitHub Release is created with the tag name (e.g., `v1.2.0`) as the release title

#### Scenario: Native binaries attached
- **WHEN** the release is created
- **THEN** three platform-specific binaries are attached as release assets: `eval-cli` (linux-x64), `eval-cli.exe` (win-x64), and `eval-cli` (osx-arm64)

#### Scenario: Self-contained single-file executables
- **WHEN** the binaries are published
- **THEN** each is a self-contained single-file executable that runs without a .NET runtime installed

### Requirement: Workflow preserves existing dotnet tool behavior
The automated publishing SHALL preserve all existing dotnet tool packaging behavior defined in the `dotnet-tool-distribution` spec.

#### Scenario: Dotnet tool pack unchanged
- **WHEN** the workflow runs `dotnet pack`
- **THEN** it produces a framework-dependent NuGet package with the same metadata and structure as the current manual build

#### Scenario: CLI arguments unaffected
- **WHEN** a user installs the published package via `dotnet tool install`
- **THEN** the `eval-cli` command accepts all CLI arguments documented in the README

### Requirement: Trusted Publishing policy configuration
The project SHALL document the one-time manual configuration required on nuget.org and GitHub for the Trusted Publishing setup.

#### Scenario: Setup instructions clear
- **WHEN** a maintainer reads the project documentation
- **THEN** they find explicit steps for: (1) creating the Trusted Publishing policy on nuget.org, (2) adding the `NUGET_USERNAME` secret on GitHub

#### Scenario: Policy matches workflow
- **WHEN** the Trusted Publishing policy is created on nuget.org
- **THEN** it specifies repository owner `ypyl`, repository `ai-eval-cli`, and workflow file `publish.yml`
