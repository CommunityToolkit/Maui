.NET MAUI Community Toolkit

## Initializing

In order to use the .NET MAUI Community Toolkit you need to call the extension method in your `MauiProgram.cs` file as follows:

```csharp
using CommunityToolkit.Maui;

public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();

    // Initialise the toolkit
	builder.UseMauiApp<App>().UseMauiCommunityToolkit();

    // the rest of your logic...
}
```

## XAML usage

In order to make use of the toolkit within XAML you can use this namespace:

xmlns:toolkit="http://schemas.microsoft.com/dotnet/2022/maui/toolkit"

!!!WARNING!!!
In order to use the custom schema mentioned above you need to call the `UseMauiCommunityToolkit` method or call any API on C# code. If you don't do that that url will not work and you need to use the `xmlns= namespace;assembly` pattern. This is a limitation of the custom schema API, more info here:
https://docs.microsoft.com/en-us/xamarin/xamarin-forms/xaml/custom-namespace-schemas#consuming-a-custom-namespace-schema

## Further information

For more information please visit:

- Our documentation site: https://docs.microsoft.com/dotnet/communitytoolkit/maui

- Our GitHub repository: https://github.com/CommunityToolkit/Maui
