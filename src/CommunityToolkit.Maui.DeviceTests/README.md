# CommunityToolkit.Maui device tests

Device tests run inside a real app on Android, iOS, Mac Catalyst and Windows. They cover the toolkit code that the
`net10.0` unit tests cannot reach: platform-specific source (`*.android.cs`, `*.ios.cs`, `*.macos.cs`, `*.macios.cs`,
`*.windows.cs`), handlers and their platform views, behaviors attached to native views, and platform services. The
architecture is dotnet/maui's (`src/Controls/tests/DeviceTests`): xUnit v2 tests, a headless runner driven by XHarness on CI,
and a visual runner for running tests by hand.

If a test would also pass in `CommunityToolkit.Maui.UnitTests` (converters, defaults, property setters, math), it belongs there,
not here.

The house rules are in [`.github/instructions/device-tests.instructions.md`](../../.github/instructions/device-tests.instructions.md).
How to run the tests from the command line and on CI is in [`eng/devices/README.md`](../../eng/devices/README.md), which also says where the
code coverage of the device tests, merged with the unit tests', is published.

## Projects

| Project (`src/`) | What it is | ApplicationId |
|---|---|---|
| `CommunityToolkit.Maui.DeviceTests` | test app for `CommunityToolkit.Maui` + `CommunityToolkit.Maui.Core` (and `CommunityToolkit.Maui.Maps` on Windows) | `com.communitytoolkit.maui.devicetests` |
| `CommunityToolkit.Maui.MediaElement.DeviceTests` | test app for `CommunityToolkit.Maui.MediaElement` | `com.communitytoolkit.maui.mediaelement.devicetests` |
| `CommunityToolkit.Maui.Camera.DeviceTests` | test app for `CommunityToolkit.Maui.Camera` | `com.communitytoolkit.maui.camera.devicetests` |
| `CommunityToolkit.Maui.DeviceTests.Shared` | base classes and helpers (`dotnet-maui/` = vendored and ported dotnet/maui code) | |
| `CommunityToolkit.Maui.DeviceTests.Runners` (+ `.SourceGen`) | dotnet/maui's runner, vendored; see its [README](../CommunityToolkit.Maui.DeviceTests.Runners/README.md) | |

There is one app per shipped feature package, as in dotnet/maui (Core, Controls, Essentials): the main app covers
`CommunityToolkit.Maui`, `CommunityToolkit.Maui.Core` and `CommunityToolkit.Maui.Maps`, the other two apps cover MediaElement and
Camera. Each app references only its packages, plus the analyzers and source generators that the packages add to a consumer's app. Assembly names equal the project names. Every
app and `Shared` uses the root namespace `CommunityToolkit.Maui.DeviceTests`.

## Layout

```
CommunityToolkit.Maui.DeviceTests/
  Alerts/  Behaviors/  Essentials/  Extensions/  Maps/  PlatformConfiguration/  Services/  Views/   feature folders
  Infrastructure/                 smoke tests of the device-test infrastructure itself
  Resources/Images/**             MauiImage: test images, e.g. Resources/Images/red.png -> Source = "red.png"
  Resources/Raw/**                MauiAsset: test media and files, e.g. FileSystem.OpenAppPackageFileAsync("audio/a.mp3")
  Platforms/<Android|iOS|MacCatalyst|Windows>/   app heads (manifests, scene delegates, Windows App)
  CommunityToolkitHandlerTestBase.cs  base class of this app's tests
  TestCategory.cs                 category constants (partial class)
  MauiProgram.cs                  the runner's app configuration
CommunityToolkit.Maui.MediaElement.DeviceTests/   MediaElement/{MediaElementAndroid, MediaElementApple, MediaElementWindows}/, Infrastructure/, MediaElementHandlerTestBase.cs, ...
CommunityToolkit.Maui.Camera.DeviceTests/         Camera/{CameraAndroid, CameraApple, CameraWindows}/, Infrastructure/, CameraHandlerTestBase.cs, ...
```

- **Folders and files.** Put each feature in its folder, as `<Folder>/<Feature>/<Feature>Tests.cs` plus platform partials. A feature
  folder is named like the product folder of the feature (`Behaviors/IconTintColor` holds the `IconTintColorBehavior` tests). A base
  class that several features' tests share stays in the folder of the feature it was written for (`StatusBarTestBase` in
  `Behaviors/StatusBar`); helpers that tests of several folders use are at the app root (`VisualBehaviorHelpers.cs`).
  Files added under `Resources/Images` and `Resources/Raw` are packaged without editing the `.csproj`. Keep assets small and
  local: tests never use the network.
