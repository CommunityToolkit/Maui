---
description: 'Building and running device tests for the .NET MAUI Community Toolkit: the DeviceRunners test runner, handler creation without page navigation, and avoiding infinite re-run loops'
applyTo: 'src/CommunityToolkit.Maui.DeviceTests/**/*.cs'
---

## Device Tests

Device tests live in `src/CommunityToolkit.Maui.DeviceTests` and run inside a real MAUI application on a target platform (Windows, Android, iOS, MacCatalyst). They verify platform-specific behavior that unit tests cannot.

### Test runner architecture

The project uses [DeviceRunners](https://github.com/mattleibow/DeviceRunners) by Matthew Leibowitz — the same infrastructure recommended by the .NET MAUI team:

- **`DeviceRunners.VisualRunners.Maui`** provides the visual runner UI, pages, view models, and diagnostics.
- **`DeviceRunners.VisualRunners.Xunit`** provides xUnit v2 test discovery and execution.
- **`DeviceRunners.Testing.Targets`** provides the headless `VSTest` target for device projects (TRX results, filtering, CI integration).
- Tests are registered via `builder.UseVisualTestRunner(conf => conf.AddTestAssembly(...).AddXunit())` in `MauiProgram.cs`.

Do **not** build a custom `DeviceRunner`/`XunitFrontController` wrapper — DeviceRunners handles discovery, execution, result collection, and diagnostics.

### Creating handlers in tests

When a test needs a handler/platform view, create it directly with `element.ToHandler(context)` using the application's `MauiContext` — do **not** replace `window.Page` to force handler creation:

```csharp
var context = Application.Current?.Handler?.MauiContext;
var handler = element.ToHandler(context);
```

Handler creation must run on the main thread (`MainThread.InvokeOnMainThreadAsync`).

### Platform behavior tests

Tests that construct platform behaviors (e.g., `StatusBarBehavior`) or add behaviors to a `Page` must also run on the main thread, because Android requires all view hierarchy operations on the main thread:

```csharp
[Fact]
public async Task StatusBarBehavior_CanBeAttachedToPage()
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

### Running

**Visual Runner (IDE / Interactive):**
```bash
dotnet build src/CommunityToolkit.Maui.DeviceTests/CommunityToolkit.Maui.DeviceTests.csproj -f net10.0-android -t:Run
```

**`VSTest` target (CI / Headless, Recommended):**
```bash
dotnet build src/CommunityToolkit.Maui.DeviceTests/CommunityToolkit.Maui.DeviceTests.csproj -t:VSTest -f net10.0-android
dotnet build ... -t:VSTest "-p:VSTestTestCaseFilter=FullyQualifiedName~StatusBarBehavior"
```

The `DeviceRunners.Testing.Targets` package replaces the `VSTest` target to build, deploy, run, and collect TRX results automatically.

Do **not** use `dotnet test` for this project: `global.json` selects Microsoft.Testing.Platform, and `dotnet test` then rejects VSTest-based projects such as this one.

### Packages

- xunit **v2** (`2.9.3`) is required.
- DeviceRunners packages are from NuGet.org (`DeviceRunners.VisualRunners.Maui`, `DeviceRunners.VisualRunners.Xunit`, `DeviceRunners.Testing.Targets` at `0.1.0-preview.12`).
- No custom NuGet feeds are needed — the `NuGet.config` for `dotnet-eng` has been removed.
