# Overview
This document provides guidelines for using GitHub Copilot to contribute to the .NET MAUI Community Toolkit. It includes instructions on setting up your environment, writing code, and following best practices specific to .NET MAUI.

## Prerequisites
1.	Install the latest stable [.NET SDK](https://dotnet.microsoft.com/en-us/download).
2.	Install .NET MAUI workloads (we recommend using Visual Studio installer).

## Setting Up GitHub Copilot
1.	Ensure you have GitHub Copilot installed and enabled in Visual Studio.
2.	Familiarize yourself with the basic usage of GitHub Copilot by reviewing the [official documentation](https://docs.github.com/en/copilot).

## Writing Code with GitHub Copilot
### General Guidelines
* Use GitHub Copilot to assist with code completion, documentation, and generating boilerplate code.
* Always review and test the code suggested by GitHub Copilot to ensure it meets the project's standards and requirements.

### Specific to .NET MAUI
* Ensure that any UI components or controls are compatible with .NET MAUI.
* Avoid using Xamarin.Forms-specific code unless there is a direct .NET MAUI equivalent.
* Follow the project's coding style and best practices as outlined in the [contributing](https://github.com/CommunityToolkit/Maui/blob/main/CONTRIBUTING.md) document.

### C# File Naming
* Platform-suffix naming is required for files in projects that produce NuGet packages.
* Determine package-producing projects by checking for NuGet metadata in the `.csproj` (for example `PackageId`) and by following the pack targets in `.github/workflows/dotnet-build.yml`.
* For package-producing projects, C# files must use one of these patterns: `*.shared.cs`, `*.net.cs`, `*.ios.cs`, `*.macos.cs`, `*.macios.cs`, `*.android.cs`, `*.windows.cs`, `*.tizen.cs`.
* Projects that do not produce NuGet packages (for example samples, tests, analyzers, and benchmarks) should keep standard `*.cs` naming, ignoring generated patterns like `*.xaml.cs` and `*.Designer.cs`.

## Best Practices
* Use **Trace.WriteLine()** for debug logging instead of **Debug.WriteLine()**.
* Include a **CancellationToken** as a parameter for methods returning **Task** or **ValueTask**.
* Use **is** for null checking and type checking.
* Use file-scoped namespaces to reduce code verbosity.
* Avoid using the **!** null forgiving operator.
* Follow naming conventions for enums and property names.

### Element Positioning (enforced as build warnings)
StyleCop rules SA1201, SA1202, SA1204, SA1214, and SA1215 are reported as **build warnings** via `.editorconfig`. Always write new and modified C# code in the correct order so no warnings are introduced:
* **Within a type, members appear in this order:** fields → constructors → finalizers/destructors → delegates → events → enums → interfaces → properties → indexers → methods.
* **Within each member kind, order by access level:** public → internal → protected internal → protected → private protected → private.
* **Within the same member kind and access level:** static members before instance members; readonly fields before non-readonly fields.
* **Within a file:** usings → namespace → delegates → enums → interfaces → structs → classes; static classes before non-static classes of the same access level.
* To fix violations, reorder the members to comply with these rules, or use the IDE code fix (lightbulb). When a `BindableProperty`/`BindablePropertyKey` static initializer depends on another field, initialization order takes precedence — suppress with a targeted `#pragma warning disable` and a comment explaining why.


### Debug Logging
* Always use `Trace.WriteLine()` instead of `Debug.WriteLine` for debug logging because `Debug.WriteLine` is removed by the compiler in Release builds

### Methods Returning Task and ValueTask
* Always include a `CancellationToken` as a parameter to every method returning `Task` or `ValueTask`
* If the method is public, provide a the default value for the `CancellationToken` (e.g. `CancellationToken token = default`)
* If the method is not public, do not provide a default value for the `CancellationToken`
* If the method is used outside of a .net MAUI control, Use `CancellationToken.ThrowIfCancellationRequested()` to verify the `CancellationToken`, as it is not possible to catch exceptions in XAML.

### Enums
* Always use `Unknown` at index 0 for return types that may have a value that is not known
* Always use `Default` at index 0 for option types that can use the system default option
* Follow naming guidelines for tense... `SensorSpeed` not `SensorSpeeds`
* Assign values (0,1,2,3) for all enums, if not marked with a `Flags` attribute. This is to ensure that the enum can be serialized and deserialized correctly across platforms.

### Property Names
* Include units only if one of the platforms includes it in their implementation. For instance HeadingMagneticNorth implies degrees on all platforms, but PressureInHectopascals is needed since platforms don't provide a consistent API for this.

### Units
* Use the standard units and most well accepted units when possible. For instance Hectopascals are used on UWP/Android and iOS uses Kilopascals so we have chosen Hectopascals.

### Pattern matching
#### Null checking
* Prefer using `is` when checking for null instead of `==`.

e.g.

```csharp
// null
if (something is null)
{

}

// or not null
if (something is not null)
{
   
}
```

* Avoid using the `!` [null forgiving operator](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/null-forgiving) to avoid the unintended introduction of bugs.

#### Type checking
* Prefer `is` when checking for types instead of casting.

e.g.

```csharp
if (something is Bucket bucket)
{
   bucket.Empty();
}
```

### Use collection initializers or expressions
* Use [Use collection initializers or expressions](https://learn.microsoft.com/en-gb/dotnet/fundamentals/code-analysis/style-rules/ide0028) Use collection initializers or expressions.

e.g.

```csharp
List<int> list = [1, 2, 3];
List<int> list = [];
```

### File Scoped Namespaces
* Use [file scoped namespaces](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-10.0/file-scoped-namespaces) to help reduce code verbosity.

e.g.

```csharp
namespace CommunityToolkit.Maui.Converters;

using System;

class BoolToObjectConverter
{
}
```

### Braces
Please use `{ }` after `if`, `for`, `foreach`, `do`, `while`, etc.

e.g.

```csharp
if (something is not null)
{
   ActOnIt();
}
```

### `NotImplementedException`
* Please avoid adding new code that throws a `NotImplementedException`. According to the [Microsoft Docs](https://docs.microsoft.com/dotnet/api/system.notimplementedexception), we should only "throw a `NotImplementedException` exception in properties or methods in your own types when that member is still in development and will only later be implemented in production code. In other words, a NotImplementedException exception should be synonymous with 'still in development.'"
In other words, `NotImplementedException` implies that a feature is still in development, indicating that the Pull Request is incomplete.

### ExpectedFailure Trait for Device Tests
* Use `[Trait("Category", "ExpectedFailure")]` on device tests that are known to fail on certain platforms but are still under investigation. This allows CI to filter them out with `--filter "Category!=ExpectedFailure"` while keeping them runnable locally.
* Do **not** use `[Fact(Skip = "...")]` for platform-specific failures — `Skip` hides the test entirely and it may silently rot. Prefer `[Trait("Category", "ExpectedFailure")]` so the test still runs locally and in unfiltered CI runs.
* Example:
```csharp
[Fact]
[Trait("Category", "ExpectedFailure")]
public void Snackbar_Make_CreatesInstance()
{
    var snackbar = Snackbar.Make("Hello");
    Assert.NotNull(snackbar);
}
```

### Bug Fixes
If you're looking for something to fix, please browse [open issues](https://github.com/CommunityToolkit/Maui/issues).

Follow the style used by the [.NET Foundation](https://github.com/dotnet/runtime/blob/master/docs/coding-guidelines/coding-style.md), with two primary exceptions:

* We do **not** use the `private` keyword as it is the default accessibility level in C#.
* We will **not** use `_` or `s_` as a prefix for internal or private field names
* We will use `camelCaseFieldName` for naming internal or private fields in both instance and static implementations

Read and follow our [Pull Request template](https://github.com/CommunityToolkit/Maui/blob/main/.github/PULL_REQUEST_TEMPLATE.md)

## Device Testing (CommunityToolkit.Maui.DeviceTests)

Device tests live in `src/CommunityToolkit.Maui.DeviceTests` and run inside a real MAUI app on Android, iOS, macOS Catalyst, and Windows. They verify platform-specific behavior (handler creation, platform service interactions, view measurement) that unit tests cannot.

The project uses [DeviceRunners](https://github.com/mattleibow/DeviceRunners) by Matthew Leibowitz — a comprehensive cross-platform device testing framework that enables running tests on real devices across multiple platforms using various testing frameworks. DeviceRunners originated from migrating and modernizing the .NET MAUI team's device testing solutions.

### DeviceRunners Architecture (how it maps to this project)

DeviceRunners is a set of NuGet packages. This project consumes these packages — it does **not** need to build custom test runner infrastructure.

| NuGet Package | Role in This Project |
|---|---|
| `DeviceRunners.Core` | Core abstractions: `ITestDiscoverer`, `ITestRunner`, `IResultChannelManager` |
| `DeviceRunners.VisualRunners.Core` | Visual runner abstractions: result channels, formatters, test events |
| `DeviceRunners.VisualRunners.Maui` | MAUI visual runner UI (pages, view models, diagnostics, app shell). Provides `UseVisualTestRunner()`. |
| `DeviceRunners.VisualRunners.Xunit` | xUnit v2 test discovery and execution adapter. Provides `AddXunit()`. |
| `DeviceRunners.Testing.Targets` | MSBuild targets enabling `dotnet test` for device projects (build → deploy → run → TRX). Provides `AddCliConfiguration()`. |

Do **not** build custom `XunitFrontController` wrappers or `DeviceRunner` classes — DeviceRunners provides discovery, execution, visual runner UI, result collection, and `dotnet test` integration.

### Platform Support

```xml
<TargetFrameworks>net10.0-android;net10.0-ios;net10.0-maccatalyst</TargetFrameworks>
<TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('windows'))">$(TargetFrameworks);net10.0-windows10.0.19041.0</TargetFrameworks>
```

The project uses conditional compilation for platform-specific code:

```csharp
#if ANDROID
    // Android-specific code
    return Android.App.Application.Context.CacheDir.AbsolutePath;
#elif IOS || MACCATALYST
    // iOS/macOS-specific code
    var root = NSBundle.MainBundle.BundlePath;
#elif WINDOWS
    // Windows-specific code
    return AppContext.BaseDirectory;
#endif
```

### Project configuration

**`MauiProgram.cs`** — register test assemblies and the test framework adapter:

```csharp
using DeviceRunners.VisualRunners;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseVisualTestRunner(conf => conf
                .AddCliConfiguration()
                .AddConsoleResultChannel()
                .AddTestAssembly(typeof(MauiProgram).Assembly)
                .AddXunit())
            .ConfigureFonts(fonts => { ... });
        return builder.Build();
    }
}
```

- `AddCliConfiguration()` enables `dotnet test` support — reads env vars / CLI args for auto-start and TCP result streaming. When running interactively from the IDE (no env vars present), it is a no-op and the visual runner behaves normally.
- `AddXunit()` registers the xUnit v2 test discoverer and runner. The project uses **xunit v2 (2.9.3)** — not xunit v3.
- `AddTestAssembly(...)` tells the runner which assemblies contain tests.
- `AddConsoleResultChannel()` writes pass/fail results to the console / trace log.
- Do **not** call `builder.UseMauiApp<App>()` — DeviceRunners registers its own `VisualRunnerApp` via `UseVisualTestRunner`.

**`.csproj`** — required packages:

```xml
<PackageReference Include="DeviceRunners.VisualRunners.Maui" Version="0.1.0-preview.12" />
<PackageReference Include="DeviceRunners.VisualRunners.Xunit" Version="0.1.0-preview.12" />
<PackageReference Include="DeviceRunners.Testing.Targets" Version="0.1.0-preview.12" />
<PackageReference Include="Microsoft.Maui.Controls" Version="$(MauiPackageVersion)" />
<PackageReference Include="xunit" Version="2.9.3" />
```

Also required: `<GenerateTestingPlatformEntryPoint>false</GenerateTestingPlatformEntryPoint>` because the MAUI app defines its own `Program.Main`. No custom NuGet feeds are needed — all packages are on NuGet.org.

### Writing device tests

Device test classes are plain xUnit `[Fact]`/`[Theory]` classes in `Tests/` organized by area (`Behaviors/`, `Converters/`, `Views/`, `Core/`, `Camera/`, `Maps/`, `MediaElement/`, etc.):

```csharp
namespace CommunityToolkit.Maui.DeviceTests.Tests.Behaviors;

public class MyBehaviorTests
{
    [Fact]
    public void MyBehavior_DefaultValue_IsCorrect()
    {
        var behavior = new MyBehavior();
        Assert.Equal(expectedValue, behavior.SomeProperty);
    }
}
```

#### Main thread requirement

**Android requires all view hierarchy operations on the main thread.** Tests that construct platform behaviors (e.g., `StatusBarBehavior`), create `Page` instances, add behaviors to pages, or call `element.ToHandler(context)` **must** run on the main thread via `MainThread.InvokeOnMainThreadAsync`:

```csharp
[Fact]
public async Task Behavior_CanBeAttachedToPage()
{
    await MainThread.InvokeOnMainThreadAsync(() =>
    {
        var page = new ContentPage();
        var behavior = new StatusBarBehavior { StatusBarColor = Colors.Fuchsia };
        page.Behaviors.Add(behavior);
        Assert.Single(page.Behaviors.OfType<StatusBarBehavior>());
    });
}
```

Tests that only set/get CLR properties (no platform view interaction) do **not** need main-thread dispatch:

```csharp
[Fact]
public void Behavior_DefaultColor_IsTransparent()
{
    var behavior = new StatusBarBehavior();
    Assert.Equal(Colors.Transparent, behavior.StatusBarColor);  // no platform touch
}
```

#### Handler creation

Create handlers directly via `element.ToHandler(context)` using the app's `MauiContext`. Do **not** replace `window.Page` to force handler creation — DeviceRunners manages its own page:

```csharp
[Fact]
public async Task LabelHandlerIsCreated()
{
    var label = new Label { Text = "Test" };
    var handler = await MainThread.InvokeOnMainThreadAsync(() =>
    {
        var context = Application.Current?.Handler?.MauiContext;
        Assert.NotNull(context);
        return label.ToHandler(context);
    });
    Assert.NotNull(handler.PlatformView);
}
```

### Running tests

**Visual runner (IDE / interactive debugging):**
```bash
dotnet build src/CommunityToolkit.Maui.DeviceTests/CommunityToolkit.Maui.DeviceTests.csproj -f net10.0-android -t:Run
```
Launches the app with the DeviceRunners visual runner UI showing pass/fail counts, per-test details, and diagnostics.

**`dotnet test` (CI / headless, recommended):**
```bash
dotnet test src/CommunityToolkit.Maui.DeviceTests/CommunityToolkit.Maui.DeviceTests.csproj -f net10.0-android
dotnet test ... -f net10.0-android --filter "FullyQualifiedName~StatusBarBehavior"
```
`DeviceRunners.Testing.Targets` hooks into `dotnet test` to build, deploy, run, and collect TRX results automatically. Filter with standard `--filter` syntax.

How `dotnet test` works under the hood:
1. **Build** — The app is compiled for the target platform (APK, .app bundle, .exe)
2. **Deploy** — The DeviceRunners CLI tool installs the app on the device/simulator
3. **Launch** — The app starts with configuration (env vars or CLI args) that tells it to auto-run tests and connect back via TCP
4. **Collect** — The CLI listens on a TCP port for NDJSON test events and writes a TRX file
5. **Report** — Results are parsed and reported in the standard `dotnet test` format

MSBuild properties for configuration (set via `-p:` or in `.csproj`):

| Property | Default | Description |
|---|---|---|
| `DeviceRunnersPort` | 16384 | TCP port for test result collection |
| `DeviceRunnersConnectionTimeout` | 120 | Seconds to wait for the app to connect |
| `DeviceRunnersDevice` | (auto) | Target device ID |

### Platform-specific test files

DeviceTests is **not** a NuGet-packaged project, so standard `*.cs` naming is used. For platform-specific tests, use `#if` directives:

```csharp
#if ANDROID
[Fact]
public void PlatformIsAndroid() => Assert.True(OperatingSystem.IsAndroid());
#elif IOS
[Fact]
public void PlatformIsIOS() => Assert.True(OperatingSystem.IsIOS());
#elif MACCATALYST
[Fact]
public void PlatformIsMacCatalyst() => Assert.True(OperatingSystem.IsMacCatalyst());
#elif WINDOWS
[Fact]
public void PlatformIsWindows() => Assert.True(OperatingSystem.IsWindows());
#endif
```

### Key DeviceRunners Interfaces

These are provided by DeviceRunners — tests can reference them when extending the runner:

```csharp
// Core testing interfaces (in DeviceRunners.Core)
public interface ITestDiscoverer
{
    Task<IEnumerable<TestAssemblyInfo>> DiscoverTestsAsync(IEnumerable<string> sources);
}

public interface ITestRunner
{
    Task<TestRunSummary> RunTestsAsync(IEnumerable<TestCase> testCases);
}

public interface IResultChannelManager
{
    Task SendResultsAsync(TestResult result);
}

// Platform abstractions (in DeviceRunners.VisualRunners.Core)
public interface IAppTerminator
{
    Task TerminateAsync();
}

public interface IDiagnosticsManager
{
    Task<DiagnosticData> CollectDiagnosticsAsync();
}
```

### Common Scenarios for Copilot Assistance

#### When adding new device tests
- Create a plain xUnit `[Fact]`/`[Theory]` class in `Tests/{Area}/`
- If the test touches platform views/behaviors, use `async Task` + `MainThread.InvokeOnMainThreadAsync`
- If the test only checks CLR properties, use synchronous `void`
- For handler creation, use `element.ToHandler(context)` with `Application.Current?.Handler?.MauiContext`

#### When adding platform-specific tests
- Use `#if ANDROID` / `#elif IOS` / `#elif MACCATALYST` / `#elif WINDOWS` directives
- Use `#if` guards at the method level, not the class level
- Standard `*.cs` naming (DeviceTests is not a NuGet-packaged project)

#### When debugging test failures
- Android: Check `adb logcat` for `[DeviceRunners]` / `[FAIL]` trace messages
- The visual runner's Diagnostics page shows assembly paths, environment, and runner logs
- Use `--filter "FullyQualifiedName~TestName"` with `dotnet test` to isolate a single test

#### When working with MAUI integration
- Use MAUI service registration patterns via `builder.Services`
- Follow MAUI lifecycle management (tests run after app startup)
- Test across all target platforms before submitting

### Dos and Don'ts

**Do:**
- Use `MainThread.InvokeOnMainThreadAsync` for any test that constructs or touches platform views/behaviors
- Use `element.ToHandler(context)` for handler creation
- Use `Trace.WriteLine()` (not `Debug.WriteLine()`) for logging
- Add tests in `Tests/` organized by area (`Behaviors/`, `Converters/`, `Views/`, etc.)
- Use `xunit` v2 `[Fact]` and `[Theory]` attributes
- Use `dotnet test` for CI/headless runs; use `dotnet build -t:Run` for interactive debugging

**Don't:**
- Don't build custom `DeviceRunner` or `XunitFrontController` wrappers — DeviceRunners handles this
- Don't call `builder.UseMauiApp<App>()` in `MauiProgram.cs` — DeviceRunners registers its own `VisualRunnerApp`
- Don't replace `window.Page` to force handler creation
- Don't use `xunit.runner.utility` directly — DeviceRunners manages test execution
- Don't add `NuGet.config` with the `dotnet-eng` feed — DeviceRunners packages are on NuGet.org
- Don't use `NotImplementedException` — implement the feature or use `NotSupportedException`

### Historical Context

DeviceRunners consolidates and modernizes several earlier testing solutions:
- **xunit/devices.xunit** — Migrated to .NET MAUI with separated UI components
- **xunit/uitest.xunit** — Migrated to .NET MAUI
- **nunit/nunit.xamarin** — Migrated to .NET MAUI with individual test support
- **dotnet/maui** — Temporary hosting during migration; the .NET MAUI team now recommends DeviceRunners for device testing

The architecture reflects lessons learned from these migrations, emphasizing modularity, cross-platform support, and separation of concerns between test execution and user interfaces. This project adopts DeviceRunners as its device testing infrastructure, replacing the earlier custom `XunitFrontController`-based runner.

## Submitting Contributions
1.	Fork the repository and create a new branch for your changes.
2.	Implement your changes using GitHub Copilot as needed.
3.	Ensure your changes include tests, samples, and documentation.
4.	Open a pull request and follow the [Pull Request template](https://github.com/CommunityToolkit/Maui/blob/main/.github/PULL_REQUEST_TEMPLATE.md).

## Additional Resources
* [GitHub Copilot Documentation](https://docs.github.com/en/copilot)
* [.NET MAUI Documentation](https://learn.microsoft.com/en-us/dotnet/maui/)

By following these guidelines, you can effectively use GitHub Copilot to contribute to the .NET MAUI Community Toolkit. Thank you for your contributions!
