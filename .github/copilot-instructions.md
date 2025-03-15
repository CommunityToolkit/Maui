# Overview
This document provides guidelines for using GitHub Copilot to contribute to the .NET MAUI Community Toolkit. It includes instructions on setting up your environment, writing code, and following best practices specific to .NET MAUI.

## Prerequisites
1.	Install the latest stable (.NET SDK)[https://dotnet.microsoft.com/en-us/download].
2.	Install .NET MAUI workloads (we recommend using Visual Studio installer).

## Setting Up GitHub Copilot
1.	Ensure you have GitHub Copilot installed and enabled in Visual Studio.
2.	Familiarize yourself with the basic usage of GitHub Copilot by reviewing the (official documentation)[https://docs.github.com/en/copilot].

## Writing Code with GitHub Copilot
### General Guidelines
* Use GitHub Copilot to assist with code completion, documentation, and generating boilerplate code.
* Always review and test the code suggested by GitHub Copilot to ensure it meets the project's standards and requirements.

### Specific to .NET MAUI
* Ensure that any UI components or controls are compatible with .NET MAUI.
* Avoid using Xamarin.Forms-specific code unless there is a direct .NET MAUI equivalent.
* Follow the project's coding style and best practices as outlined in the (contributing)[https://github.com/CommunityToolkit/Maui/blob/main/CONTRIBUTING.md] document.

## Best Practices
* Use **Trace.WriteLine()** for debug logging instead of **Debug.WriteLine()**.
* Include a **CancellationToken** as a parameter for methods returning **Task** or **ValueTask**.
* Use **is** for null checking and type checking.
* Use file-scoped namespaces to reduce code verbosity.
* Avoid using the **!** null forgiving operator.
** Follow naming conventions for enums and property names.

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

### Bug Fixes
If you're looking for something to fix, please browse [open issues](https://github.com/CommunityToolkit/Maui/issues).

Follow the style used by the [.NET Foundation](https://github.com/dotnet/runtime/blob/master/docs/coding-guidelines/coding-style.md), with two primary exceptions:

* We do **not** use the `private` keyword as it is the default accessibility level in C#.
* We will **not** use `_` or `s_` as a prefix for internal or private field names
* We will use `camelCaseFieldName` for naming internal or private fields in both instance and static implementations

Read and follow our [Pull Request template](https://github.com/CommunityToolkit/Maui/blob/main/.github/PULL_REQUEST_TEMPLATE.md)

## Submitting Contributions
1.	Fork the repository and create a new branch for your changes.
2.	Implement your changes using GitHub Copilot as needed.
3.	Ensure your changes include tests, samples, and documentation.
4.	Open a pull request and follow the [Pull Request template](https://github.com/CommunityToolkit/Maui/blob/main/.github/PULL_REQUEST_TEMPLATE.md).

## Additional Resources
- (GitHub Copilot Documentation)[https://docs.github.com/en/copilot]
- (.NET MAUI Documentation)[https://learn.microsoft.com/en-us/dotnet/maui/]

By following these guidelines, you can effectively use GitHub Copilot to contribute to the .NET MAUI Community Toolkit. Thank you for your contributions!