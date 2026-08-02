# CommunityToolkit.Maui.DeviceTests

Device tests for the .NET MAUI Community Toolkit. These tests run on actual devices/emulators and verify platform-specific behavior that cannot be tested with unit tests alone.

This project uses the same test-runner approach as the .NET MAUI team: tests are discovered and executed directly through xunit's `XunitFrontController` (from the `xunit.runner.utility` package), with no reflection-based runner loading.

## Prerequisites

- .NET SDK with MAUI workloads installed (`dotnet workload install maui`)
- For Android: Android emulator or physical device connected
- For iOS/MacCatalyst: macOS with Xcode installed
- For Windows: Windows 10/11 with Windows App SDK

## Running Tests Locally

When the app launches, the **visual test runner** (`VisualRunnerPage`) automatically discovers and executes all tests in the assembly via `DeviceRunner`, which drives xunit directly through `XunitFrontController`. Results are displayed in the UI.

### Windows

```bash
dotnet build src/CommunityToolkit.Maui.DeviceTests/CommunityToolkit.Maui.DeviceTests.csproj -f net10.0-windows10.0.19041.0 -t:Run
```

Or press F5 in Visual Studio / VS Code with the Windows Machine profile selected.

### Android

```bash
# Ensure an emulator is running or a device is connected
dotnet build src/CommunityToolkit.Maui.DeviceTests/CommunityToolkit.Maui.DeviceTests.csproj -f net10.0-android -t:Run
```

### iOS (macOS only)

```bash
# Ensure a simulator is available
dotnet build src/CommunityToolkit.Maui.DeviceTests/CommunityToolkit.Maui.DeviceTests.csproj -f net10.0-ios -t:Run
```

### MacCatalyst (macOS only)

```bash
dotnet build src/CommunityToolkit.Maui.DeviceTests/CommunityToolkit.Maui.DeviceTests.csproj -f net10.0-maccatalyst -t:Run
```

## Running Tests Headlessly (CI)

The `HeadlessRunnerService` is registered in DI and can be used for automated CI pipelines. It runs all tests without UI interaction via `DeviceRunner` and reports pass/fail counts to the trace log.

The headless runner is configured in `MauiProgram.cs`:

```csharp
builder.UseHeadlessRunner(new HeadlessRunnerOptions
{
    RequiresUIContext = true,
});
```

To run headlessly from a CI script, resolve `HeadlessRunnerService` from the service provider and call `RunTestsAsync()`. It returns a non-zero exit code when any test fails, and writes the pass/fail summary plus failure details to the trace log.

## Architecture

The test runner follows the pattern established by the [dotnet/maui](https://github.com/dotnet/maui) team:

- **`TestOptions`** — Configuration for test assemblies and skip categories
- **`HeadlessRunnerOptions`** — Configuration for CI/automated headless execution
- **`DeviceRunner`** — Discovers and executes xunit tests via `XunitFrontController` (no reflection)
- **`VisualRunnerPage`** — In-app visual test runner that displays results in the UI
- **`HeadlessRunnerService`** — Headless runner service for automated CI pipelines
- **`AppBuilderExtensions`** — `ConfigureTests()`, `UseVisualRunner()`, `UseHeadlessRunner()` extension methods

Tests are discovered and executed directly through `XunitFrontController` from the `xunit.runner.utility` package, mirroring the `DeviceRunner` used by the [dotnet/maui](https://github.com/dotnet/maui) team. This avoids reflection-based runner loading so tests reliably execute on all platforms.

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
├── Runners/
│   ├── AppBuilderExtensions.cs   # ConfigureTests / UseVisualRunner / UseHeadlessRunner
│   ├── DeviceRunner.cs           # XunitFrontController-based test discovery & execution
│   ├── HeadlessRunnerService.cs  # Headless CI test runner
│   └── VisualRunnerPage.cs       # In-app visual test runner
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
├── App.cs                    # MAUI Application class
├── MauiProgram.cs            # App builder / DI configuration
├── TestOptions.cs            # Test configuration options
├── HeadlessRunnerOptions.cs  # Headless runner configuration
├── GlobalUsings.cs           # Assembly-level xunit configuration
├── SmokeTests.cs             # Basic app boot verification tests
├── PlatformDetectionTests.cs # Platform detection tests
├── HandlerTests.cs           # Handler creation tests
└── NuGet.config              # NuGet feed for XHarness packages (dotnet-eng)
```

## Adding New Tests

1. Create a new test class in the `Tests/` folder (organized by area)
2. Use `[Fact]` or `[Theory]` attributes from xunit v2
3. For platform-specific tests, use `#if` directives or create `*.android.cs` / `*.ios.cs` / `*.windows.cs` / `*.macios.cs` files
4. Tests run sequentially (parallelization is disabled) to avoid UI threading issues

## Notes

- Tests run inside a real MAUI application on the target platform via `XunitFrontController`
- The app displays a visual test runner page while tests execute
- This project does NOT produce a NuGet package — it is test-only
- The XHarness packages are sourced from the `dotnet-eng` Azure DevOps NuGet feed (not nuget.org)
