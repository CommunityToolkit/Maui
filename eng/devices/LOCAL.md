# Running the device tests on your machine

The device tests are three .NET MAUI apps, one per shipped feature package, that run the tests on a real platform: `src/CommunityToolkit.Maui.DeviceTests` (`CommunityToolkit.Maui`, `.Core` and `.Maps`), `src/CommunityToolkit.Maui.MediaElement.DeviceTests` and `src/CommunityToolkit.Maui.Camera.DeviceTests`. `dotnet test` does not run them. The `Device Tests` job of every pull request builds and runs them on Android, iOS, Mac Catalyst and Windows; this guide is for running them on your own machine.

* [Run everything with one command](#run-everything-with-one-command)
* [Run an app from your IDE](#run-an-app-from-your-ide)
* [Run the tests that need a person](#run-the-tests-that-need-a-person)
* [What your machine does differently from CI](#what-your-machine-does-differently-from-ci)

[README.md](README.md) in this folder is the reference for the scripts, their parameters, the exit codes, the capability declarations and the CI jobs. How to write a device test is in [src/CommunityToolkit.Maui.DeviceTests/README.md](../../src/CommunityToolkit.Maui.DeviceTests/README.md).

## Run everything with one command

Install the [prerequisites](README.md#prerequisites) once: the .NET SDK from `global.json` with the MAUI workload, and PowerShell 7 on a Mac. Then run this from the repository root:

```bash
pwsh eng/devices/run.ps1
```

```powershell
powershell -ExecutionPolicy Bypass -File eng\devices\run.ps1
```

`run.ps1` finds out what your machine can run, prints the plan, builds and runs the apps on each platform with the scripts of this folder, and ends with one summary: the passed, failed and skipped tests per platform and app, and the folder of their results.

| Your machine | What runs by default |
| --- | --- |
| Mac | the iOS simulator (needs Xcode and an iOS simulator runtime) and Mac Catalyst (needs Xcode) |
| Windows | the packaged (MSIX) app and the unpackaged app |
| Mac and Windows | Android, on the emulator that is already running if exactly one is, or else on an AVD that your CPU can run, which the script boots and stops again |

* A platform that your machine cannot run is skipped, and the plan and the summary say why: no Xcode, no iOS simulator runtime, no Android SDK, several emulators running (it does not guess; `-DeviceId` or `-Avd` chooses) or no emulator running and no AVD. The run fails when nothing can run (exit code 71) and when a platform that you name with `-Platform` cannot run.
* It never uses a physical Android device unless you name it with `-DeviceId`, and it never touches a simulator that it did not create unless you name it with `-Udid`.
* All three apps run (the platform scripts alone run the main app unless `-Project` says otherwise). The tests that need a person (`RequiresUserInteraction`) never run here; see [Run the tests that need a person](#run-the-tests-that-need-a-person).
* The platforms run one after the other, and each takes tens of minutes. A packaged Windows run needs an elevated PowerShell the first time, to trust its signing certificate.

Choose what runs:

```bash
pwsh eng/devices/run.ps1 -DryRun                                        # print the plan, run nothing (also -WhatIf)
pwsh eng/devices/run.ps1 -Platform ios,maccatalyst                      # only these platforms
pwsh eng/devices/run.ps1 -Project MediaElement                          # only this app (Maui, MediaElement, Camera or All)
pwsh eng/devices/run.ps1 -TestFilter 'Category=IconTintColorBehavior'   # one test category, where it has tests
pwsh eng/devices/run.ps1 -TestFilter 'SkipCategories=Snackbar,Badge'   # everything except these categories
pwsh eng/devices/run.ps1 -Configuration Release                         # what CI builds
pwsh eng/devices/run.ps1 -Target buildOnly                              # build now, then run with -Target testOnly as often as you like
```

The platform names are `ios`, `maccatalyst`, `android`, `windows-packaged` and `windows-unpackaged` (`catalyst`, `packaged`, `unpackaged` and `windows`, for both Windows platforms, are short forms). A category is a constant of `TestCategory` in `src/<app>/**/TestCategory*.cs`; every test class has exactly one. With a `Category=` filter, `run.ps1` leaves out the apps (unless you name them with `-Project`) and the platforms (unless you name them with `-Platform`) that have no test of the category, such as iOS for a category whose tests are all in `*.android.cs` files, because a run of 0 tests fails. [README.md](README.md#choosing-tests-testfilter-and-categories) explains the filters, and its [run.ps1](README.md#one-command-runps1) section lists every parameter, among them the ones that choose the Android device (`-Avd`, `-DeviceId`), the simulator (`-Udid`) and the Xcode (`-Xcode`).

The summary looks like this (the numbers are an example):

```text
Platform            App           Result   Total  Passed  Failed Skipped  Time     Results
ios                 Maui          PASS      6012    5990       0      22  9m 12s   artifacts/device-tests/ios/CommunityToolkit.Maui.DeviceTests
                    MediaElement  PASS       300     290       0      10           artifacts/device-tests/ios/CommunityToolkit.Maui.MediaElement.DeviceTests
                    Camera        FAIL       120     110       2       8           artifacts/device-tests/ios/CommunityToolkit.Maui.Camera.DeviceTests
maccatalyst         (all)         FAIL         -       -       -       -  0s       artifacts/device-tests/maccatalyst

2 of 4 passed in 9m 40s

Failed:
  ios / Camera: exit code 1 (TESTS_FAILED). ...
  maccatalyst: exit code 71 (GENERAL_FAILURE). ...
Exit code: 1 (TESTS_FAILED)
```

The exit code is the first non-zero exit code of the scripts (see [Results and exit codes](README.md#results-and-exit-codes)), or 0. Every app writes to `artifacts/device-tests/<platform>/<app>/`: the xUnit results, the logs, `gate.json` with its verdict and, on iOS, a screenshot after a failure. Next to the apps, `summary.md` lists up to 50 failed tests of each app. The build logs (`*.binlog`) are in `artifacts/logs/`.

## Run an app from your IDE

An IDE runs a device test app like any .NET MAUI app. Started without arguments, the app is the visual runner: it lists the test assemblies and runs the tests that you choose, with the debugger attached if you want. Use it to debug a test that the scripts reported (set a breakpoint in the test, start the app with the debugger and tap the test in the runner), to try a change on one platform, and to run the tests that need a person.

### Visual Studio (Windows)

Install the .NET Multi-platform App UI development workload.

1. Open `src/CommunityToolkit.Maui.slnx`. The apps are in the `Tests/Device Tests` folder.
1. Right-click `CommunityToolkit.Maui.DeviceTests` (or the MediaElement or Camera app) and choose Set as Startup Project.
1. In the debug target list of the toolbar choose the target framework (`Framework`) and the device:
    * Android emulator: `net10.0-android` and an emulator under `Android Emulators`. Create one in Tools > Android > Android Device Manager; Visual Studio starts it.
    * Windows, packaged: `net10.0-windows10.0.19041.0` and `Windows Machine`. Visual Studio registers the app from its build folder, which needs Developer Mode (Settings > System > For developers).
    * iOS simulator: `net10.0-ios` with a Mac paired through Pair to Mac. On the Mac itself, use Rider or Visual Studio Code.
    * Mac Catalyst runs on a Mac only, so use Rider or Visual Studio Code there.
1. Press F5 to debug, or Ctrl+F5 to run.

The launch profile of the apps is the packaged one. For the unpackaged app, build and start it from a terminal as described below.

### Rider (macOS and Windows)

1. Open `src/CommunityToolkit.Maui.slnx`.
1. Choose the app (`CommunityToolkit.Maui.DeviceTests`, `CommunityToolkit.Maui.MediaElement.DeviceTests` or `CommunityToolkit.Maui.Camera.DeviceTests`) in the run configurations.
1. In the toolbar, choose the target framework and the device next to it: an Android emulator (Device Manager creates one), an iOS simulator (macOS) or the Mac (`net10.0-maccatalyst`).
1. Run, or Debug.

For Windows, and for anything the toolbar does not offer, use a terminal command from below; the Rider terminal is a good place for it.

### Visual Studio Code (macOS and Windows)

1. Install the C# Dev Kit and the .NET MAUI extension, and open the repository folder.
1. Make one of the three apps the startup project.
1. Choose the target in the status bar: an Android emulator, an iOS simulator, Mac Catalyst, or Windows Machine.
1. Start debugging (F5), or run without debugging (Ctrl+F5).

### From a terminal

The IDEs run these builds. They work in every shell, and they are the quickest way to find out whether your setup is the problem. Replace `CommunityToolkit.Maui.DeviceTests` with the MediaElement or Camera app to run that one.

| Target | Command |
| --- | --- |
| Mac Catalyst | `dotnet build src/CommunityToolkit.Maui.DeviceTests -f net10.0-maccatalyst -t:Run` |
| iOS simulator | `dotnet build src/CommunityToolkit.Maui.DeviceTests -f net10.0-ios -t:Run -p:_DeviceName=:v2:udid=<simulator UDID>` |
| Android emulator | `dotnet build src/CommunityToolkit.Maui.DeviceTests -f net10.0-android -t:Run -p:AdbTarget="-s emulator-5554"` |

`net10.0` is the `NetVersion` of `Directory.Build.props`. `xcrun simctl list devices available` lists the simulators with their UDIDs. `adb devices` lists the serial of an emulator, and the Android emulator has to be running already (`$ANDROID_HOME/emulator/emulator -avd <name>`).

On Windows the scripts build the app, and you start it yourself. The first packaged build needs an elevated PowerShell to trust its signing certificate.

```powershell
# Unpackaged: publish a self-contained app, then start its exe without arguments
powershell -ExecutionPolicy Bypass -File eng\devices\windows.ps1 -Packaging unpackaged -Target buildOnly -Project Maui
$exe = Get-ChildItem src\CommunityToolkit.Maui.DeviceTests\bin -Recurse -Filter CommunityToolkit.Maui.DeviceTests.exe | Where-Object { $_.DirectoryName -like '*\publish' } | Sort-Object LastWriteTime -Descending | Select-Object -First 1
& $exe.FullName

# Packaged: build and install the signed MSIX with a one-category run that leaves it installed, then start CommunityToolkit.Maui.DeviceTests from the Start menu
powershell -ExecutionPolicy Bypass -File eng\devices\windows.ps1 -Packaging packaged -Project Maui -TestFilter 'Category=DeviceTestsInfrastructure' -KeepDevice
Get-AppxPackage -Name com.communitytoolkit.maui.devicetests | Remove-AppxPackage   # when you are done
```

### The visual runner

* The first page, Test Runner, lists the test assembly of the app with four counters: ✔ passed, ⛔ failed, ⚠ skipped and 🔷 not run. The Diagnostic Information at the bottom shows messages of the runner.
* Run All Tests runs every test of every assembly, and Run All every test of one assembly. The visual runner does not skip the `RequiresUserInteraction` tests, so their dialogs open one after the other. To avoid them, filter for the tests that you want and choose Run N Filtered.
* Tap the assembly to see its tests. The filter box keeps the tests whose name contains your text, without regard to case; the name is the namespace, the class and the method, so `IconTintColorBehaviorTests` or `ThePersonPicks` both work. The Filter by results list shows only the passed, failed, skipped or not run tests.
* Run N Filtered runs the tests that the list shows now, and Run All runs every test of the assembly. To run a test again, tap it, or filter by Failed and choose Run N Filtered.
* Tapping a test runs it and opens its result: the message, the output, the stack trace and buttons to copy them.
* The visual runner runs test classes in parallel, except the ones in the `DisableParallelization` collection (every class that hosts views or changes app-wide state, and every class with tests that need a person), which run alone after the others.

## Run the tests that need a person

The tests of the `RequiresUserInteraction` category need a person: they open a dialog, wait for a permission prompt, or wait for a key or a voice. Headless runs, the scripts included, never run them. To run them, start the app from your IDE (or from the terminal, see above) on the platform, open the assembly, type a filter from the tables below, and tap a test. It runs, you do what the table says, and the result page opens. A test that waits for you gives up after a while and fails; run it again. The classes that these tests belong to also have tests that run without you. The filters below match them as well, and they pass or skip on their own.

Some answers can only be given once. The system remembers a permission, so the test of the other answer skips until you reset it; the tables say how.

### iOS simulator and Mac Catalyst

| App | Filter | You |
| --- | --- | --- |
| Maui | `BadgeTests` (iOS only; on Mac Catalyst they run without you) | Answer the "Would Like to Send You Notifications" prompt that the first test shows, with Allow or Don't Allow. The tests that do not fit your answer skip. Uninstall the app from the simulator to be asked again. |
| Maui | `SpeechToTextTests` (also matches `OfflineSpeechToTextTests`) | `StartListenAsyncRecognizesWhatAPersonSays`: allow speech recognition and the microphone, say something in English, and then keep quiet for 3 seconds. `RequestPermissionsReturnsTheDecisionOfTheUser`: answer the speech recognition prompt. |
| Camera | `CameraAppleUserInteractionTests` | A Mac with a camera, so Mac Catalyst (the simulator has none, and the tests skip). `AllowingTheCameraPromptLetsTheCameraViewPreviewAndCaptureAPhoto`: tap Allow. `DenyingTheCameraPromptMakesStartVideoRecordingThrowCameraException`: tap Don't Allow. The system remembers the first answer, so run one test, then `tccutil reset Camera com.communitytoolkit.maui.camera.devicetests` and run the other. |

### Android emulator

| App | Filter | You |
| --- | --- | --- |
| Maui | `SpeechToTextTests` (also matches `OfflineSpeechToTextTests`) | `RequestPermissionsStopsWaitingWhenTheTokenIsCancelled`: leave the microphone permission dialog open for 3 seconds, then answer it. It skips once the microphone is allowed; `adb shell pm revoke com.communitytoolkit.maui.devicetests android.permission.RECORD_AUDIO` brings the dialog back. `RequestPermissionsReturnsTheDecisionOfTheUser`: answer the dialog. `StartListenAsyncRecognizesWhatAPersonSays`: allow the microphone and say something within 30 seconds; give the emulator your microphone first (Extended controls > Microphone > Virtual microphone uses host audio input). |
| Maui | `OfflineSpeechToTextTests` | `StartListenAsyncDownloadsALanguageThatIsSupportedButNotInstalled`: approve the download of the language pack that the system offers (the test waits up to 10 minutes). It skips below Android 14 and without an on-device recognizer. |

### Windows

The Maui app needs a person for these tests:

| Filter | You |
| --- | --- |
| `FileSaverTests` | A Save dialog opens. `SaveAsyncWritesTheStreamToTheFileThatThePersonPicksInTheSaveDialog`: save `files-test-saved.txt` in any folder. `SaveAsyncWithAnInitialPathWritesTheStreamToTheFileThatThePersonPicks` (two rows): save `files-test-initial-path.txt` in any folder. `SaveAsyncOffersOnlyAllFilesForAFileNameWithoutAnExtensionAndWritesTheStream`: check that "Save as type" has the single choice "All files", then save the file. `SaveAsyncReturnsACanceledResultWhenThePersonCancelsTheSaveDialog`: press Cancel. `SaveAsyncFailsWhenThePersonPicksALocationWithoutAFileSystemPath`: save into a location without a file system path, such as a library or a phone connected over MTP. `SaveAsyncClosesTheSaveDialogAndReturnsACanceledResultWhenTheTokenIsCanceled`: only watch, the test closes the dialog. |
| `FolderPickerTests` | A folder dialog opens. `PickAsyncReturnsThePathAndTheNameOfTheFolderThatThePersonPicks`: pick the folder `files-test-folder` that the test creates in the temporary folder (the dialog may open elsewhere). `PickAsyncReturnsACanceledResultWhenThePersonCancelsTheFolderDialog`: press Cancel. `PickAsyncFailsWhenThePersonPicksAFolderWithoutAFileSystemPath`: pick a folder without a file system path. `PickAsyncClosesTheFolderDialogAndReturnsACanceledResultWhenTheTokenIsCanceled`: only watch. |
| `SnackbarTests` | `PressingTheButtonOfTheNotificationRunsTheActionOnTheUiThreadAndDismissesTheSnackbar`: press OK on the Windows notification "Press OK on this notification" within a minute. Notifications must be on for the app; the test skips when Windows does not list the notification. |
| `SpeechToTextTests` (also matches `OfflineSpeechToTextTests`) | A machine with a microphone and an installed speech language. `StartListenAsyncStartsTheContinuousRecognitionSession` (two rows): allow the microphone when Windows asks; in the second row stay silent for 25 seconds after the start. `RequestPermissionsReturnsTheDecisionOfTheUser`: answer the microphone consent prompt. |
| `MapHandlerWindowsPageTests` | `ShowingTheUserAddsALocationPinWhenTheLocationIsAllowed`: allow the app to use your location when Windows asks. |

The MediaElement app has three tests (`MediaMetadataWindowsTests`) that wait for a key of the keyboard or for the system media overlay, within a minute. `PressingThePauseMediaKeyPausesTheMediaElement`: press Pause or Play/Pause. `PressingThePlayMediaKeyPlaysTheMediaElement`: press Play or Play/Pause. `PressingAnyOtherMediaKeyLeavesThePlaybackAlone`: press Next track or Stop, not Play or Pause.

## What your machine does differently from CI

The CI jobs prepare their runners, and a script that runs on your machine does not. These are the differences that decide whether a test runs or skips.

### Mac Catalyst asks for camera, microphone and speech recognition

macOS shows a privacy prompt the first time an app uses the camera, the microphone or speech recognition, and the answer belongs to that app: each of the three test apps asks for itself, because their bundle ids differ (`com.communitytoolkit.maui.devicetests`, `com.communitytoolkit.maui.camera.devicetests` and `com.communitytoolkit.maui.mediaelement.devicetests`). The CI job answers the prompts in advance by writing into the TCC database of its runner, which needs a virtual machine with System Integrity Protection off. Your Mac cannot do that, and a headless run cannot answer a prompt, so the tests that need one of the permissions are written to skip, with the reason, while the app has no answer yet. An unattended run on your Mac therefore passes with those tests skipped, and the summary counts them. Mac Catalyst may also ask whether the app may send notifications (the Badge tests); either answer is fine.

Answer a prompt when one shows. To run the permission-gated tests, give the apps their answers once:

1. Build the apps without running them: `pwsh eng/devices/catalyst.ps1 -Project All -Target buildOnly`.
1. Open the app that needs the permission from that build, without building it again, for example `open src/CommunityToolkit.Maui.DeviceTests/bin/Debug/net10.0-maccatalyst/maccatalyst-arm64/CommunityToolkit.Maui.DeviceTests.app` (`maccatalyst-x64` on an Intel Mac). Run the tests in [Run the tests that need a person](#run-the-tests-that-need-a-person) that ask for the permission, and click Allow. Once an app has asked, you can also switch it on under System Settings > Privacy & Security > Camera, Microphone or Speech Recognition.
1. Run the tests without building the apps again: `pwsh eng/devices/run.ps1 -Platform maccatalyst -Target testOnly`. The apps are signed ad hoc, so macOS remembers an answer for the app as it was built, and a build that changes the app brings the prompts back. `tccutil reset Camera|Microphone|SpeechRecognition <bundle id>` takes an answer back.

### iOS simulator: your own simulator gets no grants

Without `-Udid`, `ios.ps1` creates a simulator, grants the camera, the microphone and speech recognition to the test apps on it, and deletes it afterwards. With `-Udid <simulator>` it grants nothing on your simulator, so the tests that need these permissions skip until the apps have them. Grant them yourself before the run, once per simulator:

```bash
xcrun simctl privacy <udid> grant camera com.communitytoolkit.maui.camera.devicetests
xcrun simctl privacy <udid> grant microphone com.communitytoolkit.maui.camera.devicetests
```

Do the same for the other two bundle ids from the list above. `simctl` has no service for speech recognition; `Grant-SimulatorPrivacy` in `ios.ps1` writes that row into the TCC database of the simulator, and on your own simulator you can answer the prompt in the visual runner instead. Notifications cannot be granted either (see the Badge tests above). The simulator has no camera, so the tests that need one skip on it whatever you grant.

### Windows: consent, Developer Mode and the certificate

* A packaged app may use the camera or the microphone only after a person allowed it. The first time it tries, Windows shows a consent prompt, which a headless run cannot answer, so the tests that open them skip on a machine that has the device. Allow the app under Settings > Privacy & security > Camera or Microphone, or answer the prompt once from the visual runner. An unpackaged run follows the "Let desktop apps access your camera/microphone" settings. The CI runners have neither device, and a Windows virtual machine usually has no camera.
* The packaged run installs an MSIX that `windows.ps1` signs with a self-signed certificate. It trusts the certificate in `Cert:\LocalMachine\TrustedPeople`, which needs an elevated PowerShell the first time (see [Windows details](README.md#windows-details)). Windows must also let you install such an app: the CI job sets `AllowAllTrustedApps` and `AllowDevelopmentWithoutDevLicense` in `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock`. Turn on Developer Mode (Settings > System > For developers) if `Add-AppxPackage` is refused. Visual Studio needs Developer Mode to run the packaged app.

### Android: the emulator needs the cameras that CI gives it

`android.ps1`, and so `run.ps1`, boots an AVD with both cameras emulated (`-camera-front emulated`), because an AVD that `avdmanager` creates has no front camera, and the tests of two cameras skip without one. An emulator that you start yourself needs the same options. The on-device speech tests can wait 30 seconds for each question on an emulator whose recognizer never answers, which the CI skips with `OnDeviceSpeech`; skip them the same way with `-SkipCapabilities OnDeviceSpeech`. See [Capability declarations](README.md#capability-declarations).

### Everything else

* CI builds and runs `Release`; the scripts default to `Debug`. A Release build trims and compiles ahead of time, so a failure that only CI shows needs `-Configuration Release` (see [CI](README.md#ci)). It builds much more slowly.
* A Mac that goes to sleep stops the run in the middle; keep it awake with `caffeinate -di pwsh eng/devices/run.ps1`.
* The scripts find the Android SDK in `ANDROID_HOME`, `ANDROID_SDK_ROOT` or the folder where Android Studio installs it, and `run.ps1` skips Android without one. Visual Studio installs the SDK in `C:\Program Files (x86)\Android\android-sdk`; set `ANDROID_HOME` to that folder.
* The Mac Catalyst apps open a window while they run and need your session: stay logged in, leave the screen unlocked, and leave the window alone. Run one Mac Catalyst test app at a time.
* Run one script at a time per checkout: the builds and the results folders are shared.
