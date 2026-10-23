# Running the device tests from the command line

These scripts build the device test apps and run them headless on Android, iOS, Mac Catalyst and Windows. They follow dotnet/maui's `eng/devices/*.cake` scripts (same targets, same `TestFilter` syntax, same XHarness arguments, same Windows app contract) and are what the device test jobs (`.github/workflows/device-tests.yml`, part of the `Build` workflow) run on every pull request. To run the tests on your own machine, start with [`run.ps1`](#one-command-runps1) and the guide for contributors, [LOCAL.md](LOCAL.md): one command for every platform your machine can run, running an app from Visual Studio, Rider or Visual Studio Code, the tests that need a person, and what a local run does differently from CI.

| Script | Platform | Runner |
| --- | --- | --- |
| `run.ps1` | every platform this machine can run | the scripts below, one after the other |
| `android.ps1` | Android emulator or device | `xharness android test` |
| `ios.ps1` | iOS simulator | `xharness apple test --target ios-simulator-64` |
| `catalyst.ps1` | Mac Catalyst on this Mac | `xharness apple test --target maccatalyst` |
| `windows.ps1` | Windows, packaged (MSIX) or unpackaged | the app itself (no XHarness on Windows) |
| `devices-shared.ps1` | shared helpers, dot-sourced by the scripts above | |
| `devices-coverage.ps1` | code coverage (`-Coverage`), dot-sourced by `android.ps1`, `ios.ps1` and `catalyst.ps1` | coverlet |
| `Run-PackagedAppAndWait.ps1` | starts a packaged Windows app and waits for its process | vendored from dotnet/maui (toolkit changes listed in its header) |

There is one test app per shipped feature package: `src/CommunityToolkit.Maui.DeviceTests` (`CommunityToolkit.Maui`, `.Core` and `.Maps`), `src/CommunityToolkit.Maui.MediaElement.DeviceTests` and `src/CommunityToolkit.Maui.Camera.DeviceTests`. `dotnet test` does not run them.

## Prerequisites

