## Context

`eval-cli` currently ships as a native self-contained binary (`dotnet publish -r <rid> --self-contained`). .NET teams who already have the runtime installed would benefit from a dotnet tool distribution — it integrates with existing tool manifests, private NuGet feeds, and supports `dotnet tool update` for version management. The two distribution models can coexist because they're triggered by different MSBuild commands (`publish` vs `pack`) and the SDK properties don't conflict.

## Goals / Non-Goals

**Goals:**
- Add dotnet tool packaging to the existing `AiEvalCli` project without a separate project head
- Produce a framework-dependent NuGet package that installs via `dotnet tool install`
- Preserve the existing native binary build path unchanged
- Keep the same CLI interface, engine, and dependencies

**Non-Goals:**
- Replacing native binary distribution — both modes coexist
- Creating a separate tool project (`AiEvalCli.Tool`) — single-project approach is simpler
- Self-contained dotnet tool (not supported by .NET tool infrastructure)
- Changing the runtime target (`net10.0`)

## Decisions

### Decision 1: Single-project approach (add `PackAsTool` to existing csproj)

**Chosen**: Add `<PackAsTool>true</PackAsTool>` and NuGet metadata to `AiEvalCli.csproj`.

**Rationale**: `SelfContained`, `PublishSingleFile`, and `PublishTrimmed` are publish-time properties — they only affect `dotnet publish`, not `dotnet build` or `dotnet pack`. The two packaging modes don't conflict. A separate project would duplicate `Program.cs` and `Args.cs` with no benefit.

**Alternatives considered**:
- *Separate `AiEvalCli.Tool` project*: Cleaner separation but duplicates thin CLI layer. Rejected as unnecessary overhead.
- *MSBuild conditions to toggle modes*: Adds complexity with conditional properties. Rejected for a feature that should always be on.

### Decision 2: Framework-dependent tool package

**Chosen**: The NuGet package contains IL DLLs (not native binary), requiring .NET 10 runtime.

**Rationale**: This is the standard dotnet tool model. The tool package is small (~50 KB vs ~65 MB for native binary). Self-contained tools aren't supported by the .NET tool infrastructure — `dotnet tool install` always builds against the installed runtime.

### Decision 3: Package identity

**Chosen**: `PackageId` = `eval-cli`, version follows the same versioning as native binary releases.

**Rationale**: The `AssemblyName` is already `eval-cli`, so the tool command will be `eval-cli` — consistent with the native binary invocation.

## Risks / Trade-offs

| Risk | Mitigation |
|---|---|
| Users attempt `dotnet tool install` without .NET 10 runtime | README documents runtime prerequisite clearly; native binary remains available for those without .NET |
| Package version drifts from native binary version | Both derive from the same source (Git tag or csproj `<Version>`). CI publishes both from the same commit. |
| `PublishTrimmed` accidentally affects tool build | Not possible — `PublishTrimmed` is a publish property; `dotnet pack` uses `dotnet build` which is untrimmed. |
| NuGet feed availability (workstation with restricted internet at build time) | Native binary distro is unaffected; dotnet tool is additive only |

## Open Questions

- **NuGet destination**: NuGet.org (public), private Azure Artifacts feed, or both? Decision deferred to pipeline configuration; doesn't affect the code change.
- **Pre-release versions**: Should CI push prerelease packages (e.g., `1.0.0-preview.1`) for non-tagged builds? The version property is straightforward to update later.
