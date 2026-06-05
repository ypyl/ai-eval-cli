## Why

The native self-contained binary serves teams without .NET installed (Python, R, bash), but .NET shops using Azure DevOps or GitHub Actions already have the runtime. For them, a `dotnet tool install` is more natural than downloading platform-specific binaries — it integrates with existing tool manifests, auto-updates, and private NuGet feeds.

## What Changes

- Add `<PackAsTool>true</PackAsTool>` and NuGet metadata to the existing `AiEvalCli.csproj` — the same project continues to produce native binaries via `dotnet publish`
- CI/CD gains a `dotnet pack` step to produce a NuGet tool package alongside native binaries
- README updated with dotnet tool installation as an alternative to native binary download
- No code changes required — the CLI, engine, and arguments are unchanged

## Capabilities

### New Capabilities

- `dotnet-tool-distribution`: Package and distribute eval-cli as a .NET tool via NuGet, enabling `dotnet tool install --global eval-cli` and `dotnet tool install` with a tool manifest for repo-local usage.

### Modified Capabilities

<!-- No existing capabilities changed -->

## Impact

- **Affected code**: `src/AiEvalCli/AiEvalCli.csproj` (~5 lines of MSBuild properties added)
- **Affected CI/CD**: Build pipeline gains a pack-and-push step for NuGet
- **Dependencies**: None new — the tool uses the same engine and NuGet dependencies already declared
- **Breaking changes**: None — native binary distribution and CLI interface are unchanged