* The .NET SDK from `global.json` and the MAUI workload (`dotnet workload install maui`; `maui-android` is enough on Linux).
* PowerShell 7 (`pwsh`) on macOS and Linux. On Windows use Windows PowerShell 5.1 (`powershell`) or PowerShell 7.
* XHarness is a local tool pinned in `.config/dotnet-tools.json`; the scripts run `dotnet tool restore` themselves. It comes from the public `dotnet-eng` feed, which `NuGet.config` maps to `Microsoft.DotNet.XHarness.*` only. coverlet and ReportGenerator, for [code coverage](#code-coverage), are pinned there too.
* Android: the Android SDK (`ANDROID_HOME`, or the default SDK location) with platform-tools and the emulator, and JDK 17 or later (`JAVA_HOME`).
* iOS and Mac Catalyst: macOS with the Xcode version that the installed iOS workload requires.
* Windows: Windows 10 1809 or later. The first packaged run needs an elevated shell to trust its signing certificate.

## Quick start

Run these from the repository root. Each command builds the app and runs its tests.

```bash
# Everything this machine can run: on a Mac the iOS simulator and Mac Catalyst, on Windows the packaged and the unpackaged app, and Android
# when an emulator is running or an AVD exists; all three apps, then one summary (see run.ps1 below)
pwsh eng/devices/run.ps1

# Android: boot an existing AVD on port 5560, run the tests, shut the emulator down afterwards
pwsh eng/devices/android.ps1 -Avd CTM_DeviceTests -EmulatorPort 5560

# iOS: creates a dedicated simulator (newest iOS runtime, newest iPhone with one built-in screen), grants the test apps the
# camera, the microphone and speech recognition on it, and deletes it afterwards
pwsh eng/devices/ios.ps1

# Mac Catalyst: the app opens a window on this Mac while it runs
pwsh eng/devices/catalyst.ps1
```

```powershell
# Windows: everything this machine can run
powershell -ExecutionPolicy Bypass -File eng\devices\run.ps1

# Windows, one platform
powershell -ExecutionPolicy Bypass -File eng\devices\windows.ps1 -Packaging unpackaged
powershell -ExecutionPolicy Bypass -File eng\devices\windows.ps1 -Packaging packaged
```

Run all three apps with `-Project All`, or one with `-Project MediaElement` (`Maui`, `MediaElement`, `Camera`, a project name or a `.csproj` path; comma-separated values are fine). Build once and test many times with `-Target buildOnly` and `-Target testOnly`.

Create an AVD for Android once, for example:

```bash
"$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager" "system-images;android-36;google_apis;arm64-v8a"   # x86_64 on Intel hosts
echo no | "$ANDROID_HOME/cmdline-tools/latest/bin/avdmanager" create avd -n CTM_DeviceTests -k "system-images;android-36;google_apis;arm64-v8a" -d pixel_7
```

## One command: run.ps1

`run.ps1` detects the operating system and the platforms it can run, runs the platform scripts on each with `-Target buildAndTest` (all three apps, `Debug`), and ends with one summary. It needs no setup beyond the [prerequisites](#prerequisites). [LOCAL.md](LOCAL.md) is the guide for using it.

```bash
pwsh eng/devices/run.ps1                                                   # everything this machine can run
pwsh eng/devices/run.ps1 -DryRun                                           # print the plan and run nothing (also -WhatIf)
pwsh eng/devices/run.ps1 -Platform ios,android -Project Maui -TestFilter 'Category=IconTintColorBehavior'
```

| Machine | Runs without `-Platform` |
| --- | --- |
| macOS | `ios` (needs Xcode and an iOS simulator runtime), `maccatalyst` (needs Xcode) and `android` |
| Windows | `windows-packaged`, `windows-unpackaged` and `android` |
| Linux | `android` |

A platform that the machine cannot run is skipped, with the reason in the plan and in the summary. A platform that `-Platform` names and that cannot run is an error: exit code 3 on the wrong operating system, 81 for Android without a device, 71 otherwise. When nothing can run the exit code is 71.

Android runs on, in this order: the device named by `-DeviceId` (without `-Avd`); the AVD named by `-Avd`, which the script boots and stops; an emulator that is running already (the first `emulator-*` in state `device`); an AVD that the script picks and boots. Of the AVDs (the `*.ini` files in `$ANDROID_AVD_HOME`, `$ANDROID_USER_HOME/avd` or `~/.android/avd`), it picks one with a system image for this machine's CPU that is not for Wear OS, TV, Automotive, Desktop or XR: one named `CTM_DeviceTests*` first, then the newest API level. A physical device is never used unless `-DeviceId` names it, and the emulator gets a free port from 5554 up unless `-EmulatorPort` says which.

| Parameter | Default | Meaning |
| --- | --- | --- |
| `-Platform` | every platform this machine can run | `ios`, `maccatalyst` (`catalyst`), `android`, `windows-packaged` (`packaged`), `windows-unpackaged` (`unpackaged`) and `windows` for both; comma-separated values are fine |
| `-Project` | `All` | `Maui`, `MediaElement`, `Camera`, `All`, a project name or a `.csproj` path. With `-TestFilter 'Category=<name>'` and no `-Project`, only the apps that declare the category run, because an app that runs 0 tests fails with 94 |
| `-TestFilter` | `$env:TEST_FILTER` | as in the other scripts |
| `-Configuration` | `Debug` | CI builds `Release` |
| `-Target` | `buildAndTest` | `buildOnly`, `testOnly` or `buildAndTest` |
| `-DryRun` (`-WhatIf`) | | print the plan and stop |
| `-Results` | `artifacts/device-tests` | the folder that holds `<platform>/<app>/` |
| `-BinlogDir`, `-SkipCapabilities`, `-RequireCapabilities`, `-Timeout`, `-InfraRetries`, `-BuildArgs` | those of the platform scripts | passed to every platform script when given |
| `-Avd`, `-EmulatorPort`, `-DeviceId` | | Android: boot this AVD, on this port; use this device |
| `-Udid`, `-Xcode` | | iOS: use this simulator (nothing is granted on it); iOS and Mac Catalyst: use this Xcode.app |
| `-KeepDevice` | | keep the simulator or the AVD that a script started, and the installed packaged Windows app |

The platform scripts run in the process of `run.ps1`, one after the other, and print as they always do; the summary at the end has one row per platform and app with the tests that passed, failed and were skipped, the time of the platform and the results folder, then the reasons of every failure and the platforms that were skipped. The exit code is the first non-zero exit code of the scripts, or 0. Before `run.ps1` runs the tests of a platform it deletes the `gate.json` and `summary.md` of an earlier run, so a script that ends before it writes its verdicts shows no old numbers. Packaged Windows runs first of the two Windows platforms, so that a missing elevation shows within seconds. `-Coverage` and `-XHarnessArgs` are not passed on: use the platform scripts for them.

## Parameters

Common to every script:

| Parameter | Default | Meaning |
| --- | --- | --- |
| `-Target` | `buildAndTest` | `buildOnly`, `testOnly` or `buildAndTest` |
| `-Project` | `CommunityToolkit.Maui.DeviceTests` | app(s) to build and run |
| `-Configuration` | `Debug` | build configuration; CI builds and runs `Release` (see [CI](#ci)) |
| `-Results` | `artifacts/device-tests/<platform>` | results root; every app writes to `<Results>/<project name>/` |
| `-BinlogDir` | `artifacts/logs` | one binlog per build |
| `-TestFilter` | `$env:TEST_FILTER` | `Category=<name>` or `SkipCategories=<name>,<name>` (see below) |
| `-SkipCapabilities` | `$env:DEVICE_TEST_SKIP_CAPABILITIES` | capabilities this machine lacks; their tests are skipped (see [Capability declarations](#capability-declarations)) |
| `-RequireCapabilities` | `$env:DEVICE_TEST_REQUIRE_CAPABILITIES` | capabilities this machine must offer; their tests fail instead of skipping without them |
| `-Timeout` | `00:30:00` | limit for one app's test run |
| `-LaunchTimeout` | `00:10:00` | XHarness launch timeout (Android, iOS, Mac Catalyst) |
| `-InfraRetries` | 1 on CI, 0 locally | retries for infrastructure failures only (see below) |
| `-BuildArgs` | none | extra `dotnet build`/`publish` arguments; write properties as `/p:Name=Value`, because PowerShell reads `-p:...` as a parameter name |

Platform parameters:

| Script | Parameter | Meaning |
| --- | --- | --- |
| `android.ps1` | `-Avd <name>`, `-EmulatorPort` (5554) | boot this AVD for the run, with both cameras emulated as on CI, and stop it afterwards (`-KeepDevice` keeps it running) |
| `android.ps1` | `-DeviceId <serial>` | use this attached device; without `-Avd`/`-DeviceId` the only attached device is used |
| `android.ps1` | `-DeviceArch` | XHarness `--device-arch`; defaults to the device's ABI |
| `android.ps1`, `ios.ps1`, `catalyst.ps1` | `-XHarnessArgs` | extra XHarness arguments, for example `--set-env=NAME=value` |
| `android.ps1`, `ios.ps1`, `catalyst.ps1` | `-Coverage` | collect [code coverage](#code-coverage) of the toolkit assemblies; defaults to `$env:DEVICE_TEST_COVERAGE` (`true`), which only the CI jobs set |
| `ios.ps1` | `-Udid` | use this simulator instead of creating one |
| `ios.ps1` | `-DeviceType`, `-Runtime`, `-SimulatorName` | for the created simulator, for example `-DeviceType 'iPhone 17' -Runtime 27.0`. Without `-DeviceType` the simulator is the runtime's first iPhone with one built-in screen: the status bar tests read the status bar from the top of the screen, and iPhone Duo (two screens) shows it on their right edge |
| `ios.ps1` | `-KeepDevice` | keep the created simulator |
| `ios.ps1`, `catalyst.ps1` | `-Xcode` | Xcode.app to use instead of `xcode-select -p` |
| `windows.ps1` | `-Packaging packaged\|unpackaged` | required |
| `windows.ps1` | `-CategoryTimeout` (`00:08:00`) | limit for one category launch |
| `windows.ps1` | `-CertificateSubject` | MSIX signing certificate subject; defaults to the manifest Publisher |
| `windows.ps1` | `-KeepDevice` | leave the packaged app installed |

## Choosing tests: TestFilter and categories

Every test class has `[Category(TestCategory.X)]`, and `TestFilter` uses dotnet/maui's syntax:

* `Category=X` runs only category X. The apps skip every other `TestCategory` constant, so a test without a category would run too.
* `SkipCategories=A,B` runs everything except A and B.

Each app has its own `TestCategory` class, and an app that runs 0 tests fails with 94. So pass a `Category=` filter together with the app that defines the category, for example `-Project MediaElement`; with `-Project All` the other apps fail.

The scripts pass it the way dotnet/maui does: `--arg TestFilter=...` on Android, `--set-env TestFilter=...` on iOS and Mac Catalyst. On Windows a packaged app started through package activation does not inherit the caller's environment, so a filter uses dotnet/maui's category protocol instead: the app is launched once with `-1` to list its categories and then once per selected category, skipping every other category. Without a filter one launch runs every test.

So every test has exactly one feature category (plus, optionally, `RequiresUserInteraction`): a test with two would be skipped by both `Category=` runs and by every Windows category launch. Each app's `EveryTestHasOneFeatureCategory` infrastructure test enforces it.

`RequiresUserInteraction` marks tests that need a person. Headless runs never execute them (the apps skip that category, and the scripts always add it to `SkipCategories`); run them by hand from the visual runner by starting the app without arguments. Without `-TestFilter` the filter is `SkipCategories=RequiresUserInteraction`.

```bash
pwsh eng/devices/ios.ps1 -Target testOnly -Project Maui -TestFilter 'Category=IconTintColorBehavior'
```

## Capability declarations

Some tests need something that a machine may lack: a camera, a second camera, a MediaPlayer that renders video, an on-device speech service. Such a test probes for it and skips visibly without it. A CI job knows more than the probe: what its runner can never do, and what it installed and must therefore work. It declares both as capabilities, which the tests read through `DeviceTestCapabilities` (`src/CommunityToolkit.Maui.DeviceTests.Shared/DeviceTestCapabilities.cs`, which lists them):

* `-SkipCapabilities A,B` (`DEVICE_TEST_SKIP_CAPABILITIES`): the tests that need A or B are skipped without probing, with a reason that names the declaration.
* `-RequireCapabilities C` (`DEVICE_TEST_REQUIRE_CAPABILITIES`): the tests that need C fail, instead of skipping, when the probe finds it missing.

Only the CI jobs set the variables, so local script runs and the visual runner declare nothing and run everything. The scripts pass the lists the way they pass `TestFilter`: `--arg SkipCapabilities=...` on Android, `--set-env SkipCapabilities=...` on iOS and Mac Catalyst, and on Windows a `devicetestcapabilities.txt` file next to the results file, because a packaged app does not inherit the environment. A name in both lists is an invalid argument (3), and each app's `DeclaredCapabilitiesAreValid` infrastructure test fails on a name that is not a capability. The run header and `summary.md` show the declarations. To see what a CI job skips, replay it locally with its lists:

```bash
pwsh eng/devices/catalyst.ps1 -Project All -SkipCapabilities Camera
```

## Results and exit codes

`<Results>/<project>/` contains everything the run produced: the xUnit v2 results (`testResults.xml` on Android, `xunit-test-*.xml` on iOS and Mac Catalyst, `TestResults-*.xml` on Windows), `xharness-result.json`, `xharness-console.log`, logcat or the app log, crash reports, a screenshot after a failure, and `gate.json` with the verdict. `<Results>/summary.md` has a table per app and the first 50 failures; in GitHub Actions it is also added to the job summary.

The scripts exit with XHarness's exit codes, on every platform:

| Code | Name | Meaning |
| --- | --- | --- |
| 0 | SUCCESS | every app ran at least one test and none failed |
| 1 | TESTS_FAILED | a test failed, or the results contain errors |
| 3 | INVALID_ARGUMENTS | bad parameter, for example an unknown `TestFilter` form |
| 4 | PACKAGE_NOT_FOUND | the app was not built (run `-Target buildOnly` first) |
| 70 | TIMED_OUT | the run did not finish within `-Timeout` |
| 71 | GENERAL_FAILURE | build failure or script error |
| 78 | PACKAGE_INSTALLATION_FAILURE | the app could not be installed |
| 80 | APP_CRASH | the app crashed |
| 81 | DEVICE_NOT_FOUND | no usable Android device or emulator |
| 83 | APP_LAUNCH_FAILURE | the app could not be started |
| 88 | SIMULATOR_FAILURE | the iOS simulator could not be created or booted, or simctl did not answer in time (every simctl call has a timeout; a created simulator that does not boot is replaced once on CI) |
| 94 | TEST_RESULTS_MISSING | no results file, an unreadable one, 0 tests, or every test was skipped |

With several apps the script returns the first non-zero code in project order.

The XHarness exit code is never trusted on its own, because XHarness reports SUCCESS when an iOS app crashes before its test runner starts and for every Mac Catalyst crash: it treats the missing results file as a device communication problem. After every run `devices-shared.ps1` fails it when the results file is missing or unreadable, when it reports 0 tests or only skipped tests, when a test failed or the results contain errors, when XHarness matched a crash report to the app (`appleLaunch.crashReport.matchedReport`), or when XHarness says the results file does not exist (`appleLaunch.testResultFile.exists`). A failed test is reported as 1 even when XHarness also returned an infrastructure code (for example 88 because the logcat dump failed after the results were pulled). On Windows a crash is recognized from the app's exit code, or from the app being gone right after activation, when it wrote no results.

Only infrastructure failures (78, 81, 83, 84, 86, 88, 89, 90, 91, 92) are retried, once on CI, and never when the results contain a failed test. Test failures, recognized crashes, hangs and missing results are never retried. Some crashes cannot be told apart from infrastructure failures and are retried once: an iOS app that dies before its runner connects without a matching crash report (92), and a packaged Windows app that dies before activation completes (83). The failed attempt's files stay in `attempt-1/` and the summary says `RETRIED`.

## Code coverage

`-Coverage` measures which lines of the toolkit assemblies (`CommunityToolkit.Maui`, `.Core`, `.MediaElement` and `.Camera`) the device tests run, with [coverlet](https://github.com/coverlet-coverage/coverlet) on Android, the iOS simulator and Mac Catalyst. In a test run (`testOnly` or `buildAndTest`) the script:

1. copies the toolkit assemblies that the apps reference, with everything the apps compile against, into a temporary folder, instruments them there with coverlet, puts the instrumented copies in place of the toolkit projects' build outputs, and packages the apps again with `/p:BuildProjectReferences=false`. In Debug that takes seconds per app on iOS and Mac Catalyst and one to two minutes per app on Android; a Release build compiles the apps ahead of time again, up to a minute per app on Mac Catalyst and two to three minutes per app on the iOS simulator;
2. runs the apps as usual, with `CTM_COVERAGE_DIRECTORY` (an XHarness `--set-env` on iOS and Mac Catalyst, an instrumentation `--arg` on Android). When the tests have run, the headless runner writes the hits of each instrumented assembly there (`CoverageRecorder` in `CommunityToolkit.Maui.DeviceTests.Runners`): on iOS and Mac Catalyst to `<Results>/<project>/coverage/`, on Android to the app's external files folder, which XHarness copies to the same place (`--device-out-folder`);
3. adds up the hits of all apps, lets coverlet write `<Results>/coverage/<name of the Results folder>.cobertura.xml` (`ios.cobertura.xml` by default; its log is `coverlet.log` there) and puts the original toolkit assemblies back, newer than the apps' instrumented copies (it also gives the toolkit projects' intermediate assemblies in `obj` the current time, because building a toolkit project copies them over its build output). The packaged apps keep the instrumented assemblies until the apps are built again; that build packages the original ones. A run that fails or is stopped (Ctrl+C) puts the original assemblies back as well.

Coverage never changes the exit code: when it fails, for example because an app crashed before it wrote its hits, the script warns and the report covers fewer apps or is missing. Every test of the three apps, the leak tests included, had the same outcome with and without coverage on all three platforms, and the runs took about as long.

The Windows device tests collect no coverage: `windows.ps1` has no `-Coverage`, because no way of collecting it there has been verified yet. In CI, the Android API 36, iOS simulator and Mac Catalyst jobs collect it, and the `Code Coverage` job of the `Build` workflow publishes one report that merges it with the unit tests' coverage: its job summary in the `Build` run and the `code-coverage-report` artifact (see [CI](#ci)).

`eng/coverage-report.ps1` merges Cobertura files of the unit tests and the device tests into one report (ReportGenerator: HTML, a Markdown summary and a text summary). The Markdown summary, also the `Code Coverage` job summary, starts with the line and branch coverage of all assemblies together, then one folded section per assembly in alphabetical order, labeled with its line and branch coverage. Each line and branch coverage (the totals, each assembly, and each class inside its assembly's section) gets a flag on the scale of [Google's code coverage guidelines](https://testing.googleblog.com/2020/08/code-coverage-best-practices.html): 🔴 below `-Acceptable` (60% by default: needs work now), 🟡 from `-Acceptable` (acceptable, could be better), 🟢 from `-Commendable` (75%: commendable), 🔵 from `-Exemplary` (90%: exemplary, more tests rarely pay off). Lines and branches are flagged separately. It first rewrites every file to repository-relative paths and one class per source file, because the tools and target frameworks name files and classes differently; a line then counts as covered when any test run covered it. The unit test coverage must come from the configuration of the device tests (`Release` in CI): a Debug build has more coverable lines (sequence points on braces), so the braces of code that only the Release tests run would count as uncovered. For example:

```bash
dotnet test --project src/CommunityToolkit.Maui.UnitTests -c Release --coverage --coverage-output-format cobertura --coverage-output "$PWD/artifacts/coverage/unit-tests.cobertura.xml"
pwsh eng/devices/ios.ps1 -Project All -Configuration Release -Coverage
pwsh eng/coverage-report.ps1 -Report artifacts/coverage,artifacts/device-tests/ios/coverage
# artifacts/coverage-report/summary.md and artifacts/coverage-report/report/index.html
```

## Windows details

* `unpackaged` publishes a self-contained app with `-p:WindowsPackageType=None -p:_CtmDeviceTestUnpackaged=true`; the app project bundles the Windows App SDK in that case (`WindowsAppSDKSelfContained` must not be passed on the command line, it would reach every library).
* `packaged` publishes a signed MSIX. The script reuses or creates a self-signed certificate whose subject is the manifest Publisher in `Cert:\CurrentUser\My` and trusts it in `Cert:\LocalMachine\TrustedPeople` (elevation needed once). To remove it (from an elevated shell, because TrustedPeople is machine-wide; the trusted copy has no friendly name, so match the subject): `Get-ChildItem Cert:\CurrentUser\My, Cert:\LocalMachine\TrustedPeople | Where-Object { $_.Subject -eq 'CN=CommunityToolkit.Maui.DeviceTests' -and $_.Issuer -eq $_.Subject } | Remove-Item`.
* The app is launched with `"<Results>\<project>\TestResults-<ApplicationId with dots replaced by underscores>.xml"`; a filtered run adds the category index (`-1` lists the categories in `devicetestcategories.txt`). Capability declarations go in `devicetestcapabilities.txt` in the same folder, one `SkipCapabilities=...` or `RequireCapabilities=...` line each. Packaged apps start through `Run-PackagedAppAndWait.ps1`, which waits for the real app process and reports its exit code.
* The app writes its log next to its results file (`TestResults-<...>.log`, one per category launch), written through at once, so it survives a crash and exists for packaged runs, whose standard output nobody reads. It names each test as it starts (`[STRT]`), every unhandled exception (`[UNHANDLED]`) and every exception as it is thrown (`[THROWN]`, with its callers): a failing WinRT callback, such as a DispatcherQueue handler, ends the app with a stowed exception (0xC000027B) that no unhandled-exception event reports. When a launch crashes or hangs, the verdict quotes the last test that started, the unhandled exceptions and the last exception thrown after that test started. An app that dies during the run leaves an empty results file, which counts as no results (a crash, 80, when its exit code says so).
* After a failure the run folder gets the Application event log (`application-events.txt`); crash dumps go to `dumps/` when the shell is elevated.
* A packaged app may use a camera or the microphone only once a person allowed it: the first time it tries, Windows shows a consent prompt, which nobody answers in a headless run. On a machine with a camera or a microphone, a packaged run therefore skips the tests that open them (the Camera app's camera tests, the offline speech recognition tests) until the app was allowed (Settings > Privacy & security > Camera or Microphone, or once from the visual runner). An unpackaged run follows the "Let desktop apps access your camera/microphone" settings instead.

## Using your own devices

The scripts only touch devices they create or that you name; `run.ps1` follows the same rule, and uses an emulator that is already running or boots an AVD, never an attached device that you did not name with `-DeviceId`. `ios.ps1` grants permissions on, erases or deletes only a simulator it created; a `-Udid` simulator that was shut down is booted for the run and shut down again. It never passes `--reset-simulator` to XHarness, which kills every simulator on the Mac. `android.ps1` stops only an emulator it started. Run one Mac Catalyst test app at a time.

## CI

The device test jobs, in `.github/workflows/device-tests.yml`, run every app on Android (API 36 and 28, x86_64 emulators on `ubuntu-24.04`), the iOS simulator and Mac Catalyst (`xcode-27`), and Windows packaged and unpackaged (`windows-2025`), and upload `artifacts/device-tests` and `artifacts/logs` from every job. The `Build` workflow (`dotnet-build.yml`) runs them as its `Device Tests` job (a reusable workflow) for pushes to `main` and pull requests, not for tags, and they load the .NET and Xcode versions from `.github/workflows/shared.env`, as the `Build` jobs do. The `Device Tests / Device Tests` job fails when any of the others fails; make it the required status check. To run a subset, start the `Device Tests` workflow from the Actions tab (`workflow_dispatch`) and choose the inputs `project` (`All`, `Maui`, `MediaElement` or `Camera`) and `test-filter`.

The jobs build and run the apps in `Release` (`DEVICE_TEST_CONFIGURATION`), the configuration that apps ship in; a manual run can choose `Debug` (the `configuration` input) for readable BCL exception messages and line numbers in stack traces. Debug runs every assembly in the Mono interpreter on Android, iOS and Mac Catalyst (MAUI sets `UseInterpreter` for Debug) and trims nothing, which no shipped app does, so it cannot show what only a Release build does:

* Android trims the toolkit and its bindings, and runs compiled code: the methods in the startup profiles of .NET for Android and MAUI ahead of time (profiled AOT), everything else with the JIT. A Java object whose class has a binding that nothing names arrives as a plain `Java.Lang.Object`, because the trimmer removed that binding: cast it to a binding interface with `JavaCast<T>()`, not with a C# cast.
* iOS and Mac Catalyst compile everything ahead of time, with no JIT and no interpreter. Native code can only call a static method marked `[MonoPInvokeCallback]`, and a delegate type that `Marshal.GetDelegateForFunctionPointer` creates needs `[UnmanagedFunctionPointer]`; anything else fails at run time with "Attempting to JIT compile method ... while running in aot-only mode".
* Windows compiles the apps ReadyToRun.
* Compiled code is faster than the interpreter, so a test that depends on how long something takes behaves differently, and Mono's conservative stack scanning finds stale references in other places (the leak checks therefore overwrite the main thread's unused stack before they collect).
* UIKit's thread checks are off; the Maui device test app turns them back on (`CheckForIllegalCrossThreadCalls` in its project), because six tests check that the toolkit lets `UIKitThreadAccessException` through.
* BCL exception messages are resource keys on Android, iOS and Mac Catalyst (`System.Resources.UseSystemResourceKeys`), so a test must not compare BCL exception text.

Besides the scripts, the jobs prepare their runners (on iOS, `ios.ps1` itself grants the camera, the microphone and speech recognition on the simulator it creates): the Android emulator gets a front camera (`-camera-front emulated`) and cold-boots without a snapshot (the action rewrites the AVD's configuration on every start, so a saved snapshot never matched), API 28 also with `-feature -GLDMA,-YUVCache,-YUV420888toNV21` (its x86_64 image turns these on, and the host's YUV converter then aborted the emulator when the camera streamed), and the job fails fast without KVM; when the emulator goes away during a run, `android.ps1` says so and does not hand the remaining apps to XHarness, which would wait for it and then try restarts that do not work on a hosted runner; on Mac Catalyst the job grants the camera, the microphone and speech recognition to the test apps in the runner's TCC database (CI only: it needs SIP off, and a grant it cannot write only warns) and installs BlackHole when the runner has no audio input; the Windows job installs Media Foundation first and sets the display to 1920x1080. Every job prints a short runner diagnostics step (CPUs, memory, cameras, audio devices and, on Windows, the speech recognizers and the Windows App Runtime). An app's run is limited to 30 minutes (`-Timeout`), 40 on the iOS simulator.

Each job declares its capabilities in its `env` (see [Capability declarations](#capability-declarations)):

| Job | `DEVICE_TEST_SKIP_CAPABILITIES` | `DEVICE_TEST_REQUIRE_CAPABILITIES` |
| --- | --- | --- |
| Android API 36 and 28 | `OnDeviceSpeech`: the image's on-device recognizer may never answer, and each question would wait 30 s | `SecondCamera`: the emulator has a back and a front camera, so the two-camera tests fail instead of skipping if it loses one |
| iOS simulator | | |
| Mac Catalyst | `Camera`: the runner has none | |
| Windows packaged and unpackaged | `VideoRendering`: the runner has no GPU, and whether it renders the H.264 test clip is unknown (48 MediaElement tests); `Camera` | `MediaPlayback`: the job installs Media Foundation (`Server-Media-Foundation`, which the Windows Server runner images do not include); `WebView2` |

A skipped capability is the CI's statement that its runner cannot run those tests; local runs execute them (a Windows 11 ARM64 VM in Parallels, for example, opens the clip but renders no video, so its probe skips the 48 tests visibly). To find out whether a runner can run them now, start the `Device Tests` workflow by hand with `run-everything` checked, which empties the skip lists; when the tests pass, delete the entry from the job.

The Android API 36, iOS simulator and Mac Catalyst jobs of a `Build` run also collect [code coverage](#code-coverage) (`DEVICE_TEST_COVERAGE` in their `env`) and upload it as `coverage-device-tests-<results folder>`. The `Code Coverage` job of the `Build` workflow (`dotnet-build.yml`) merges these with the unit tests' coverage (`coverage-unit-tests`, from the Windows library build, which runs the unit tests a second time, also in Release, to collect it; only the first run can fail the build) into one report with `eng/coverage-report.ps1`: its job summary in the `Build` run, and the HTML report in the `code-coverage-report` artifact. The device test jobs run in a called workflow, whose artifacts belong to the `Build` run, so the job waits for the whole `Device Tests` job (the Windows jobs too, which collect no coverage). It runs whatever the tests' results are; a tag build has no device tests and merges the unit tests alone. A manual run of the `Device Tests` workflow collects no coverage: nothing merges it there, a run of one app or with a filter covers only part of the tests, and the second packaging takes minutes.
