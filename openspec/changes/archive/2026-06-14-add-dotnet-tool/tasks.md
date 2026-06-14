## 1. Project file changes

- [x] 1.1 Add `<PackAsTool>true</PackAsTool>` to `src/AiEvalCli/AiEvalCli.csproj`
- [x] 1.2 Add NuGet metadata: `<PackageId>eval-cli</PackageId>`, `<Version>`, `<Description>`, `<PackageLicenseExpression>MIT</PackageLicenseExpression>`, `<PackageReadmeFile>README.md</PackageReadmeFile>`
- [x] 1.3 Include README.md in the package via `<None Include="..\..\README.md" Pack="true" PackagePath="\" />`

## 2. Verify dual packaging

- [x] 2.1 Run `dotnet pack -c Release` and confirm `eval-cli.<version>.nupkg` is produced with `tools/net10.0/any/` structure and `DotnetToolSettings.xml`
- [x] 2.2 Run `dotnet publish -c Release -r win-x64` and confirm native binary is produced unchanged
- [x] 2.3 Install the local package with `dotnet tool install --global --add-source ./bin/Release eval-cli` and verify `eval-cli --help` outputs the help text
- [x] 2.4 Uninstall the test tool with `dotnet tool uninstall --global eval-cli`

## 3. Documentation

- [x] 3.1 Update `README.md` to document dotnet tool installation as an alternative alongside native binary download
- [x] 3.2 Add a note that dotnet tool requires .NET 10 runtime, with a link to the native binary for non-.NET users
