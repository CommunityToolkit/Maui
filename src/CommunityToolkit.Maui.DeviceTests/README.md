# CommunityToolkit.Maui.DeviceTests

Device tests for the .NET MAUI Community Toolkit. These tests run on actual devices/emulators and verify platform-specific behavior that cannot be tested with unit tests alone.

## Prerequisites

- .NET SDK with MAUI workloads installed (`dotnet workload install maui`)
- For Android: Android emulator or physical device connected
- For iOS/MacCatalyst: macOS with Xcode installed
- For Windows: Windows 10/11 with Windows App SDK

## Running Tests Locally

### Windows

```bash
dotnet test src/CommunityToolkit.Maui.DeviceTests/CommunityToolkit.Maui.DeviceTests.csproj -f net10.0-windows10.0.19041.0
```

Or press F5 in Visual Studio / VS Code with the Windows Machine profile selected.

### Android

```bash
# Ensure an emulator is running or a device is connected
dotnet test src/CommunityToolkit.Maui.DeviceTests/CommunityToolkit.Maui.DeviceTests.csproj -f net10.0-android
```

### iOS (macOS only)

```bash
# Ensure a simulator is available
dotnet test src/CommunityToolkit.Maui.DeviceTests/CommunityToolkit.Maui.DeviceTests.csproj -f net10.0-ios
```

### MacCatalyst (macOS only)

```bash
dotnet test src/CommunityToolkit.Maui.DeviceTests/CommunityToolkit.Maui.DeviceTests.csproj -f net10.0-maccatalyst
```

## Project Structure

```text
CommunityToolkit.Maui.DeviceTests/
├── Platforms/
│   ├── Android/          # Android-specific app entry points
│   ├── iOS/              # iOS-specific app entry points
│   ├── MacCatalyst/      # MacCatalyst-specific app entry points
│   └── Windows/          # Windows-specific app entry points
├── Resources/
│   ├── AppIcon/          # App icon assets
│   └── Splash/           # Splash screen assets
├── Properties/
│   └── launchSettings.json
├── App.cs                # MAUI Application class
├── MauiProgram.cs        # App builder / DI configuration
├── SmokeTests.cs         # Basic app boot verification tests
├── PlatformDetectionTests.cs  # Platform detection tests
├── HandlerTests.cs       # Handler creation tests
└── GlobalUsings.cs       # Assembly-level xunit configuration
```

## Adding New Tests

1. Create a new test class in the project root (or a subfolder for organization)
2. Use `[Fact]` or `[Theory]` attributes from xunit
3. For platform-specific tests, use `#if` directives or create `*.android.cs` / `*.ios.cs` / `*.windows.cs` / `*.macios.cs` files
4. Tests run sequentially (parallelization is disabled) to avoid UI threading issues

## Notes

- Tests run inside a real MAUI application on the target platform
- The app displays "Device Tests Running..." while tests execute
- Test results are reported through the standard xunit test runner output
- This project does NOT produce a NuGet package — it is test-only
