.NET MAUI Community Toolkit

## Initializing

In order to use the .NET MAUI Community Toolkit MediaPlayer you need to call the extension method in your `MauiProgram.cs` file as follows:

```csharp
using CommunityToolkit.Maui.MediaPlayer;

public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();

    // Initialise the toolkit
	builder.UseMauiApp<App>().UseMauiCommunityToolkitMediaPlayer();

    // the rest of your logic...
}
```

## Further information

For more information please visit:

- Our documentation site: https://docs.microsoft.com/dotnet/communitytoolkit/maui

- Our GitHub repository: https://github.com/CommunityToolkit/Maui