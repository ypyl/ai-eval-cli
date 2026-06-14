## ADDED Requirements

### Requirement: eval-cli is installable as a dotnet tool
The project SHALL support installation via `dotnet tool install` by including `PackAsTool` and required NuGet metadata in the project file.

#### Scenario: Global tool installation
- **WHEN** a user runs `dotnet tool install --global eval-cli`
- **THEN** the `eval-cli` command becomes available system-wide, accepting the same CLI arguments as the native binary

#### Scenario: Local tool installation with manifest
- **WHEN** a user runs `dotnet tool install eval-cli` in a repository with a tool manifest
- **THEN** the tool is installed locally and pinned in `.config/dotnet-tools.json`

#### Scenario: Tool update
- **WHEN** a user runs `dotnet tool update --global eval-cli`
- **THEN** the tool updates to the latest package version from the configured NuGet source

### Requirement: Dotnet tool preserves native binary build
Adding dotnet tool packaging SHALL NOT alter the existing `dotnet publish` output for native self-contained binaries.

#### Scenario: Native binary publish unchanged
- **WHEN** the project is built with `dotnet publish -r win-x64 -c Release`
- **THEN** a self-contained single-file native binary is produced, identical in behavior to before the tool packaging change

#### Scenario: Dotnet pack produces only tool package
- **WHEN** the project is built with `dotnet pack -c Release`
- **THEN** a framework-dependent NuGet package is produced containing IL assemblies, without native runtime assets

### Requirement: CLI interface is identical across distribution modes
The dotnet tool SHALL accept the same command-line arguments, produce the same output formats, and use the same evaluators as the native binary.

#### Scenario: Arguments consistent
- **WHEN** `eval-cli --endpoint <url> --model <name> --input scenarios.json` is invoked via the dotnet tool
- **THEN** all arguments are parsed identically to the native binary, producing the same evaluation output

#### Scenario: Provider support consistent
- **WHEN** the dotnet tool is invoked with `--provider azure` or `--provider openai`
- **THEN** Azure OpenAI and OpenAI-compatible providers are supported identically to the native binary

### Requirement: Package metadata is complete
The NuGet package SHALL include sufficient metadata for discovery and compliance.

#### Scenario: Package metadata present
- **WHEN** the package is published to a NuGet feed
- **THEN** it includes PackageId (`eval-cli`), Description, License (MIT), and links back to the project repository