- **MediaElement and Camera apps.** Their implementations are almost entirely platform code (one `MediaManager` and one `CameraManager`
  per platform), so their tests are classes per platform and concern in one folder per platform, for example
  `MediaManagerPlaybackAndroidTests`, `MediaManagerPlaybackAppleTests` and `MediaManagerPlaybackWindowsTests`, rather than one feature
  class with platform partials. Each platform folder is named `<Area><Platform>` (`CameraAndroid`, `CameraApple`, `CameraWindows`), and
  each file after the class it declares. Never name a folder after a platform alone (`Android`, `iOS`, `MacCatalyst`, `Windows`, ...):
  `Directory.Build.targets` compiles the files of such a folder for that platform only (the match ignores case on macOS and Windows).
- **Platform files.** They use the repository's suffixes, which the repository-wide `Directory.Build.targets` filters:
  `.android.cs`, `.ios.cs` (iOS only), `.macos.cs` (Mac Catalyst only), `.macios.cs` (iOS + Mac Catalyst) and `.windows.cs`.
  Inside a `.macios.cs` file use `#if MACCATALYST` for Catalyst-only code. Do not add compile filters to the projects.
- **Namespace.** Every test file uses `namespace CommunityToolkit.Maui.DeviceTests;`, whatever its folder (dotnet/maui's tests
  all live in `Microsoft.Maui.DeviceTests`). Two reasons:
  - This assembly's `[Fact]`, `[Theory]`, `[InlineData]`, `[ClassData]`, `[MemberData]` and `[Category]` win over xUnit's
    (`global using Xunit;`) only from inside that namespace.
  - A namespace segment named after a toolkit type would hide the type. In `namespace CommunityToolkit.Maui.DeviceTests.MediaElement`,
    `new MediaElement()` does not compile.

## Writing a test

The shared file owns the class attributes, the base class and the cross-platform tests. Each platform partial owns the accessors
that read native state (and platform-only tests). An accessor the shared file calls must exist in every partial that compiles.

```csharp
// Behaviors/IconTintColor/IconTintColorBehaviorTests.cs
using CommunityToolkit.Maui.Behaviors;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.IconTintColorBehavior)]
[Collection(TestCollections.DisableParallelization)]
public partial class IconTintColorBehaviorTests : CommunityToolkitHandlerTestBase
{
	[Fact]
	public async Task TintColorAppliedWhenLoaded()
	{
		SetupBuilder();

		var image = new Image { Source = "red.png", WidthRequest = 40, HeightRequest = 40 };
		image.Behaviors.Add(new IconTintColorBehavior { TintColor = Colors.Blue });

		await AttachAndRun(image, async handler =>
		{
			await image.OnLoadedAsync();
			Assert.True(HasTint(handler));
		});
	}
}

// Behaviors/IconTintColor/IconTintColorBehaviorTests.android.cs
public partial class IconTintColorBehaviorTests
{
	static bool HasTint(IPlatformViewHandler handler) => ((Android.Widget.ImageView)handler.PlatformView!).ColorFilter is not null;
}

// Behaviors/TestCategory.Behaviors.cs
public static partial class TestCategory
{
	public const string IconTintColorBehavior = nameof(IconTintColorBehavior);
}
```

Tests are `public async Task <Subject><Condition><Outcome>()` (or `void`), PascalCase without underscores. Build every TFM
before pushing: a missing accessor in one partial breaks only that platform.

## Base classes

`CommunityToolkit.Maui.DeviceTests.Shared` holds the generic part, ported from dotnet/maui and using public MAUI API only:

```
TestBase                         InvokeOnMainThreadAsync (4 overloads), WaitForGC()
 └ HandlerTestBasement           the test's own MauiApp, MauiContext, CreateHandler, InitializeViewHandler, AttachAndRun,
    │                            SetValueAsync, ValidateHasColor, DisposeAsyncCore
    └ HandlerTestBase            adds the .NET MAUI Controls handlers; CreateHandler(Async), GetValueAsync, OnLoadedAsync,
       │                         OnUnloadedAsync
       ├ CommunityToolkitHandlerTestBase  (CommunityToolkit.Maui.DeviceTests)
       ├ MediaElementHandlerTestBase      (CommunityToolkit.Maui.MediaElement.DeviceTests)
       └ CameraHandlerTestBase            (CommunityToolkit.Maui.Camera.DeviceTests)
```

