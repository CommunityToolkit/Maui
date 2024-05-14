﻿.NET MAUI Community Toolkit

# Intramotev.Toolkit.Maui.MediaElement

This is our fork of the CommunityToolkit.Maui.MediaElement. It exposes some exoplayer buffer values to the control (only for Android).

## Initializing

In order to use the .NET MAUI Intramotev Toolkit MediaElement you need to call the extension method in your `MauiProgram.cs` file as follows:

```csharp
using Intramotev.Toolkit.Maui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			// Initialize the .NET MAUI Community Toolkit MediaElement by adding the below line of code
			.UseMauiIntramotevToolkitMediaElement()
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

## Pushing a new version to our repo

* If changes have been made, increment Version number and update PackageReleaseNotes appropriately (these are attributes in the .csproj file).
* Pack the project with (from the root folder): `dotnet pack ./src/CommunityToolkit.Maui.MediaElement/CommunityToolkit.Maui.MediaElement.csproj -c Release`
* Push it to the Intramotev packages list (from the root folder): `dotnet nuget push ./src/Intramotev.Toolkit.Maui.MediaElement/bin/Release/Intramotev.Toolkit.Maui.MediaElement.{VERSION-NUMBER}.nupkg --source "github" --api-key {YOUR-GITHUB-PAT}`