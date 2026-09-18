# CommunityToolkit.Maui.DeviceTests

Device tests for the .NET MAUI Community Toolkit. These tests run on actual devices/emulators and verify platform-specific behavior that cannot be tested with unit tests alone.

This project uses [DeviceRunners](https://github.com/mattleibow/DeviceRunners) by Matthew Leibowitz, the same test-runner infrastructure recommended by the .NET MAUI team. Tests are discovered and executed through `DeviceRunners.VisualRunners.Xunit`, with a built-in visual runner UI and `dotnet test` support via `DeviceRunners.Testing.Targets`.

## Prerequisites

- .NET SDK with MAUI workloads installed (`dotnet workload install maui`)
- For Android: Android emulator or physical device connected
- For iOS/MacCatalyst: macOS with Xcode installed
- For Windows: Windows 10/11 with Windows App SDK

## Running Tests

### Visual Runner (IDE / Interactive)

Launch the app like any other MAUI app — F5 in Visual Studio / VS Code, or:

```bash
dotnet build src/CommunityToolkit.Maui.DeviceTests/CommunityToolkit.Maui.DeviceTests.csproj -f net10.0-android -t:Run
```

The **DeviceRunners visual runner** UI displays test results with pass/fail counts, per-test details, and diagnostics.

### `dotnet test` (CI / Headless, Recommended)

```bash
dotnet test src/CommunityToolkit.Maui.DeviceTests/CommunityToolkit.Maui.DeviceTests.csproj -f net10.0-android
dotnet test src/CommunityToolkit.Maui.DeviceTests/CommunityToolkit.Maui.DeviceTests.csproj -f net10.0-ios
dotnet test src/CommunityToolkit.Maui.DeviceTests/CommunityToolkit.Maui.DeviceTests.csproj -f net10.0-maccatalyst
dotnet test src/CommunityToolkit.Maui.DeviceTests/CommunityToolkit.Maui.DeviceTests.csproj -f net10.0-windows10.0.19041.0
```

The `DeviceRunners.Testing.Targets` package hooks into `dotnet test` to build, deploy, run, and collect TRX results automatically. Filter tests with `--filter`:

```bash
dotnet test ... -f net10.0-android --filter "FullyQualifiedName~StatusBarBehavior"
```

## Architecture

Powered by [DeviceRunners](https://github.com/mattleibow/DeviceRunners):

- **`DeviceRunners.VisualRunners.Maui`** — MAUI visual runner UI (pages, view models, diagnostics)
- **`DeviceRunners.VisualRunners.Xunit`** — xUnit v2 test discovery and execution adapter
- **`DeviceRunners.Testing.Targets`** — MSBuild targets enabling `dotnet test` for device projects
- **`DeviceRunners.Core`** / **`DeviceRunners.VisualRunners.Core`** — Core abstractions (test runners, result channels, formatters)

Configured in `MauiProgram.cs`:

```csharp
builder.UseVisualTestRunner(conf => conf
    .AddCliConfiguration()
    .AddConsoleResultChannel()
    .AddTestAssembly(typeof(MauiProgram).Assembly)
    .AddXunit());
```

## Project Structure

```text
CommunityToolkit.Maui.DeviceTests/
├── Platforms/
│   ├── Android/              # Android-specific app entry points
│   ├── iOS/                  # iOS-specific app entry points
│   ├── MacCatalyst/          # MacCatalyst-specific app entry points
│   └── Windows/              # Windows-specific app entry points
├── Resources/
│   ├── AppIcon/              # App icon assets
│   └── Splash/               # Splash screen assets
├── Tests/                    # Device test classes organized by area
│   ├── Additional/           # Additional cross-cutting tests
│   ├── Behaviors/            # Behavior tests
│   ├── Camera/               # Camera package tests
│   ├── Converters/           # Converter tests
│   ├── Core/                 # Core package tests (primitives, layouts, extensions, essentials)
│   ├── Extensions/           # Internal extension tests
│   ├── Maps/                 # Maps package tests
│   ├── MediaElement/         # MediaElement package tests
│   └── Views/                # View tests
├── Properties/
│   └── launchSettings.json
├── MauiProgram.cs            # App builder / DI configuration
├── GlobalUsings.cs           # Assembly-level xunit configuration
├── SmokeTests.cs             # Basic app boot verification tests
├── PlatformDetectionTests.cs # Platform detection tests
└── HandlerTests.cs           # Handler creation tests
```

## Adding New Tests

1. Create a new test class in the `Tests/` folder (organized by area)
2. Use `[Fact]` or `[Theory]` attributes from xunit v2
3. For platform-specific tests, use `#if` directives or create `*.android.cs` / `*.ios.cs` / `*.windows.cs` / `*.macios.cs` files
4. Tests run sequentially (parallelization is disabled) to avoid UI threading issues

## Notes

- Tests run inside a real MAUI application on the target platform. DeviceRunners handles test discovery, execution, and result collection — do **not** build custom `XunitFrontController` wrappers or `DeviceRunner` classes.
- The app displays a visual test runner page while tests execute
- This project does NOT produce a NuGet package — it is test-only
- All NuGet packages (including transitive XHarness dependencies) come from nuget.org; no custom feeds are required
