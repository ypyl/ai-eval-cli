## Context

The `eval-cli` project is a .NET dotnet tool published to NuGet.org at package ID `eval-cli`. Currently, publishing is fully manual — the maintainer runs `dotnet pack` and `dotnet nuget push` locally with an API key. NuGet.org is now strongly discouraging long-lived API keys and recommends Trusted Publishing (OIDC-based short-lived tokens) for automated workflows. The project has no CI/CD pipeline for releases. The README references downloading native binaries from GitHub Releases, but no release automation exists.

**Constraints:**
- Must work with GitHub Actions (the repository is on GitHub at `ypyl/ai-eval-cli`)
- Must cross-compile native binaries for linux-x64, win-x64, and osx-arm64
- The project uses .NET 10 (LTS) with self-contained single-file publishing
- Trusted Publishing is available on the maintainer's nuget.org account

## Goals / Non-Goals

**Goals:**
- Eliminate long-lived API keys from the publishing process
- Automate the full release: build, pack, push to NuGet, create GitHub Release
- Make versioning effortless — git tag becomes the single source of truth
- Attach native binaries (linux-x64, win-x64, osx-arm64) as GitHub Release assets
- Publish the dotnet tool package to NuGet.org

**Non-Goals:**
- Multi-repository or multi-package publishing (single package only)
- Native AOT publishing (still experimental; commented out in `.csproj`)
- Docker-based builds or multi-OS CI matrix
- Cross-compilation for OS-specific targets beyond the three listed
- Package signing or SBOM generation (can be added later)

## Decisions

### Decision 1: Git tags as the version source of truth

**Choice**: The workflow reads the version from the git tag (`refs/tags/v*`), strips the `v` prefix, and injects it via `-p:Version=$VERSION` into all `dotnet` commands. The `<Version>` element is removed from `.csproj` (local builds will show `1.0.0` default).

**Alternatives considered**:
- **Manual version in .csproj**: Requires editing the file before tagging, two sources of truth. Rejected — the user explicitly wants versioning to be automatic.
- **workflow_dispatch with version input**: Requires clicking through the GitHub UI to enter a version. Rejected — an extra human step, and the version still ends up only in the workflow run history, not in git history.
- **Auto-bump on push to main**: Removes semantic versioning control from the maintainer. Rejected — the maintainer should decide major/minor/patch.

**Rationale**: Git tags are immutable, auditable, and already the standard for release versioning in open-source .NET projects.

### Decision 2: Single Linux runner for cross-compilation

**Choice**: Run the entire workflow on `ubuntu-latest`. Cross-compile win-x64 and osx-arm64 from Linux using the .NET SDK's bundled targeting packs. No matrix strategy.

**Alternatives considered**:
- **Multi-OS matrix (ubuntu, windows, macos)**: More CI minutes, artifact-passing complexity. Rejected — `PublishSingleFile` + `SelfContained` enables cross-compilation from a single Linux runner.
- **Separate build and publish jobs**: Overhead of uploading/downloading artifacts between jobs. Rejected — the project builds in under 5 minutes, a single job is simpler.

**Rationale**: .NET 10's single-file self-contained publishing bundles the runtime for any RID (runtime identifier) from any build host. We only need three sequential `dotnet publish` commands with different `-r` flags.

### Decision 3: NuGet/login@v1 action for OIDC token exchange

**Choice**: Use the official `NuGet/login@v1` GitHub Action to perform the OIDC token exchange with nuget.org. The action requests a short-lived (1-hour) API key and exposes it via `steps.login.outputs.NUGET_API_KEY`.

**Alternatives considered**:
- **Manual OIDC token exchange via curl**: Error-prone, requires implementing the token exchange protocol. Rejected — NuGet/login@v1 is the official, maintained solution.
- **Keep using API key as GitHub secret**: Doesn't solve the problem. Rejected — the entire point of the change is to eliminate long-lived secrets.

**Rationale**: The NuGet team maintains this action. It handles the full OIDC flow: request token from GitHub, exchange with nuget.org, expose temporary API key.

### Decision 4: softprops/action-gh-release@v2 for GitHub Releases

**Choice**: Use `softprops/action-gh-release@v2` to create the GitHub Release from the tag and attach the three native binaries as assets.

**Alternatives considered**:
- **GitHub CLI (`gh release create`)**: Available on the runner but requires more boilerplate. Rejected — the action does everything in one step.
- **actions/create-release (deprecated)**: No longer maintained. Rejected.

**Rationale**: `action-gh-release` is the de facto standard (17k+ stars), maintained, and handles the common case of creating a release from a tag with file assets.

### Decision 5: Workflow trigger — tag push only

**Choice**: Trigger on `push: tags: ['v*']`. No `workflow_dispatch`, no branch push trigger.

**Alternatives considered**:
- **Push to main → auto-publish**: Would publish on every merge. Rejected — releases should be intentional.
- **workflow_dispatch as primary**: Requires clicking through the GitHub UI. Rejected — tag push is simpler for the maintainer and keeps the version in git history.

**Rationale**: Tag-on-push is the standard release trigger for open-source .NET tools. One command: `git tag v1.3.0 && git push --tags`.

## Risks / Trade-offs

**[Risk] Cross-compilation of win-x64 and osx-arm64 from Linux may fail for future .NET workloads**
→ **Mitigation**: Currently only self-contained single-file publishing, which cross-compiles reliably. If Native AOT is enabled later, a multi-OS matrix will be needed instead (AOT requires native toolchains per platform).

**[Risk] OIDC token exchange fails intermittently**
→ **Mitigation**: The workflow will fail with a clear error from `NuGet/login@v1`. The maintainer can re-push the tag (delete + recreate) to retry. The token is valid for 1 hour, and the push happens immediately after exchange.

**[Risk] Trusted Publishing policy becomes inactive**
→ **Mitigation**: Documented conditions in nuget.org docs: user leaves org, org is deleted, or no publish within 7-day probation period for private repos. Since this is a public repo, the policy should activate permanently on first successful publish.

**[Risk] Version format mismatch**
→ **Mitigation**: Strip only the leading `v` prefix from the tag. Invalid version formats (e.g., `v1.2.3-beta.1`) are valid semver2 and accepted by both NuGet and GitHub. If the tag format is wrong, the workflow fails fast at the `dotnet pack` step with a clear error.

## Migration Plan

1. **One-time setup (manual)**:
   - On nuget.org: Create Trusted Publishing policy (Owner: `ypyl`, Repo: `ai-eval-cli`, Workflow: `publish.yml`)
   - On GitHub: Add repository secret `NUGET_USERNAME` with the nuget.org profile name
2. **Code changes**: Merge the workflow file and `.csproj` change
3. **First release**: Tag the current or next version, push the tag, verify the workflow succeeds
4. **Rollback**: If Trusted Publishing fails, the old manual `dotnet nuget push` with API key still works — nothing is removed, only added

## Open Questions

- **Should Native AOT be enabled later?** It's currently commented out in `.csproj`. If uncommented, the single-runner cross-compilation approach won't work — a multi-OS matrix will be needed. This is tracked as a future decision, not a blocker.
- **Package signing / SBOM?** Not in scope for this change. Can be layered on the workflow later.
