﻿.NET MAUI Community Toolkit

## Initializing

In order to use the .NET MAUI Community Toolkit CameraView you need to call the extension method in your `MauiProgram.cs` file as follows:

```csharp
using CommunityToolkit.Maui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			// Initialize the .NET MAUI Community Toolkit CameraView by adding the below line of code
			.UseMauiCommunityToolkitCamera()
			// After initializing the .NET MAUI Community Toolkit, optionally add additional fonts
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Continue initializing your .NET MAUI App here

		return builder.Build();
	}
}
```

## XAML usage

In order to make use of the toolkit within XAML you can use this namespace:

xmlns:toolkit="http://schemas.microsoft.com/dotnet/2022/maui/toolkit"

## Further information

For more information please visit:

- Our documentation site: https://docs.microsoft.com/dotnet/communitytoolkit/maui

- Our GitHub repository: https://github.com/CommunityToolkit/Maui