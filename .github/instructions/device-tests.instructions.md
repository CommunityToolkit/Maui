---
description: 'Building and running device tests for the .NET MAUI Community Toolkit: the XHarness/XunitFrontController test runner, handler creation without page navigation, and avoiding infinite re-run loops'
applyTo: 'src/CommunityToolkit.Maui.DeviceTests/**/*.cs'
---

## Device Tests

Device tests live in `src/CommunityToolkit.Maui.DeviceTests` and run inside a real MAUI application on a target platform (Windows, Android, iOS, MacCatalyst). They verify platform-specific behavior that unit tests cannot.

### Test runner architecture

The runner mirrors the [dotnet/maui](https://github.com/dotnet/maui) team's approach:

- **`DeviceRunner`** discovers and executes tests directly through xunit's `XunitFrontController` (from the `xunit.runner.utility` package). Do **not** load the XHarness runner types via reflection (`Activator.CreateInstance` for `XUnitTestRunner`, or `XmlResultJargon` for results) — that fails silently and tests never run.
- Results are counted per-test from the execution sink messages (`ITestPassed`, `ITestFailed`, `ITestSkipped`). `ITestAssemblyFinished` does not expose a summary in the xunit version in use.
- The execution sink implements `IExecutionSink` directly (including `OnMessageWithTypes`) and must be marked `partial` to satisfy the CsWinRT analyzer on Windows (CsWinRT1028). The outer `DeviceRunner` must also be `partial` for the same reason.

### Creating handlers in tests

When a test needs a handler/platform view, create it directly with `element.ToHandler(context)` using the application's `MauiContext` — do **not** replace `window.Page` to force handler creation:

```csharp
var context = Application.Current?.Handler?.MauiContext;
var handler = element.ToHandler(context);
```

Replacing `window.Page` hides the visual test runner page (leaving stray content such as a "Click Me" button) and forces the runner to restore the page afterward. Handler creation must run on the main thread (`MainThread.InvokeOnMainThreadAsync`).

### Run tests exactly once

`VisualRunnerPage` runs tests from `OnAppearing`. Anything that reassigns `window.Page` (or otherwise re-shows the page) re-fires `OnAppearing` and can cause an infinite re-run loop. Guard with a `hasRun` flag so the suite executes a single time per app launch.

### Packages and feeds

- xunit **v2** (`2.9.3`) is required — XHarness is not compatible with xunit v3.
- XHarness packages (`Microsoft.DotNet.XHarness.TestRunners.Xunit`) come from the `dotnet-eng` Azure DevOps NuGet feed configured in `NuGet.config`, not nuget.org.

### Running

```bash
dotnet build src/CommunityToolkit.Maui.DeviceTests/CommunityToolkit.Maui.DeviceTests.csproj -f net10.0-windows10.0.19041.0 -t:Run
```

The visual runner displays results in the app UI and reports pass/fail counts to the trace log.
