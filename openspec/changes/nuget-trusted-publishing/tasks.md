## 1. .csproj change — remove hardcoded version

- [x] 1.1 Remove `<Version>1.1.1</Version>` from `src/AiEvalCli/AiEvalCli.csproj` so the git tag becomes the sole version source of truth

## 2. Create the publish workflow

- [x] 2.1 Create `.github/workflows/publish.yml` triggered on `push: tags: ['v*']`
- [x] 2.2 Add job-level `permissions: id-token: write, contents: write` (OIDC + releases)
- [x] 2.3 Add `actions/checkout@v4` to fetch the full repo at the tag ref
- [x] 2.4 Add `actions/setup-dotnet@v4` with .NET 10 SDK
- [x] 2.5 Add tag version extraction step: strip `v` prefix from `github.ref_name` into `VERSION` env var
- [x] 2.6 Add three `dotnet publish` steps for linux-x64, win-x64, osx-arm64 (self-contained single-file, `-p:Version=$VERSION`)
- [x] 2.7 Add `dotnet pack` step producing `.nupkg` to `./artifacts/` with `-p:Version=$VERSION`
- [x] 2.8 Add `NuGet/login@v1` step with `user: ${{ secrets.NUGET_USERNAME }}` for OIDC token exchange
- [x] 2.9 Add `dotnet nuget push` step using `${{ steps.login.outputs.NUGET_API_KEY }}` and `https://api.nuget.org/v3/index.json`
- [x] 2.10 Add `softprops/action-gh-release@v2` step attaching all three native binary outputs as release assets

## 3. Documentation

- [x] 3.1 Update README's "Install" section to reference GitHub Releases as the download source for native binaries (replace placeholder "Download the native binary... from releases" with concrete instructions)
- [x] 3.2 Add a "Publishing" section to README documenting the tag-driven release process and one-time nuget.org/GitHub setup steps

## 4. Verification

- [ ] 4.1 Push a test tag (e.g., `v1.1.2`) and verify the workflow builds all three platform binaries successfully
- [ ] 4.2 Verify the NuGet push completes via OIDC token exchange (no API key used)
- [ ] 4.3 Verify the GitHub Release is created with all three native binary assets attached
- [ ] 4.4 Verify `dotnet tool install --global eval-cli --version 1.1.2` succeeds from the published package
