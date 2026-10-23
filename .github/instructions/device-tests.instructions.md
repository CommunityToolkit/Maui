---
description: 'Writing device tests for the .NET MAUI Community Toolkit: what belongs in a device test, file layout, base classes, categories, hosting views in a window, run-time skips and running a single category'
applyTo: 'src/CommunityToolkit.Maui.DeviceTests*/**,src/CommunityToolkit.Maui.MediaElement.DeviceTests/**,src/CommunityToolkit.Maui.Camera.DeviceTests/**'
---

# Device Tests

The full guide is `src/CommunityToolkit.Maui.DeviceTests/README.md`, and running the tests is described in `eng/devices/README.md`. The apps use dotnet/maui's device-test architecture: xUnit v2, a vendored runner, XHarness on CI. There is one app per shipped feature package: `CommunityToolkit.Maui.DeviceTests` (`CommunityToolkit.Maui`, `.Core` and `.Maps`), `CommunityToolkit.Maui.MediaElement.DeviceTests` and `CommunityToolkit.Maui.Camera.DeviceTests`.

## Rules

1. **Test only what needs a device.** A test must touch a handler, a platform view, a platform service, a window or the app lifecycle. Logic that passes in `CommunityToolkit.Maui.UnitTests` belongs there.
2. **Use the namespace `CommunityToolkit.Maui.DeviceTests`** in every file, whatever its folder. Only there do `[Fact]`, `[Theory]`, `[InlineData]` and `[Category]` bind to the device-test attributes. Folders are for organization only. Never create a namespace segment named after a toolkit type, such as `.MediaElement`.
3. **Structure each feature as one partial class:** `<Folder>/<Feature>/<Feature>Tests.cs`, plus platform partials with the repository suffixes `.android.cs`, `.ios.cs`, `.macos.cs`, `.macios.cs` and `.windows.cs`.
   - The shared file owns the attributes, the base class and the cross-platform tests.
   - The platform files own the accessors that read native state, and the platform-only tests.
   - The MediaElement and Camera apps, whose code is almost all platform code, use classes per platform and concern in one folder per platform instead (`MediaElement/MediaElementAndroid/MediaManagerPlaybackAndroidTests.android.cs`). Never name a folder after a platform alone (`Android`, `iOS`, `MacCatalyst`, `Windows`): `Directory.Build.targets` filters such folders by platform.
   - Every accessor the shared file calls must exist in each platform partial.
   - Do not add compile filters to the projects.
4. **Derive from the app's base class:** `CommunityToolkitHandlerTestBase`, `MediaElementHandlerTestBase` or `CameraHandlerTestBase`.
   - Call `SetupBuilder(...)` first in every test that uses `MauiContext`, handlers or toolkit options.
   - Register extra handlers through its `configureBuilder` argument.
   - Toolkit options passed to `SetupBuilder` are app-wide statics. The class must be in the `DisableParallelization` collection, and the values are restored when the test ends.
   - Each test's `MauiApp` is disposed when it ends (the base class implements xUnit's `IAsyncLifetime`). For extra per-test cleanup, override `DisposeAsyncCore` and call the base; never implement `IDisposable` or `IAsyncLifetime` in a test class.
5. **Put `[Category(TestCategory.X)]` on every test class.** `X` is a `public const string` in the `TestCategory.<Folder>.cs` partial of the top-level folder of the tests (for example `Behaviors/TestCategory.Behaviors.cs`). Never use a string literal. Every category must be runnable alone with `TestFilter=Category=X`. A test has exactly one feature category; only `RequiresUserInteraction` may be added. The `EveryTestHasOneFeatureCategory` infrastructure test enforces this. Name each constant after its feature (`IconTintColorBehavior`), never after a kind of test such as `Memory`: two partial files cannot declare the same constant.
6. **Add `[Collection(TestCollections.DisableParallelization)]`** to classes that host views in the window or change app-wide state: options, status bar, `Application.Current`, preferences, permissions, files, focus.
7. **Choose the lightest host.**
   - Property mapping: `CreateHandlerAsync<THandler>(view)`, `GetValueAsync` or `SetValueAsync`.
   - Loaded/Unloaded, `PlatformBehavior`s, focus and pixels: `AttachAndRun(view, async handler => ...)`.
   - Wait with `await view.OnLoadedAsync()` and `OnUnloadedAsync()`, an event with a `TaskCompletionSource` and `WaitAsync`, or `AssertEventually`. Never synchronize with `Task.Delay`, and never use `[Fact(Timeout = ...)]`.
8. **Assert virtual and platform state.** Read the native property, such as `UIImageView.TintColor` or `ImageView.ColorFilter`, never a field the toolkit caches.
   - Behaviors: assert before attach, while attached, after a property change and after detach, which must restore the state.
   - Disconnect the handlers of views that hold platform resources.
9. **Cover every branch.** That means the happy path, each `#if` and OS-version branch, edge values, the exact exception types, cancellation, calls off the UI thread, detach/re-attach/dispose, and a leak test (`AssertionExtensions.WaitForGC`) for views and behaviors with platform code.
10. **No silent passes.** Never write a bare `return` for "not supported here".
    - Assert what the other branch is specified to do.
    - Or skip visibly, which is reported as Skipped:
      - `Skip.If(condition, reason)` or `Skip.IfNot(condition, reason)` at run time;
      - `DeviceTestCapabilities.Require(capability, isAvailable, reason)` for a machine capability that a CI runner may lack (camera, second camera, video rendering, media playback, on-device speech, WebView2), so the CI job can declare it missing or required;
      - `[SkipOnIOSVersion(...)]`;
      - `[Fact(Skip = "reason, issue link")]` under `#if`.
    - Never add a placeholder, a test whose body only skips or is empty, for a branch that only a person or special hardware reaches. Put the interactive version under `RequiresUserInteraction` (rule 11), or leave the branch to a reproduction in the Sample (`samples/CommunityToolkit.Maui.Sample`).
11. **Human-only tests** (pickers, permission prompts, microphone or camera input) keep their feature category and add `[Category(TestCategory.RequiresUserInteraction)]` to the method. Headless runs never execute them. Their non-interactive paths are still tested.
12. **Use no network and no large media.** Add small assets under `Resources/Images` (MauiImage) or `Resources/Raw` (MauiAsset).
13. **Use only public MAUI API.** The device-test assembly names have no `InternalsVisibleTo` from MAUI. They do have it from the toolkit, through `Directory.Build.props`.
14. **Build every target framework before pushing:** android, ios and maccatalyst on macOS, windows on Windows. Then run the category on each platform:
    - `pwsh eng/devices/run.ps1 -TestFilter 'Category=X'` runs it in the app that declares it, on every platform of the machine that builds a test of it
    - or one platform at a time: `pwsh eng/devices/{android,ios,catalyst}.ps1 -Project Maui -TestFilter 'Category=X'` and `eng\devices\windows.ps1 -Packaging unpackaged -Project Maui -TestFilter 'Category=X'`
