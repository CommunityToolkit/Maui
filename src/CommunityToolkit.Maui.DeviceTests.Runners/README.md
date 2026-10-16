# CommunityToolkit.Maui.DeviceTests.Runners

dotnet/maui's device-test runner, vendored from [dotnet/maui](https://github.com/dotnet/maui) at commit
`2067dd42c6032fbc40d2e46245806679a85df5e6` (main, 2026-10-02) under the MIT License (`LICENSE-dotnet-maui.txt`).
dotnet/maui does not publish its runner as a NuGet package. Every copied file starts with a header that names its original
path and lists each toolkit change. The authoring guide for the tests is
[`../CommunityToolkit.Maui.DeviceTests/README.md`](../CommunityToolkit.Maui.DeviceTests/README.md).

| Here | dotnet/maui |
|---|---|
| `CommunityToolkit.Maui.DeviceTests.Runners/` | `src/TestUtils/src/DeviceTests.Runners/` |
| `CommunityToolkit.Maui.DeviceTests.Runners.SourceGen/` | `src/TestUtils/src/DeviceTests.Runners.SourceGen/` |
| `CommunityToolkit.Maui.DeviceTests.Runners.props` / `.targets` | `src/Microsoft.Maui.TestUtils.DeviceTests.Runners.props` / `.targets` |
| `../CommunityToolkit.Maui.DeviceTests.Shared/dotnet-maui/` | `src/TestUtils/src/DeviceTests/`, `src/TestUtils/src/TestShared/`, `src/Core/tests/DeviceTests.Shared/` (public-API subset, some files ported) |

## What it does

- **Headless runner**: used by CI. XHarness starts the app, the tests run with the XHarness xUnit runner, and the results are
  written as xUnit v2 XML.
  - Android: the `<ApplicationId>.TestInstrumentation` instrumentation writes `testResults.xml`.
  - iOS and Mac Catalyst: `xharness apple test` launches the app and collects `xunit-test-*.xml`.
  - Windows: `<App>.exe "<results.xml>"` writes the results to that path and then exits. The visual runner's home page reads
    the command line, the same way dotnet/maui does. Two other forms exist: `<App>.exe "<results.xml>" -1` writes the test
    categories to `devicetestcategories.txt` next to the results path, and `<App>.exe "<results.xml>" <index>` runs only the
    category on that line (0-based) and writes `<results>_<Category>.xml`.
- **Visual runner**: the app started without arguments. It lists the test assemblies and runs tests by hand.
- **Source generator**: generates each app's platform entry points from `ApplicationId`, `ApplicationTitle` and
  `RootNamespace`:
  - Android: `MainApplication`, `MainActivity`, and `TestActivity` + `TestInstrumentation` with fixed Java names.
  - iOS and Mac Catalyst: `Program.Main`, which picks the headless or the visual app delegate.

## Toolkit changes to the vendored files

- **Names**: the assemblies are `CommunityToolkit.Maui.DeviceTests.Runners(.SourceGen)` and the namespaces
  `CommunityToolkit.Maui.DeviceTests.Runners*`. `Microsoft.Maui.dll` grants `[InternalsVisibleTo]` to dotnet/maui's assembly
  names, so keeping those names would compile silently against MAUI internals. Everything here uses public MAUI API only.
- **Dispatcher**: `TestDispatcher.Current` resolves `TestApplicationDispatcher`, which is toolkit code, instead of MAUI's
  internal `ApplicationDispatcher`. `AppHostBuilderExtensions.ConfigureTests` registers it.
- **Platform folders**: `HeadlessRunner/Android|iOS|MacCatalyst|Windows` became `HeadlessRunner/android|macios|windows`, so
  the repository's `Directory.Build.targets` filters them. MSBuild matches globs case-insensitively on macOS and Windows, so
  without the rename the repository's iOS-only `ios/` rule would drop the iOS runner from the Mac Catalyst build.
- **UIScene life cycle (iOS 27 SDK)**: apps linked against the iOS 27 SDK are killed at launch unless they adopt UIScene.
  `MauiTestApplicationDelegate` therefore checks the scene manifest on iOS too, where upstream checks it on Mac Catalyst only,
  and `MauiTestSceneDelegate` is compiled for iOS as well. Each app declares a `UIApplicationSceneManifest` and a `SceneDelegate`.
- **`IPlatformApplication`**: `MauiTestApplicationDelegate` implements `IPlatformApplication` and sets
  `IPlatformApplication.Current`, as `MauiUIApplicationDelegate` does. Toolkit code such as `CameraViewHandler` and
  `PopupService` resolves services through it. Android (`MauiApplication`) and Windows already set it.
- **Windows**: the internal `FileSystemUtils.PlatformGetFullAppPackageFilePath` is replaced by `AppContext.BaseDirectory`
  (`VisualRunner/DeviceRunner.cs`, `HeadlessRunner/windows/ControlsHeadlessTestRunner.cs`), and `UI.Xaml` is written as
  `Microsoft.UI.Xaml`. Category discovery (`-1`) skips a test case without a `Category` trait; upstream's `Traits["Category"]`
  threw `KeyNotFoundException` there and dropped every category found after it.
- **Windows run log**: `HeadlessRunner/windows/TestRunLog.cs` (toolkit code) copies the console output of the headless runner
  (XHarness logs to `Console.Out` when it writes XML results) into a file next to the results file, written through at once, with
  XHarness's `[STRT]` line for each test that starts, every unhandled exception (`[UNHANDLED]`) and every exception as it is thrown
  (`[THROWN]`). Both Windows runners start it.
  `eng/devices/windows.ps1` reads it when an app crashes or hangs; a packaged app has no other log.
- **Code coverage**: `CoverageRecorder.cs` (toolkit code) writes the hits that coverlet-instrumented toolkit assemblies recorded, for
  the coverage runs of `eng/devices` (`-Coverage`, see `eng/devices/README.md`). coverlet writes them from a process-exit handler, which
  never runs in these apps, so the Android instrumentation (before `Finish`) and the iOS and Mac Catalyst headless runner (when the
  tests have completed) call `CoverageRecorder.Flush` with the folder named by `CTM_COVERAGE_DIRECTORY`. Without it they do nothing.
- **Project settings**: `ImplicitUsings` and `Nullable` are disabled as upstream, and the `.editorconfig` marks the vendored files as
  generated code so that the repository's style analyzers leave them as they are upstream; the toolkit's own files
  (`TestRunLog.cs`, `TestApplicationDispatcher.cs`, `CoverageRecorder.cs`) are analyzed like the rest of the repository. `CS0618` (`Frame` in the visual
  runner XAML) and `CsWinRT1028` are not reported, and `AllowUnsafeBlocks` is set for CsWinRT.
- **MSBuild files**: `CommunityToolkit.Maui.DeviceTests.Runners.targets` drops upstream's Arcade-only property and an
  asset-less `xunit.runner.visualstudio` reference. `CommunityToolkit.Maui.DeviceTests.Runners.props` names the unpackaged
  Windows switch `_CtmDeviceTestUnpackaged` (dotnet/maui: `_MauiDeviceTestUnpackaged`) and also sets the CsWinRT settings for
  the apps.

Keep `Microsoft.DotNet.XHarness.TestRunners.Xunit` (in the `.csproj`) on the same version as the `xharness` tool in
`.config/dotnet-tools.json`. Update the two together, along with the dotnet/maui commit above.
