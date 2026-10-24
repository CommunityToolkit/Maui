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
* Write comments only to explain **why** (a non-intuitive decision, a platform quirk or workaround, code that looks out of scope); never to explain **what** the code does. Use descriptive names instead.
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

### Comments
* Never write a comment that explains what the code does: the names of types, members and variables carry that. When code seems to need a "what" comment, rename it or extract a well-named method instead.
* Write a comment only when it explains why: a non-intuitive decision, a platform quirk or workaround (with the issue link), an ordering or timing constraint, or code that looks unexpected or out of scope.
* Keep XML documentation comments on public APIs: they document the contract for callers.

e.g.

```csharp
// Avoid - the comment repeats the code
// Check whether the user can retry
if (attempts < 3 && !isCancelled)
{
}

// Prefer - the name says what the code does
var canRetry = attempts < maximumAttempts && !isCancelled;
if (canRetry)
{
}

// Prefer - a comment that explains why
// AVPlayerItem reports an indefinite duration until its asset has loaded, so wait for ReadyToPlay before reading it
await readyToPlay.Task.WaitAsync(token);
```

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

#### Property patterns (non-null checks)
* Never use the empty property pattern `is { }` (e.g. `x is { } value`) to check for a non-null value and bind it: it is hard to read.
* Use a **type pattern** (`x is Type value`) instead, with the expression's exact type (for a nullable value type `Size?`, `is Size size`).
* For a pure non-null check with no binding, use `is not null`.

e.g.

```csharp
// Avoid
if (someColor is { } color && color != Colors.Transparent)
{
}

// Prefer
if (someColor is Color color && color != Colors.Transparent)
{
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

### Bug Fixes
If you're looking for something to fix, please browse [open issues](https://github.com/CommunityToolkit/Maui/issues).

Follow the style used by the [.NET Foundation](https://github.com/dotnet/runtime/blob/master/docs/coding-guidelines/coding-style.md), with two primary exceptions:

* We do **not** use the `private` keyword as it is the default accessibility level in C#.
* We will **not** use `_` or `s_` as a prefix for internal or private field names
* We will use `camelCaseFieldName` for naming internal or private fields in both instance and static implementations

Read and follow our [Pull Request template](https://github.com/CommunityToolkit/Maui/blob/main/.github/PULL_REQUEST_TEMPLATE.md)

## Device Tests
* Platform code that the `net10.0` unit tests cannot reach is covered by device tests that run inside real apps on Android, iOS, Mac Catalyst and Windows, built like dotnet/maui's device tests (xUnit, dotnet/maui's test runner, XHarness).
* There is one test app per shipped feature package: `src/CommunityToolkit.Maui.DeviceTests` (`CommunityToolkit.Maui`, `.Core` and `.Maps`), `src/CommunityToolkit.Maui.MediaElement.DeviceTests` and `src/CommunityToolkit.Maui.Camera.DeviceTests`.
* Give every test class a `[Category(TestCategory.X)]` and name platform-specific test files with the same suffixes as the libraries (`.android.cs`, `.ios.cs`, `.macos.cs`, `.macios.cs`, `.windows.cs`). How to write them is in [src/CommunityToolkit.Maui.DeviceTests/README.md](https://github.com/CommunityToolkit/Maui/blob/main/src/CommunityToolkit.Maui.DeviceTests/README.md).
* `dotnet test` does not run them. Use `eng/devices/run.ps1`, which runs every platform the machine can run and is the command of the `Device Tests` workflow on every pull request (one `-Platform` per job), or one platform's script (`android.ps1`, `ios.ps1`, `catalyst.ps1` or `windows.ps1`), as described in [eng/devices/LOCAL.md](https://github.com/CommunityToolkit/Maui/blob/main/eng/devices/LOCAL.md) and [eng/devices/README.md](https://github.com/CommunityToolkit/Maui/blob/main/eng/devices/README.md).

## Submitting Contributions
1.	Fork the repository and create a new branch for your changes.
2.	Implement your changes using GitHub Copilot as needed.
3.	Ensure your changes include tests, samples, and documentation.
4.	Open a pull request and follow the [Pull Request template](https://github.com/CommunityToolkit/Maui/blob/main/.github/PULL_REQUEST_TEMPLATE.md).

## Additional Resources
* [GitHub Copilot Documentation](https://docs.github.com/en/copilot)
* [.NET MAUI Documentation](https://learn.microsoft.com/en-us/dotnet/maui/)

By following these guidelines, you can effectively use GitHub Copilot to contribute to the .NET MAUI Community Toolkit. Thank you for your contributions!