xUnit creates one instance of the class per test. Each test gets its own `MauiApp`, built by `MauiApp.CreateBuilder()` with the
test dispatcher, a `TestApplicationStub` as `IApplication`, all Controls handlers (`UseMauiApp<TestApplicationStub>()`) and the
app's package. When the test ends, xUnit calls `HandlerTestBasement`'s `IAsyncLifetime.DisposeAsync`: the `MauiApp` is disposed
on the UI thread and `DisposeAsyncCore` restores what the base class changed. For your own per-test cleanup, override
`DisposeAsyncCore` and call the base; do not implement `IDisposable` or `IAsyncLifetime` in a test class (xUnit v2 never calls
`IAsyncDisposable` on a test class).

Call `SetupBuilder(...)` first in every test that uses `MauiContext`, handlers or toolkit options (dotnet/maui's convention);
without it the test's `MauiApp` is built with the defaults on first use. It throws if the test already used `MauiContext` or
created a handler.

| App | `SetupBuilder` | Adds |
|---|---|---|
| main | `SetupBuilder(Action<MauiAppBuilder>? configureBuilder = null, Action<Options>? configureToolkitOptions = null)` | `UseMauiCommunityToolkit(configureToolkitOptions)` |
| MediaElement | `SetupBuilder(Action<MauiAppBuilder>? configureBuilder = null, bool isAndroidForegroundServiceEnabled = false, Action<MediaElementOptions>? configureMediaElementOptions = null)` | `UseMauiCommunityToolkitMediaElement(...)` |
| Camera | `SetupBuilder(Action<MauiAppBuilder>? configureBuilder = null)` | `UseMauiCommunityToolkitCamera()` |

```csharp
SetupBuilder(builder => builder.ConfigureMauiHandlers(h => h.AddHandler<MyTestView, MyTestViewHandler>()));
SetupBuilder(configureToolkitOptions: options => options.SetShouldSuppressExceptionsInConverters(true));
```

**Toolkit options are app-wide.** `Options` and `MediaElementOptions` keep their values in static properties. A test that
passes options must be in the `DisableParallelization` collection; otherwise `SetupBuilder` throws. The base class captures the
previous values with `StaticPropertySnapshot` and restores them when the test ends.

**Two MauiApps are involved.**
- **The test's MauiApp** (`MauiContext`, `ApplicationServices`). Handlers created in the test come from it.
- **The runner's MauiApp**, built by each app's `MauiProgram`. It provides `IPlatformApplication.Current.Services`,
  `Application.Current` (the visual runner's application) and the app lifecycle events. Some toolkit code resolves services
  from the runner's MauiApp, not from the handler's `MauiContext`:
  - `CameraView` and `CameraViewHandler` resolve `ICameraProvider` there.
  - `PopupService` uses it as well.
  - The Windows Snackbar registers its notification handler at launch: the main app's `MauiProgram` enables
    `SetShouldEnableSnackbarOnWindows(true)`.

  Register in `MauiProgram.ConfigureApp` anything such code needs.

## Hosting views

| Need | Use | In a window? |
|---|---|---|
| Property mapping, handler and platform-view state | `await CreateHandlerAsync<THandler>(view)`, `GetValueAsync(view, handler => ...)`, `SetValueAsync` | no (measured and arranged) |
| Loaded/Unloaded, `PlatformBehavior`s, focus, pixels, anything that needs a window or a parent | `await AttachAndRun(view, async handler => ...)` | yes, the runner's real window |

- **`AttachAndRun`** (dotnet/maui's `HandlerTestBasement.AttachAndRun`) creates the handler with the test's `MauiContext`,
  puts the platform view into the running app's window and runs the action on the UI thread. It removes the view afterwards.
  The handler stays connected.
  - Android: a `FrameLayout` under the runner activity's `android.R.id.content`.
  - iOS and Mac Catalyst: a wrapper view on the key window's root view controller. A view that a view controller owns, such as
    a `ContentPage`'s `PageViewController`, is added as a child view controller.
  - Windows: a new WinUI window per call.
- **Pages.** Pass a `ContentPage` to host one: `await AttachAndRun(new ContentPage { Content = layout }, ...)`.
- **Waiting.** Inside the action, wait with `await view.OnLoadedAsync()`. After it, wait with `await view.OnUnloadedAsync()`.
  Both default to a 2 s timeout, as in dotnet/maui.
- **Handlers.** Disconnect handlers that hold platform resources (players, camera sessions) at the end of the test:
  `await InvokeOnMainThreadAsync(() => view.Handler?.DisconnectHandler())`.
- **Limits of headless hosting on Android, iOS and Mac Catalyst.** The headless runner has no .NET MAUI `Window`:
  `Application.Current.Windows` is empty and a hosted page has no `Window`. So `Navigation.PushModalAsync`, popups and code
  that walks `Application.Current.Windows` cannot run there. On Windows the tests run inside the visual runner app, whose window
  shows its home page.
- **iOS and Mac Catalyst behavior observed by the smoke tests:**
  - `MauiMediaElement` needs a parent `Page`, or a window in `Application.Current.Windows`, when it is created. Host
    `MediaElement` in a layout inside a `ContentPage`.
  - For a platform view that is not a MAUI view, such as `MauiMediaElement`, MAUI raises `Unloaded` (and detaches
    `PlatformBehavior`s) when the handler disconnects, not when the view leaves the window.

## Categories, collections and filters

- **Categories.** Every test class has `[Category(TestCategory.X)]`. `X` is a `public const string` of the app's `TestCategory`,
  which is a `public static partial class`. Add a feature's constant to the `TestCategory.<Folder>.cs` partial of the top-level
  folder of its tests (`Behaviors/TestCategory.Behaviors.cs`; `MediaElement/TestCategory.MediaElement.cs` and
  `Camera/TestCategory.Camera.cs` in the other apps). Two partial files cannot declare the same constant, so name each one after its feature
  (`IconTintColorBehavior`), never after a kind of test (`Memory`, `Behaviors`).
  - `TestFilter=Category=X` runs only X by skipping every other constant. A test with no category, or with a string literal,
    therefore runs in every filtered run.
  - `SkipCategories=A,B` skips A and B.
  - Every test has exactly one feature category, on its class (or on the method). A test is skipped if any of its categories
    is skipped, so a test with two would be skipped by both `Category=` runs, and by every launch of a filtered Windows run
    (one launch per category). The only category that may be added is `RequiresUserInteraction`. Each app's
    `EveryTestHasOneFeatureCategory` infrastructure test fails on a test without a category, with two feature categories, or
    with a string that is not a `TestCategory` constant.
- **`TestCategory.RequiresUserInteraction`** marks tests that need a person (pickers, permission prompts, hardware). Keep the
  feature category on the class and add `[Category(TestCategory.RequiresUserInteraction)]` to the method. Headless runs never
  execute them: `MauiProgram` adds the category to every skip list, and the scripts skip it too. Start the app without
  arguments to run them in the visual runner.
- **`TestCategory.DeviceTestsInfrastructure`**: the smoke tests in `Infrastructure/`. Each run reports two Skipped tests from
  that class on purpose; they prove that run-time skips reach the results as Skipped.
- **`[Collection(TestCollections.DisableParallelization)]`** runs a class alone, after all other tests. Use it for classes that
  host views in the window or change app-wide state: toolkit options, the status bar, `Application.Current`, `Preferences`,
  permissions, files at fixed paths, or focus. XHarness already runs every test sequentially
  (`[collection-per-class, non-parallel]`); the collection protects the visual runner, which runs collections in parallel. xUnit
  v2 only discovers collection definitions in the test assembly, so every app declares this one in
  `DisableParallelizationCollection.cs`.

Run one category on each platform from the repository root (`-Project Maui|MediaElement|Camera|All`; see
[`eng/devices/README.md`](../../eng/devices/README.md)):

```bash
pwsh eng/devices/android.ps1  -Project Maui -Avd <your AVD> -TestFilter 'Category=IconTintColorBehavior'
pwsh eng/devices/ios.ps1      -Project Maui -TestFilter 'Category=IconTintColorBehavior'
pwsh eng/devices/catalyst.ps1 -Project Maui -TestFilter 'Category=IconTintColorBehavior'
powershell -ExecutionPolicy Bypass -File eng\devices\windows.ps1 -Packaging unpackaged -Project Maui -TestFilter 'Category=IconTintColorBehavior'
```

Under the scripts, the filter is passed the way dotnet/maui passes it:
- Android: `xharness android test ... --arg TestFilter=Category=X`.
- iOS and Mac Catalyst: `xharness apple test ... --set-env=TestFilter=Category=X`.
- Windows: `<App>.exe "<results.xml>" -1` lists the categories in `devicetestcategories.txt`, then
  `<App>.exe "<results.xml>" <index>` runs one of them.

Debug builds for Android need `-p:EmbedAssembliesIntoApk=true` when they are installed by XHarness.

## Helpers

All of these are in the `CommunityToolkit.Maui.DeviceTests` namespace.

- `AssertionExtensions`
  - Waiting: `OnLoadedAsync` / `OnUnloadedAsync` / `WaitForLoaded` (on a `VisualElement`), `IsLoadedOnPlatform()`, and
    `AssertEventually(Func<bool>, timeout, interval, message)` / `AssertEventuallyAsync`.
  - Leaks: `WaitForGC(params WeakReference[])`.
  - Focus: `FocusView`, `WaitForFocused` and `WaitForUnFocused`, both for a view (with the `MauiContext`) and for a platform view.
  - Pixels, on the platform view: `ToBitmap(mauiContext)`, `AssertContainsColor` / `AssertDoesNotContainColor`,
    `AssertColorAtPoint(Async)`, `AssertColorAtCenter(Async)`, `AssertColorsAtPointsAsync`, `AssertEqualAsync`,
    `ThrowScreenshot`. Failure messages embed the bitmap as base64.
  - Other: `CloseEnough`, `AssertHasFlag`.
  - Android only: `WaitForLayout`, `WaitForLayoutOrNonZeroSize`.
- `AssertHelpers.AssertEventually` and `Wait`: import with `using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;`.
- `CountingPlatformBehavior<TView>`: a probe that records `AttachedCount`, `DetachedCount`, `IsAttached` and
  `AttachedPlatformView`.
- `StaticPropertySnapshot`: `new StaticPropertySnapshot().Capture(typeof(X)).Restore()` for static option types.
- Main app only, `VisualBehaviorHelpers` (app root): `WaitForPixelsAsync`, the leak helper `WaitForGCAsync` and the other helpers of the
  visual behavior tests.
- `[Repeat(n)]` with a `[Theory]` and an `int` parameter repeats a test n times while you hunt flakiness. Do not commit it.
- `[SkipOnIOSVersion(major, minor, build, reason)]`: Skipped on iOS and Mac Catalyst from that version on.
- `Skip.If(condition, reason)`, `Skip.IfNot(condition, reason)` and `throw new SkipException(reason)`: run-time skips,
  reported as Skipped (see the patterns).
- Runner (`using CommunityToolkit.Maui.DeviceTests.Runners;`):
  - `TestServices.Services`: the runner's services.
  - `TestDispatcher.Current`: the UI-thread dispatcher.
  - `TestWindow.PlatformWindow`.
  - `MauiProgramDefaults.DefaultContext`: the Android activity.

## Patterns

- **Platform behaviors**, e.g. `IconTintColorBehavior`, `TouchBehavior`, `SelectAllTextBehavior`, `StatusBarBehavior`.
  - Follow dotnet/maui's `BasicLoadUnloadingWorks`: no platform effect before the view is loaded, attached once while it is in
    the window, detached after `OnUnloadedAsync`.
  - Assert the native state through the platform partial before attach, while attached, after a property change, and after
    detach (restored).
  - Also cover: the behavior added after load, the behavior removed while loaded, a property changed while detached (no-op),
    and a platform view of an unsupported type.
  - `Infrastructure/DeviceTestsInfrastructureTests.cs` has the minimal version.
- **Handlers and views.** Use `CreateHandlerAsync<THandler>(view)` and compare the virtual value with the platform value, read
  by an accessor in each platform partial. Use `AttachAndRun` only when the behavior needs a window.
- **Platform services** (FileSaver, FolderPicker, SpeechToText, Badge, Snackbar, Toast…): call them as an app would. Test every
  path that runs without a person:
  - argument validation and the exact exception types;
  - a token that is already cancelled, or one cancelled during the call;
  - permission denied or not declared;
  - a call made off the UI thread;
  - the documented result on an OS version without support;
  - state read back from the platform API.

  Picker UIs and microphone input go under `RequiresUserInteraction`.
- **Leak tests.**

  ```csharp
  WeakReference viewReference = null!, handlerReference = null!, platformViewReference = null!;
  await InvokeOnMainThreadAsync(() =>
  {
  	var view = new DrawingView();
  	var handler = CreateHandler<DrawingViewHandler>(view);
  	viewReference = new(view);
  	handlerReference = new(handler);
  	platformViewReference = new(handler.PlatformView);
  	((IElementHandler)handler).DisconnectHandler(); // the handler's own overload is protected
  	view.Handler = null;
  });
  await AssertionExtensions.WaitForGC(viewReference, handlerReference, platformViewReference);
  ```

  Create the objects inside the lambda so that no local keeps them alive. Keep the leak test in the feature's test class, under
  the feature's category, so that `Category=<Feature>` runs it.

  The Mono runtime of Android, iOS and Mac Catalyst scans thread stacks conservatively, so a stale stack slot of the thread that
  hosted the views can, rarely, keep one object alive through every collection that thread runs. The behavior leak tests collect
  from a fresh thread for that reason (`VisualBehaviorHelpers.WaitForGCAsync`); use it for a leak test that has to be deterministic there.
- **OS-version branches.** Platform code can branch on `OperatingSystem.IsAndroidVersionAtLeast(n)`, `IsIOSVersionAtLeast`,
  `IsMacCatalystVersionAtLeast` or `IsWindowsVersionAtLeast`. Test both sides by asserting what each branch does on the device
  that takes it, e.g. `if (OperatingSystem.IsAndroidVersionAtLeast(30)) { ...new behavior... } else { ...fallback... }`.
  - The apps' minimum versions are the library minimums (Android 21, 26 for MediaElement; iOS 15; Windows 10.0.17763.0), so
    older emulators can run the old branches. Mac Catalyst's minimum is 17.0, forced by the Xcode 27 SDK packs.
  - Never `return` early: a test that returns early is reported as Passed.
- **Run-time skips.** When a test cannot run on this device (no camera, an API that needs a newer OS than the test asserts),
  skip it visibly. A bare `return` is a silent pass and is not allowed.

  ```csharp
  Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(33), "Needs the Android 13 photo picker.");
  ```

  - Only this assembly's `[Fact]`/`[Theory]` turn `SkipException` into Skipped. The results then show `result="Skip"` with the
    reason, and the run is not failed.
  - For a skip known at compile time, use `[Fact(Skip = "reason, issue link")]` under `#if`.
  - When the missing thing is a machine capability that a CI runner may lack (a camera, a second camera, video rendering, media
    playback, an on-device speech service, the WebView2 Runtime), guard it with `DeviceTestCapabilities` (in
    `CommunityToolkit.Maui.DeviceTests.Shared`) instead, so that each CI job can declare it missing or required:

    ```csharp
    DeviceTestCapabilities.Require(DeviceTestCapabilities.Camera, cameras.Count > 0, "Needs a camera.");
    ```

    It skips like `Skip.IfNot`, skips without looking when the run declares the capability missing
    (`DEVICE_TEST_SKIP_CAPABILITIES`), and fails when the run requires it (`DEVICE_TEST_REQUIRE_CAPABILITIES`). Call
    `DeviceTestCapabilities.SkipIfDeclaredMissing(capability)` before a probe that is slow or may hang. Local runs and the visual
    runner declare nothing, so they run the test. A new kind of capability is a new constant there (see
    [`eng/devices/README.md`](../../eng/devices/README.md#capability-declarations)).
- **Human-only scenarios.** Put them under `[Category(TestCategory.RequiresUserInteraction)]` on the method, and still test their
  non-interactive paths.
  - Do not add a placeholder for what only a person or special hardware can do: a test whose body only skips, or is empty, can never
    fail. If no interactive test fits, check the scenario by hand in the Sample (`samples/CommunityToolkit.Maui.Sample`) and add a
    page or a control there when it has none.
