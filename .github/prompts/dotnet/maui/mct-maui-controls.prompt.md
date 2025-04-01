# Community Toolkit Controls
By following these common code styles and implementation templates, you can ensure that code is consistent, maintainable, and adheres to best practices. Always review and test code to ensure it meets our project's standards and requirements.

## UI Components and Controls
- **Good Practice**: Ensure that any UI components or controls are compatible with .NET MAUI.
- **Bad Practice**: Using Xamarin.Forms-specific code unless there is a direct .NET MAUI equivalent.

```csharp
// Good Practice
public class CustomButton : Button
{
    // .NET MAUI specific implementation
}

// Bad Practice
public class CustomButton : Xamarin.Forms.Button
{
    // Xamarin.Forms specific implementation
}
```

## Using CommunityToolkit.Maui
- **Good Practice**: Utilize `CommunityToolkit.Maui` for common behaviors, converters, and extensions.
- **Bad Practice**: Re-implementing functionality that is already provided by `CommunityToolkit.Maui`.

```csharp
// Good Practice
using CommunityToolkit.Maui.Behaviors;

public class MainPage : ContentPage
{
    public MainPage()
    {
        var entry = new Entry();
        entry.Behaviors.Add(new EventToCommandBehavior
        {
            EventName = "TextChanged",
            Command = new Command(() => { /* Command implementation */ })
        });
    }
}

// Bad Practice
public class MainPage : ContentPage
{
    public MainPage()
    {
        var entry = new Entry();
        entry.TextChanged += (s, e) => { /* Event handler implementation */ };
    }
}
```

## File Scopes Namespaces
- **Good Practice**: Use file-scoped namespaces to reduce code verbosity.
- **Bad Practice**: Using block-scoped namespaces.

```csharp
// Good Practice
namespace CommunityToolkit.Maui.Views;

using System;

class AvatarView
{
}

// Bad Practice
namespace CommunityToolkit.Maui.Views
{
    using System;

    class AvatarView
    {
    }
}
```

## Alerts
Common Code Styles and Implementation Templates for `CommunityToolkit.Maui.Alerts`

### Alert Initialization
- **Good Practice**: Use dependency injection to initialize alert services.
- **Bad Practice**: Creating instances of alert services directly within the class.

```csharp
// Good Practice
public class AlertService
{
    private readonly IAlertService _alertService;

    public AlertService(IAlertService alertService)
    {
        _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
    }

    public async Task ShowAlertAsync(string message, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        await _alertService.ShowAlertAsync(message, token);
    }
}

// Bad Practice
public class AlertService
{
    private readonly IAlertService _alertService = new AlertService();

    public async Task ShowAlertAsync(string message)
    {
        await _alertService.ShowAlertAsync(message);
    }
}
```

### Handling Alert Display
- **Good Practice**: Implement proper error handling and logging when displaying alerts.
- **Bad Practice**: Not handling exceptions or logging errors.

```csharp
// Good Practice
public async Task ShowAlertAsync(string message, CancellationToken token = default)
{
    try
    {
        token.ThrowIfCancellationRequested();
        await _alertService.ShowAlertAsync(message, token);
    }
    catch (Exception ex)
    {
        Trace.WriteLine($"Error displaying alert: {ex.Message}");
    }
}

// Bad Practice
public async Task ShowAlertAsync(string message)
{
    await _alertService.ShowAlertAsync(message);
}
```

### XML Documentation
- **Good Practice**: Provide XML documentation for public members.
- **Bad Practice**: Not providing documentation or leaving it empty.

```csharp
// Good Practice
/// <summary>
/// Displays an alert with the specified message.
/// </summary>
/// <param name="message">The message to display.</param>
/// <param name="token">The cancellation token.</param>
public async Task ShowAlertAsync(string message, CancellationToken token = default)
{
    try
    {
        token.ThrowIfCancellationRequested();
        await _alertService.ShowAlertAsync(message, token);
    }
    catch (Exception ex)
    {
        Trace.WriteLine($"Error displaying alert: {ex.Message}");
    }
}

// Bad Practice
public async Task ShowAlertAsync(string message, CancellationToken token = default)
{
    try
    {
        token.ThrowIfCancellationRequested();
        await _alertService.ShowAlertAsync(message, token);
    }
    catch (Exception ex)
    {
        Trace.WriteLine($"Error displaying alert: {ex.Message}");
    }
}
```

### Using Async/Await
- **Good Practice**: Use `async` and `await` for asynchronous operations and include a `CancellationToken` parameter.
- **Bad Practice**: Using synchronous methods for operations that can be asynchronous or not including a `CancellationToken`.

```csharp
// Good Practice
public async Task ShowAlertAsync(string message, CancellationToken token = default)
{
    token.ThrowIfCancellationRequested();
    await _alertService.ShowAlertAsync(message, token);
}

// Bad Practice
public void ShowAlert(string message)
{
    _alertService.ShowAlert(message);
}
```

### Exception Handling
- **Good Practice**: Use specific exception types and provide meaningful messages.
- **Bad Practice**: Catching general exceptions or not providing meaningful messages.

```csharp
// Good Practice
public async Task ShowAlertAsync(string message, CancellationToken token = default)
{
    try
    {
        token.ThrowIfCancellationRequested();
        await _alertService.ShowAlertAsync(message, token);
    }
    catch (ArgumentNullException ex)
    {
        Trace.WriteLine($"ArgumentNullException: {ex.Message}");
    }
    catch (Exception ex)
    {
        Trace.WriteLine($"Exception: {ex.Message}");
    }
}

// Bad Practice
public async Task ShowAlertAsync(string message, CancellationToken token = default)
{
    try
    {
        token.ThrowIfCancellationRequested();
        await _alertService.ShowAlertAsync(message, token);
    }
    catch (Exception ex)
    {
        Trace.WriteLine($"Exception: {ex.Message}");
    }
}
```

## Animations
Common Code Styles and Implementation Templates for `CommunityToolkit.Maui.Animations`

### Animation Initialization
- **Good Practice**: Use dependency injection to initialize animation services.
- **Bad Practice**: Creating instances of animation services directly within the class.

```csharp
// Good Practice
public class CustomAnimation
{
    private readonly IAnimationService _animationService;

    public CustomAnimation(IAnimationService animationService)
    {
        _animationService = animationService ?? throw new ArgumentNullException(nameof(animationService));
    }

    public async Task StartAnimationAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        await _animationService.StartAsync(token);
    }
}

// Bad Practice
public class CustomAnimation
{
    private readonly IAnimationService _animationService = new AnimationService();

    public async Task StartAnimationAsync()
    {
        await _animationService.StartAsync();
    }
}
```

### Handling Animation Completion
- **Good Practice**: Implement proper error handling and logging when completing animations.
- **Bad Practice**: Not handling exceptions or logging errors.

```csharp
// Good Practice
public async Task CompleteAnimationAsync(CancellationToken token = default)
{
    try
    {
        token.ThrowIfCancellationRequested();
        await _animationService.CompleteAsync(token);
    }
    catch (Exception ex)
    {
        Trace.WriteLine($"Error completing animation: {ex.Message}");
    }
}

// Bad Practice
public async Task CompleteAnimationAsync()
{
    await _animationService.CompleteAsync();
}
```

### XML Documentation
- **Good Practice**: Provide XML documentation for public members.
- **Bad Practice**: Not providing documentation or leaving it empty.

```csharp
// Good Practice
/// <summary>Starts the animation.</summary>
/// <param name="token">The cancellation token.</param>
public async Task StartAnimationAsync(CancellationToken token = default)
{
    try
    {
        token.ThrowIfCancellationRequested();
        await _animationService.StartAsync(token);
    }
    catch (Exception ex)
    {
        Trace.WriteLine($"Error starting animation: {ex.Message}");
    }
}

// Bad Practice
public async Task StartAnimationAsync(CancellationToken token = default)
{
    try
    {
        token.ThrowIfCancellationRequested();
        await _animationService.StartAsync(token);
    }
    catch (Exception ex)
    {
        Trace.WriteLine($"Error starting animation: {ex.Message}");
    }
}
```

### Using Async/Await
- **Good Practice**: Use `async` and `await` for asynchronous operations and include a `CancellationToken` parameter.
- **Bad Practice**: Using synchronous methods for operations that can be asynchronous or not including a `CancellationToken`.

```csharp
// Good Practice
public async Task StartAnimationAsync(CancellationToken token = default)
{
    token.ThrowIfCancellationRequested();
    await _animationService.StartAsync(token);
}

// Bad Practice
public void StartAnimation()
{
    _animationService.Start();
}
```

### Exception Handling
- **Good Practice**: Use specific exception types and provide meaningful messages.
- **Bad Practice**: Catching general exceptions or not providing meaningful messages.

```csharp
// Good Practice
public async Task StartAnimationAsync(CancellationToken token = default)
{
    try
    {
        token.ThrowIfCancellationRequested();
        await _animationService.StartAsync(token);
    }
    catch (ArgumentNullException ex)
    {
        Trace.WriteLine($"ArgumentNullException: {ex.Message}");
    }
    catch (Exception ex)
    {
        Trace.WriteLine($"Exception: {ex.Message}");
    }
}

// Bad Practice
public async Task StartAnimationAsync(CancellationToken token = default)
{
    try
    {
        token.ThrowIfCancellationRequested();
        await _animationService.StartAsync(token);
    }
    catch (Exception ex)
    {
        Trace.WriteLine($"Exception: {ex.Message}");
    }
}
```

### Animation Configuration
- **Good Practice**: Use configuration objects to manage animation settings.
- **Bad Practice**: Hardcoding animation settings within the class.

```csharp
// Good Practice
public class CustomAnimation
{
    private readonly IAnimationService _animationService;
    private readonly AnimationConfig _config;

    public CustomAnimation(IAnimationService animationService, AnimationConfig config)
    {
        _animationService = animationService ?? throw new ArgumentNullException(nameof(animationService));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public async Task StartAnimationAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        await _animationService.StartAsync(_config, token);
    }
}

// Bad Practice
public class CustomAnimation
{
    private readonly IAnimationService _animationService = new AnimationService();

    public async Task StartAnimationAsync()
    {
        var config = new AnimationConfig
        {
            Duration = TimeSpan.FromSeconds(1),
            Easing = Easing.Linear
        };
        await _animationService.StartAsync(config);
    }
}
```

## Converters
Common Code Styles and Implementation Templates for `CommunityToolkit.Maui.Converters`

### Converter Implementation
- **Good Practice**: Implement `IValueConverter` or `IMultiValueConverter` for creating converters.
- **Bad Practice**: Not implementing the required interfaces or leaving methods unimplemented.

```csharp
// Good Practice
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility == Visibility.Visible;
        }
        return false;
    }
}

// Bad Practice
public class BoolToVisibilityConverter
{
    // Missing interface implementation
}
```

### Null Checking
- **Good Practice**: Use `is` for null checking.
- **Bad Practice**: Using `==` for null checking.

```csharp
// Good Practice
if (value is null)
{
    return Visibility.Collapsed;
}

// Bad Practice
if (value == null)
{
    return Visibility.Collapsed;
}
```

### Type Checking
- **Good Practice**: Use `is` for type checking.
- **Bad Practice**: Using casting for type checking.

```csharp
// Good Practice
if (value is bool boolValue)
{
    return boolValue ? Visibility.Visible : Visibility.Collapsed;
}

// Bad Practice
var boolValue = value as bool?;
if (boolValue != null)
{
    return boolValue.Value ? Visibility.Visible : Visibility.Collapsed;
}
```

### XML Documentation
- **Good Practice**: Provide XML documentation for public members.
- **Bad Practice**: Not providing documentation or leaving it empty.

```csharp
// Good Practice
/// <summary>Converts a boolean value to a visibility value.</summary>
/// <param name="value">The boolean value.</param>
/// <param name="targetType">The target type.</param>
/// <param name="parameter">The converter parameter.</param>
/// <param name="culture">The culture.</param>
/// <returns>Visibility.Visible if true, otherwise Visibility.Collapsed.</returns>
public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
{
    if (value is bool boolValue)
    {
        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }
    return Visibility.Collapsed;
}

// Bad Practice
public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
{
    if (value is bool boolValue)
    {
        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }
    return Visibility.Collapsed;
}
```

### Exception Handling
- **Good Practice**: Use specific exception types and provide meaningful messages.
- **Bad Practice**: Catching general exceptions or not providing meaningful messages.

```csharp
// Good Practice
public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
{
    try
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        throw new ArgumentException("Expected boolean value", nameof(value));
    }
    catch (ArgumentException ex)
    {
        Trace.WriteLine($"ArgumentException: {ex.Message}");
        return Visibility.Collapsed;
    }
}

// Bad Practice
public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
{
    try
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        throw new Exception("Invalid value");
    }
    catch (Exception ex)
    {
        Trace.WriteLine($"Exception: {ex.Message}");
        return Visibility.Collapsed;
    }
}
```

### Naming Conventions
- **Good Practice**: Follow consistent naming conventions for methods, properties, and variables.
- **Bad Practice**: Using inconsistent or unclear names.

```csharp
// Good Practice
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility == Visibility.Visible;
        }
        return false;
    }
}

// Bad Practice
public class BoolToVisConverter : IValueConverter
{
    public object Conv(object val, Type tgtType, object param, CultureInfo cult)
    {
        if (val is bool bVal)
        {
            return bVal ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvBack(object val, Type tgtType, object param, CultureInfo cult)
    {
        if (val is Visibility vis)
        {
            return vis == Visibility.Visible;
        }
        return false;
    }
}
```

## Essentials
Common Code Styles and Implementation Templates for `CommunityToolkit.Maui.Essentials`

### Using Essentials APIs
- **Good Practice**: Use `CommunityToolkit.Maui.Essentials` APIs for accessing device features.
- **Bad Practice**: Using platform-specific code directly.

```csharp
// Good Practice
public class DeviceInfoService
{
    public string GetDeviceModel()
    {
        return DeviceInfo.Model;
    }
}

// Bad Practice
public class DeviceInfoService
{
    public string GetDeviceModel()
    {
#if ANDROID
        return Android.OS.Build.Model;
#elif IOS
        return UIKit.UIDevice.CurrentDevice.Model;
#endif
    }
}
```

### Handling Permissions
- **Good Practice**: Use `Permissions` API to request and check permissions.
- **Bad Practice**: Not handling permissions or using platform-specific code.

```csharp
// Good Practice
public async Task<PermissionStatus> CheckAndRequestLocationPermissionAsync()
{
    var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
    if (status != PermissionStatus.Granted)
    {
        status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
    }
    return status;
}

// Bad Practice
public async Task<bool> CheckAndRequestLocationPermissionAsync()
{
#if ANDROID
    var status = ContextCompat.CheckSelfPermission(Android.App.Application.Context, Manifest.Permission.AccessFineLocation);
    if (status != Permission.Granted)
    {
        ActivityCompat.RequestPermissions(MainActivity.Instance, new[] { Manifest.Permission.AccessFineLocation }, 0);
    }
    return status == Permission.Granted;
#elif IOS
    var status = CLLocationManager.Status;
    if (status != CLAuthorizationStatus.AuthorizedWhenInUse)
    {
        locationManager.RequestWhenInUseAuthorization();
    }
    return status == CLAuthorizationStatus.AuthorizedWhenInUse;
#endif
}
```

### Using Async/Await
- **Good Practice**: Use `async` and `await` for asynchronous operations and include a `CancellationToken` parameter.
- **Bad Practice**: Using synchronous methods for operations that can be asynchronous or not including a `CancellationToken`.

```csharp
// Good Practice
public async Task<Location> GetLocationAsync(CancellationToken token = default)
{
    token.ThrowIfCancellationRequested();
    return await Geolocation.GetLocationAsync(new GeolocationRequest(), token);
}

// Bad Practice
public Location GetLocation()
{
    return Geolocation.GetLocationAsync(new GeolocationRequest()).Result;
}
```

### Exception Handling
- **Good Practice**: Use specific exception types and provide meaningful messages.
- **Bad Practice**: Catching general exceptions or not providing meaningful messages.

```csharp
// Good Practice
public async Task<Location> GetLocationAsync(CancellationToken token = default)
{
    try
    {
        token.ThrowIfCancellationRequested();
        return await Geolocation.GetLocationAsync(new GeolocationRequest(), token);
    }
    catch (FeatureNotSupportedException ex)
    {
        Trace.WriteLine($"FeatureNotSupportedException: {ex.Message}");
        return null;
    }
    catch (PermissionException ex)
    {
        Trace.WriteLine($"PermissionException: {ex.Message}");
        return null;
    }
    catch (Exception ex)
    {
        Trace.WriteLine($"Exception: {ex.Message}");
        return null;
    }
}

// Bad Practice
public async Task<Location> GetLocationAsync(CancellationToken token = default)
{
    try
    {
        token.ThrowIfCancellationRequested();
        return await Geolocation.GetLocationAsync(new GeolocationRequest(), token);
    }
    catch (Exception ex)
    {
        Trace.WriteLine($"Exception: {ex.Message}");
        return null;
    }
}
```

### XML Documentation
- **Good Practice**: Provide XML documentation for public members.
- **Bad Practice**: Not providing documentation or leaving it empty.

```csharp
// Good Practice
/// <summary>Gets the current location.</summary>
/// <param name="token">The cancellation token.</param>
/// <returns>The current location.</returns>
public async Task<Location> GetLocationAsync(CancellationToken token = default)
{
    try
    {
        token.ThrowIfCancellationRequested();
        return await Geolocation.GetLocationAsync(new GeolocationRequest(), token);
    }
    catch (Exception ex)
    {
        Trace.WriteLine($"Exception: {ex.Message}");
        return null;
    }
}

// Bad Practice
public async Task<Location> GetLocationAsync(CancellationToken token = default)
{
    try
    {
        token.ThrowIfCancellationRequested();
        return await Geolocation.GetLocationAsync(new GeolocationRequest(), token);
    }
    catch (Exception ex)
    {
        Trace.WriteLine($"Exception: {ex.Message}");
        return null;
    }
}
```

### Using Dependency Injection
- **Good Practice**: Use dependency injection to manage dependencies and improve testability.
- **Bad Practice**: Creating instances of dependencies directly within the class.

```csharp
// Good Practice
public class LocationService
{
    private readonly IGeolocation _geolocation;

    public LocationService(IGeolocation geolocation)
    {
        _geolocation = geolocation ?? throw new ArgumentNullException(nameof(geolocation));
    }

    public async Task<Location> GetLocationAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        return await _geolocation.GetLocationAsync(new GeolocationRequest(), token);
    }
}

// Bad Practice
public class LocationService
{
    private readonly IGeolocation _geolocation = new Geolocation();

    public async Task<Location> GetLocationAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        return await _geolocation.GetLocationAsync(new GeolocationRequest(), token);
    }
}
```

## Extensions
Common Code Styles and Implementation Templates for `CommunityToolkit.Maui.Extensions`

### Extension Method Implementation
- **Good Practice**: Implement extension methods in static classes and use the `this` keyword to specify the type being extended.
- **Bad Practice**: Implementing extension methods in non-static classes or not using the `this` keyword.

```csharp
// Good Practice
public static class StringExtensions
{
    public static bool IsNullOrEmpty(this string value)
    {
        return string.IsNullOrEmpty(value);
    }
}

// Bad Practice
public class StringExtensions
{
    public bool IsNullOrEmpty(string value)
    {
        return string.IsNullOrEmpty(value);
    }
}
```

### Naming Conventions
- **Good Practice**: Follow consistent naming conventions for extension methods.
- **Bad Practice**: Using inconsistent or unclear names.

```csharp
// Good Practice
public static class StringExtensions
{
    public static bool IsNullOrEmpty(this string value)
    {
        return string.IsNullOrEmpty(value);
    }
}

// Bad Practice
public static class StringExtensions
{
    public static bool IsNull(this string value)
    {
        return string.IsNullOrEmpty(value);
    }
}
```

### XML Documentation
- **Good Practice**: Provide XML documentation for public extension methods.
- **Bad Practice**: Not providing documentation or leaving it empty.

```csharp
// Good Practice
/// <summary>Determines whether the specified string is null or empty.</summary>
/// <param name="value">The string to check.</param>
/// <returns>true if the string is null or empty; otherwise, false.</returns>
public static bool IsNullOrEmpty(this string value)
{
    return string.IsNullOrEmpty(value);
}

// Bad Practice
public static bool IsNullOrEmpty(this string value)
{
    return string.IsNullOrEmpty(value);
}
```

### Exception Handling
- **Good Practice**: Use specific exception types and provide meaningful messages.
- **Bad Practice**: Catching general exceptions or not providing meaningful messages.

```csharp
// Good Practice
public static int ToInt(this string value)
{
    try
    {
        return int.Parse(value);
    }
    catch (FormatException ex)
    {
        Trace.WriteLine($"FormatException: {ex.Message}");
        return 0;
    }
}

// Bad Practice
public static int ToInt(this string value)
{
    try
    {
        return int.Parse(value);
    }
    catch (Exception ex)
    {
        Trace.WriteLine($"Exception: {ex.Message}");
        return 0;
    }
}
```

### Using Async/Await
- **Good Practice**: Use `async` and `await` for asynchronous extension methods and include a `CancellationToken` parameter.
- **Bad Practice**: Using synchronous methods for operations that can be asynchronous or not including a `CancellationToken`.

```csharp
// Good Practice
public static async Task<string> DownloadStringAsync(this HttpClient client, string url, CancellationToken token = default)
{
    token.ThrowIfCancellationRequested();
    return await client.GetStringAsync(url, token);
}

// Bad Practice
public static string DownloadString(this HttpClient client, string url)
{
    return client.GetStringAsync(url).Result;
}
```

### Handling Null Values
- **Good Practice**: Check for null values and handle them appropriately.
- **Bad Practice**: Not checking for null values, leading to potential `NullReferenceException`.

```csharp
// Good Practice
public static string ToUpperSafe(this string value)
{
    return value?.ToUpper() ?? string.Empty;
}

// Bad Practice
public static string ToUpperSafe(this string value)
{
    return value.ToUpper();
}
```

## Handler Implementation
Common Code Styles and Implementation Templates for `CommunityToolkit.Maui.HandlerImplementation`

### Handler Initialization
- **Good Practice**: Use dependency injection to initialize handlers.
- **Bad Practice**: Creating instances of handlers directly within the class.

```csharp
// Good Practice
public class CustomHandler
{
    private readonly IHandlerService _handlerService;

    public CustomHandler(IHandlerService handlerService)
    {
        _handlerService = handlerService ?? throw new ArgumentNullException(nameof(handlerService));
    }

    public void InitializeHandler()
    {
        _handlerService.Initialize();
    }
}

// Bad Practice
public class CustomHandler
{
    private readonly IHandlerService _handlerService = new HandlerService();

    public void InitializeHandler()
    {
        _handlerService.Initialize();
    }
}
```

### Handling Events
- **Good Practice**: Implement proper error handling and logging when handling events.
- **Bad Practice**: Not handling exceptions or logging errors.

```csharp
// Good Practice
public void HandleEvent()
{
    try
    {
        // Event handling logic
    }
    catch (Exception ex)
    {
        Trace.WriteLine($"Error handling event: {ex.Message}");
    }
}

// Bad Practice
public void HandleEvent()
{
    // Event handling logic
}
```

### XML Documentation
- **Good Practice**: Provide XML documentation for public members.
- **Bad Practice**: Not providing documentation or leaving it empty.

```csharp
// Good Practice
/// <summary>Initializes the handler.</summary>
public void InitializeHandler()
{
    _handlerService.Initialize();
}

// Bad Practice
public void InitializeHandler()
{
    _handlerService.Initialize();
}
```

### Using Async/Await
- **Good Practice**: Use `async` and `await` for asynchronous operations and include a `CancellationToken` parameter.
- **Bad Practice**: Using synchronous methods for operations that can be asynchronous or not including a `CancellationToken`.

```csharp
// Good Practice
public async Task HandleEventAsync(CancellationToken token = default)
{
    token.ThrowIfCancellationRequested();
    await _handlerService.HandleEventAsync(token);
}

// Bad Practice
public void HandleEvent()
{
    _handlerService.HandleEventAsync().Wait();
}
```

### Exception Handling
- **Good Practice**: Use specific exception types and provide meaningful messages.
- **Bad Practice**: Catching general exceptions or not providing meaningful messages.

```csharp
// Good Practice
public void InitializeHandler()
{
    try
    {
        _handlerService.Initialize();
    }
    catch (ArgumentNullException ex)
    {
        Trace.WriteLine($"ArgumentNullException: {ex.Message}");
    }
    catch (Exception ex)
    {
        Trace.WriteLine($"Exception: {ex.Message}");
    }
}

// Bad Practice
public void InitializeHandler()
{
    try
    {
        _handlerService.Initialize();
    }
    catch (Exception ex)
    {
        Trace.WriteLine($"Exception: {ex.Message}");
    }
}
```

### Naming Conventions
- **Good Practice**: Follow consistent naming conventions for methods, properties, and variables.
- **Bad Practice**: Using inconsistent or unclear names.

```csharp
// Good Practice
public class CustomHandler
{
    public void InitializeHandler()
    {
        _handlerService.Initialize();
    }
}

// Bad Practice
public class CustomHandler
{
    public void InitHandler()
    {
        _handlerService.Initialize();
    }
}
```

## Image Sources
Common Code Styles and Implementation Templates for `CommunityToolkit.Maui.ImageSources`

### Image Source Initialization
- **Good Practice**: Use dependency injection to initialize image sources.
- **Bad Practice**: Creating instances of image sources directly within the class.

```csharp
// Good Practice
public class CustomImageSource
{
    private readonly IImageService _imageService;

    public CustomImageSource(IImageService imageService)
    {
        _imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
    }

    public async Task<ImageSource> GetImageSourceAsync(string imageUrl, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        return await _imageService.LoadImageAsync(imageUrl, token);
    }
}

// Bad Practice
public class CustomImageSource
{
    private readonly IImageService _imageService = new ImageService();

    public async Task<ImageSource> GetImageSourceAsync(string imageUrl)
    {
        return await _imageService.LoadImageAsync(imageUrl);
    }
}
```

### Handling Image Loading
- **Good Practice**: Implement proper error handling and logging when loading images.
- **Bad Practice**: Not handling exceptions or logging errors.

```csharp
// Good Practice
public async Task<ImageSource> GetImageSourceAsync(string imageUrl, CancellationToken token = default)
{
    try
    {
        token.ThrowIfCancellationRequested();
        return await _imageService.LoadImageAsync(imageUrl, token);
    }
    catch (Exception ex)
    {
        Trace.WriteLine($"Error loading image: {ex.Message}");
        return null;
    }
}

// Bad Practice
public async Task<ImageSource> GetImageSourceAsync(string imageUrl)
{
    return await _imageService.LoadImageAsync(imageUrl);
}
```

### Caching Image Sources
- **Good Practice**: Use caching mechanisms to improve performance and reduce network usage.
- **Bad Practice**: Not implementing caching for frequently used images.

```csharp
// Good Practice
public class CachedImageSource
{
    private readonly IImageService _imageService;
    private readonly IMemoryCache _cache;

    public CachedImageSource(IImageService imageService, IMemoryCache cache)
    {
        _imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<ImageSource> GetImageSourceAsync(string imageUrl, CancellationToken token = default)
    {
        if (_cache.TryGetValue(imageUrl, out ImageSource cachedImage))
        {
            return cachedImage;
        }

        token.ThrowIfCancellationRequested();
        var imageSource = await _imageService.LoadImageAsync(imageUrl, token);
        _cache.Set(imageUrl, imageSource);
        return imageSource;
    }
}

// Bad Practice
public class CachedImageSource
{
    private readonly IImageService _imageService;

    public CachedImageSource(IImageService imageService)
    {
        _imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
    }

    public async Task<ImageSource> GetImageSourceAsync(string imageUrl)
    {
        return await _imageService.LoadImageAsync(imageUrl);
    }
}
```

### XML Documentation
- **Good Practice**: Provide XML documentation for public members.
- **Bad Practice**: Not providing documentation or leaving it empty.

```csharp
// Good Practice
/// <summary>
/// Loads an image from the specified URL.
/// </summary>
/// <param name="imageUrl">The URL of the image.</param>
/// <param name="token">The cancellation token.</param>
/// <returns>The loaded image source.</returns>
public async Task<ImageSource> GetImageSourceAsync(string imageUrl, CancellationToken token = default)
{
    try
    {
        token.ThrowIfCancellationRequested();
        return await _imageService.LoadImageAsync(imageUrl, token);
    }
    catch (Exception ex)
    {
        Trace.WriteLine($"Error loading image: {ex.Message}");
        return null;
    }
}

// Bad Practice
public async Task<ImageSource> GetImageSourceAsync(string imageUrl, CancellationToken token = default)
{
    try
    {
        token.ThrowIfCancellationRequested();
        return await _imageService.LoadImageAsync(imageUrl, token);
    }
    catch (Exception ex)
    {
        Trace.WriteLine($"Error loading image: {ex.Message}");
        return null;
    }
}
```

## Layouts
Common Code Styles and Implementation Templates for `CommunityToolkit.Maui.Layouts`

### Layout Initialization
- **Good Practice**: Use constructors to initialize layout properties and set up child elements.
- **Bad Practice**: Leaving constructors empty or not initializing necessary properties.

```csharp
// Good Practice
public class CustomLayout : StackLayout
{
    public CustomLayout()
    {
        Spacing = 10;
        Padding = new Thickness(5);
        Children.Add(new Label { Text = "Hello, World!" });
    }
}

// Bad Practice
public class CustomLayout : StackLayout
{
    public CustomLayout()
    {
        // Empty constructor
    }
}
```

### Handling Layout Changes
- **Good Practice**: Override `OnSizeAllocated` to handle layout changes and adjust child elements accordingly.
- **Bad Practice**: Not handling layout changes or using event handlers for layout changes.

```csharp
// Good Practice
public class CustomLayout : StackLayout
{
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        // Adjust child elements based on new size
    }
}

// Bad Practice
public class CustomLayout : StackLayout
{
    public CustomLayout()
    {
        SizeChanged += OnSizeChanged;
    }

    private void OnSizeChanged(object sender, EventArgs e)
    {
        // Adjust child elements based on new size
    }
}
```

### XML Documentation
- **Good Practice**: Provide XML documentation for public members.
- **Bad Practice**: Not providing documentation or leaving it empty.

```csharp
// Good Practice
/// <summary>
/// Custom layout that arranges child elements in a stack.
/// </summary>
public class CustomLayout : StackLayout
{
    public CustomLayout()
    {
        Spacing = 10;
        Padding = new Thickness(5);
        Children.Add(new Label { Text = "Hello, World!" });
    }
}

// Bad Practice
public class CustomLayout : StackLayout
{
    public CustomLayout()
    {
        Spacing = 10;
        Padding = new Thickness(5);
        Children.Add(new Label { Text = "Hello, World!" });
    }
}
```

### Using Bindable Properties
- **Good Practice**: Use `BindableProperty.Create` to define bindable properties with appropriate default values and property changed callbacks.
- **Bad Practice**: Not providing default values or property changed callbacks.

```csharp
// Good Practice
public class CustomLayout : StackLayout
{
    public static readonly BindableProperty SpacingProperty = BindableProperty.Create(
        nameof(Spacing), 
        typeof(double), 
        typeof(CustomLayout), 
        defaultValue: 10.0, 
        propertyChanged: OnSpacingPropertyChanged);

    public double Spacing
    {
        get => (double)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    private static void OnSpacingPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var layout = (CustomLayout)bindable;
        layout.Spacing = (double)newValue;
    }
}

// Bad Practice
public class CustomLayout : StackLayout
{
    public static readonly BindableProperty SpacingProperty = BindableProperty.Create(
        nameof(Spacing), 
        typeof(double), 
        typeof(CustomLayout));

    public double Spacing
    {
        get => (double)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }
}
```

### Handling Null Values
- **Good Practice**: Check for null values and handle them appropriately.
- **Bad Practice**: Not checking for null values, leading to potential `NullReferenceException`.

```csharp
// Good Practice
public class CustomLayout : StackLayout
{
    public CustomLayout()
    {
        var label = new Label { Text = "Hello, World!" };
        if (label != null)
        {
            Children.Add(label);
        }
    }
}

// Bad Practice
public class CustomLayout : StackLayout
{
    public CustomLayout()
    {
        var label = new Label { Text = "Hello, World!" };
        Children.Add(label);
    }
}
```

### Using Async/Await
- **Good Practice**: Use `async` and `await` for asynchronous operations and include a `CancellationToken` parameter.
- **Bad Practice**: Using synchronous methods for operations that can be asynchronous or not including a `CancellationToken`.

```csharp
// Good Practice
public class CustomLayout : StackLayout
{
    public async Task LoadDataAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        // Asynchronous operation
    }
}

// Bad Practice
public class CustomLayout : StackLayout
{
    public void LoadData()
    {
        // Synchronous operation
    }
}
```

## Platform Configuration
Common Code Styles and Implementation Templates for `CommunityToolkit.Maui.PlatformConfiguration`

### Platform-Specific Configuration
- **Good Practice**: Use platform-specific directives to implement platform-specific configurations.
- **Bad Practice**: Mixing platform-specific code without directives.

```csharp
// Good Practice
public static class StatusBarConfiguration
{
    public static void SetStatusBarColor(Color color)
    {
#if ANDROID
        var activity = Platform.CurrentActivity;
        activity?.Window?.SetStatusBarColor(color.ToPlatformColor());
#elif IOS
        var statusBar = UIApplication.SharedApplication.ValueForKey(new NSString("statusBar")) as UIView;
        statusBar?.SetBackgroundColor(color.ToPlatformColor());
#endif
    }
}

// Bad Practice
public static class StatusBarConfiguration
{
    public static void SetStatusBarColor(Color color)
    {
        var activity = Platform.CurrentActivity;
        activity?.Window?.SetStatusBarColor(color.ToPlatformColor());

        var statusBar = UIApplication.SharedApplication.ValueForKey(new NSString("statusBar")) as UIView;
        statusBar?.SetBackgroundColor(color.ToPlatformColor());
    }
}
```

### Using Dependency Injection
- **Good Practice**: Use dependency injection to manage platform-specific services.
- **Bad Practice**: Creating instances of platform-specific services directly within the class.

```csharp
// Good Practice
public class PlatformService
{
    private readonly IPlatformSpecificService _platformSpecificService;

    public PlatformService(IPlatformSpecificService platformSpecificService)
    {
        _platformSpecificService = platformSpecificService ?? throw new ArgumentNullException(nameof(platformSpecificService));
    }

    public void PerformOperation()
    {
        _platformSpecificService.PerformOperation();
    }
}

// Bad Practice
public class PlatformService
{
    private readonly IPlatformSpecificService _platformSpecificService = new PlatformSpecificService();

    public void PerformOperation()
    {
        _platformSpecificService.PerformOperation();
    }
}
```

### XML Documentation
- **Good Practice**: Provide XML documentation for public members.
- **Bad Practice**: Not providing documentation or leaving it empty.

```csharp
// Good Practice
/// <summary>Sets the status bar color.</summary>
/// <param name="color">The color to set.</param>
public static void SetStatusBarColor(Color color)
{
#if ANDROID
    var activity = Platform.CurrentActivity;
    activity?.Window?.SetStatusBarColor(color.ToPlatformColor());
#elif IOS
    var statusBar = UIApplication.SharedApplication.ValueForKey(new NSString("statusBar")) as UIView;
    statusBar?.SetBackgroundColor(color.ToPlatformColor());
#endif
}

// Bad Practice
public static void SetStatusBarColor(Color color)
{
#if ANDROID
    var activity = Platform.CurrentActivity;
    activity?.Window?.SetStatusBarColor(color.ToPlatformColor());
#elif IOS
    var statusBar = UIApplication.SharedApplication.ValueForKey(new NSString("statusBar")) as UIView;
    statusBar?.SetBackgroundColor(color.ToPlatformColor());
#endif
}
```

### Handling Platform Differences
- **Good Practice**: Use platform-specific extensions to handle differences in platform implementations.
- **Bad Practice**: Using conditional compilation without encapsulating platform-specific logic.

```csharp
// Good Practice
public static class PlatformExtensions
{
    public static void SetStatusBarColor(this Window window, Color color)
    {
#if ANDROID
        window.SetStatusBarColor(color.ToPlatformColor());
#elif IOS
        var statusBar = UIApplication.SharedApplication.ValueForKey(new NSString("statusBar")) as UIView;
        statusBar?.SetBackgroundColor(color.ToPlatformColor());
#endif
    }
}

// Bad Practice
public static class StatusBarConfiguration
{
    public static void SetStatusBarColor(Color color)
    {
#if ANDROID
        var activity = Platform.CurrentActivity;
        activity?.Window?.SetStatusBarColor(color.ToPlatformColor());
#elif IOS
        var statusBar = UIApplication.SharedApplication.ValueForKey(new NSString("statusBar")) as UIView;
        statusBar?.SetBackgroundColor(color.ToPlatformColor());
#endif
    }
}
```

### Using Async/Await
- **Good Practice**: Use `async` and `await` for asynchronous operations and include a `CancellationToken` parameter.
- **Bad Practice**: Using synchronous methods for operations that can be asynchronous or not including a `CancellationToken`.

```csharp
// Good Practice
public static async Task SetStatusBarColorAsync(Color color, CancellationToken token = default)
{
    token.ThrowIfCancellationRequested();
#if ANDROID
    var activity = Platform.CurrentActivity;
    await Task.Run(() => activity?.Window?.SetStatusBarColor(color.ToPlatformColor()), token);
#elif IOS
    var statusBar = UIApplication.SharedApplication.ValueForKey(new NSString("statusBar")) as UIView;
    await Task.Run(() => statusBar?.SetBackgroundColor(color.ToPlatformColor()), token);
#endif
}

// Bad Practice
public static void SetStatusBarColor(Color color)
{
#if ANDROID
    var activity = Platform.CurrentActivity;
    activity?.Window?.SetStatusBarColor(color.ToPlatformColor());
#elif IOS
    var statusBar = UIApplication.SharedApplication.ValueForKey(new NSString("statusBar")) as UIView;
    statusBar?.SetBackgroundColor(color.ToPlatformColor());
#endif
}
```

## Popup
Common Code Styles and Implementation Templates for `CommunityToolkit.Maui.Popup`

### Popup Initialization
- **Good Practice**: Use constructors to initialize popup properties and set up child elements.
- **Bad Practice**: Leaving constructors empty or not initializing necessary properties.

```csharp
// Good Practice
public class CustomPopup : Popup
{
    public CustomPopup()
    {
        Content = new Label { Text = "Hello, World!" };
        CanBeDismissedByTappingOutsideOfPopup = true;
    }
}

// Bad Practice
public class CustomPopup : Popup
{
    public CustomPopup()
    {
        // Empty constructor
    }
}
```

### Handling Popup Events
- **Good Practice**: Implement event handlers for popup events such as appearing and disappearing.
- **Bad Practice**: Not handling events or leaving event handlers empty.

```csharp
// Good Practice
public class CustomPopup : Popup
{
    public CustomPopup()
    {
        Opened += OnPopupOpened;
        Closed += OnPopupClosed;
    }

    private void OnPopupOpened(object? sender, EventArgs e)
    {
        // Handle popup opened event
    }

    private void OnPopupClosed(object? sender, EventArgs e)
    {
        // Handle popup closed event
    }
}

// Bad Practice
public class CustomPopup : Popup
{
    public CustomPopup()
    {
        Opened += OnPopupOpened;
        Closed += OnPopupClosed;
    }

    private void OnPopupOpened(object? sender, EventArgs e)
    {
        // Empty handler
    }

    private void OnPopupClosed(object? sender, EventArgs e)
    {
        // Empty handler
    }
}
```

### XML Documentation
- **Good Practice**: Provide XML documentation for public members.
- **Bad Practice**: Not providing documentation or leaving it empty.

```csharp
// Good Practice
/// <summary>Custom popup that displays a message.</summary>
public class CustomPopup : Popup
{
    public CustomPopup()
    {
        Content = new Label { Text = "Hello, World!" };
        CanBeDismissedByTappingOutsideOfPopup = true;
    }
}

// Bad Practice
public class CustomPopup : Popup
{
    public CustomPopup()
    {
        Content = new Label { Text = "Hello, World!" };
        CanBeDismissedByTappingOutsideOfPopup = true;
    }
}
```

### Using Async/Await
- **Good Practice**: Use `async` and `await` for asynchronous operations and include a `CancellationToken` parameter.
- **Bad Practice**: Using synchronous methods for operations that can be asynchronous or not including a `CancellationToken`.

```csharp
// Good Practice
public class CustomPopup : Popup
{
    public async Task ShowPopupAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        await ShowAsync(token);
    }
}

// Bad Practice
public class CustomPopup : Popup
{
    public void ShowPopup()
    {
        Show();
    }
}
```

### Exception Handling
- **Good Practice**: Use specific exception types and provide meaningful messages.
- **Bad Practice**: Catching general exceptions or not providing meaningful messages.

```csharp
// Good Practice
public class CustomPopup : Popup
{
    public async Task ShowPopupAsync(CancellationToken token = default)
    {
        try
        {
            token.ThrowIfCancellationRequested();
            await ShowAsync(token);
        }
        catch (ArgumentNullException ex)
        {
            Trace.WriteLine($"ArgumentNullException: {ex.Message}");
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Exception: {ex.Message}");
        }
    }
}

// Bad Practice
public class CustomPopup : Popup
{
    public async Task ShowPopupAsync(CancellationToken token = default)
    {
        try
        {
            token.ThrowIfCancellationRequested();
            await ShowAsync(token);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Exception: {ex.Message}");
        }
    }
}
```

### Dependency Injection
- **Good Practice**: Use dependency injection to manage dependencies and improve testability.
- **Bad Practice**: Creating instances of dependencies directly within the class.

```csharp
// Good Practice
public class CustomPopup : Popup
{
    private readonly ILoggingService _loggingService;

    public CustomPopup(ILoggingService loggingService)
    {
        _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        Content = new Label { Text = "Hello, World!" };
    }
}

// Bad Practice
public class CustomPopup : Popup
{
    private readonly ILoggingService _loggingService = new LoggingService();

    public CustomPopup()
    {
        Content = new Label { Text = "Hello, World!" };
    }
}
```

## Views
Common Code Styles and Implementation Templates for `CommunityToolkit.Maui.Views`

### Bindable Properties
- **Good Practice**: Use `BindableProperty.Create` to define bindable properties with appropriate default values and property changed callbacks.
- **Bad Practice**: Not providing default values or property changed callbacks.

```csharp
// Good Practice
public static readonly BindableProperty BorderColorProperty = BindableProperty.Create(
    nameof(BorderColor), 
    typeof(Color), 
    typeof(AvatarView), 
    defaultValue: AvatarViewDefaults.DefaultBorderColor, 
    propertyChanged: OnBorderColorPropertyChanged);

// Bad Practice
public static readonly BindableProperty BorderColorProperty = BindableProperty.Create(
    nameof(BorderColor), 
    typeof(Color), 
    typeof(AvatarView));
```

### Property Changed Callbacks
- **Good Practice**: Implement property changed callbacks to handle changes in bindable properties.
- **Bad Practice**: Not implementing property changed callbacks or leaving them empty.

```csharp
// Good Practice
static void OnBorderColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
{
    AvatarView avatarView = (AvatarView)bindable;
    avatarView.Stroke = (Color)newValue;
}

// Bad Practice
static void OnBorderColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
{
    // Empty callback
}
```

### Constructor Initialization
- **Good Practice**: Initialize properties and set up bindings in the constructor.
- **Bad Practice**: Leaving the constructor empty or not initializing necessary properties.

```csharp
// Good Practice
public AvatarView()
{
    PropertyChanged += HandlePropertyChanged;

    IsEnabled = true;
    HorizontalOptions = VerticalOptions = LayoutOptions.Center;
    HeightRequest = AvatarViewDefaults.DefaultHeightRequest;
    WidthRequest = AvatarViewDefaults.DefaultWidthRequest;
    Padding = AvatarViewDefaults.DefaultPadding;
    Stroke = AvatarViewDefaults.DefaultBorderColor;
    StrokeThickness = AvatarViewDefaults.DefaultBorderWidth;
    StrokeShape = new RoundRectangle
    {
        CornerRadius = new CornerRadius(AvatarViewDefaults.DefaultCornerRadius.TopLeft, AvatarViewDefaults.DefaultCornerRadius.TopRight, AvatarViewDefaults.DefaultCornerRadius.BottomLeft, AvatarViewDefaults.DefaultCornerRadius.BottomRight),
    };
    Content = avatarLabel;
    avatarImage.SetBinding(WidthRequestProperty, BindingBase.Create<VisualElement, double>(static p => p.WidthRequest, source: this));
    avatarImage.SetBinding(HeightRequestProperty, BindingBase.Create<VisualElement, double>(static p => p.HeightRequest, source: this));
}

// Bad Practice
public AvatarView()
{
    // Empty constructor
}
```

### Event Handling
- **Good Practice**: Implement event handlers for property changes and other events.
- **Bad Practice**: Not handling events or leaving event handlers empty.

```csharp
// Good Practice
void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
{
    if ((e.PropertyName == HeightProperty.PropertyName
            || e.PropertyName == WidthProperty.PropertyName
            || e.PropertyName == PaddingProperty.PropertyName
            || e.PropertyName == ImageSourceProperty.PropertyName
            || e.PropertyName == BorderWidthProperty.PropertyName
            || e.PropertyName == CornerRadiusProperty.PropertyName
            || e.PropertyName == StrokeThicknessProperty.PropertyName)
        && Height >= 0
        && Width >= 0
        && avatarImage.Source is not null)
    {
        // Handle property changes
    }
}

// Bad Practice
void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
{
    // Empty handler
}
```

### Interface Implementations
- **Good Practice**: Implement interfaces and their members properly.
- **Bad Practice**: Not implementing required interface members or leaving them unimplemented.

```csharp
// Good Practice
Aspect IImageElement.Aspect => avatarImage.Aspect;
bool IImageElement.IsLoading => avatarImage.IsLoading;

// Bad Practice
Aspect IImageElement.Aspect => throw new NotImplementedException();
bool IImageElement.IsLoading => throw new NotImplementedException();
```

### Debug Logging
- **Good Practice**: Use `Trace.WriteLine()` for debug logging.
- **Bad Practice**: Using `Debug.WriteLine()` which is removed by the compiler in Release builds.

```csharp
// Good Practice
Trace.WriteLine("Debug message");

// Bad Practice
Debug.WriteLine("Debug message");
```

### Methods Returning Task and ValueTask
- **Good Practice**: Include a `CancellationToken` as a parameter for methods returning `Task` or `ValueTask`.
- **Bad Practice**: Not including a `CancellationToken`.

```csharp
// Good Practice
public async Task LoadDataAsync(CancellationToken token = default)
{
    token.ThrowIfCancellationRequested();
    // Method implementation
}

// Bad Practice
public async Task LoadDataAsync()
{
    // Method implementation
}
```

### Pattern Matching
- **Good Practice**: Use `is` for null checking and type checking.
- **Bad Practice**: Using `==` for null checking or casting for type checking.

```csharp
// Good Practice
if (something is null)
{
    // Handle null
}

if (something is Bucket bucket)
{
    bucket.Empty();
}

// Bad Practice
if (something == null)
{
    // Handle null
}

var bucket = something as Bucket;
if (bucket != null)
{
    bucket.Empty();
}
```

### Dependency Injection
- **Good Practice**: Use dependency injection to manage dependencies and improve testability.
- **Bad Practice**: Creating instances of dependencies directly within the class.

```csharp
// Good Practice
public class AvatarView
{
    private readonly IImageService _imageService;

    public AvatarView(IImageService imageService)
    {
        _imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
    }
}

// Bad Practice
public class AvatarView
{
    private readonly IImageService _imageService = new ImageService();
}
```

### Async/Await Usage
- **Good Practice**: Use `async` and `await` for asynchronous operations and include a `CancellationToken` parameter.
- **Bad Practice**: Using synchronous methods for operations that can be asynchronous or not including a `CancellationToken`.

```csharp
// Good Practice
public async Task LoadDataAsync(CancellationToken token = default)
{
    token.ThrowIfCancellationRequested();
    // Asynchronous operation
}

// Bad Practice
public void LoadData()
{
    // Synchronous operation
}
```

### Exception Handling
- **Good Practice**: Use specific exception types and provide meaningful messages.
- **Bad Practice**: Catching general exceptions or not providing meaningful messages.

```csharp
// Good Practice
try
{
    // Code that may throw an exception
}
catch (ArgumentNullException ex)
{
    Trace.WriteLine($"ArgumentNullException: {ex.Message}");
}

// Bad Practice
try
{
    // Code that may throw an exception
}
catch (Exception ex)
{
    Trace.WriteLine($"Exception: {ex.Message}");
}
```

## XML Documentation
- **Good Practice**: Provide XML documentation for public members.
- **Bad Practice**: Not providing documentation or leaving it empty.

```csharp
// Good Practice
/// <summary>Gets or sets the border color.</summary>
public Color BorderColor
{
    get => (Color)GetValue(BorderColorProperty);
    set => SetValue(BorderColorProperty, value);
}

// Bad Practice
public Color BorderColor
{
    get => (Color)GetValue(BorderColorProperty);
    set => SetValue(BorderColorProperty, value);
}
```

## Naming Conventions
- **Good Practice**: Follow consistent naming conventions for methods, properties, and variables.
- **Bad Practice**: Using inconsistent or unclear names.

```csharp
// Good Practice
public void LoadUserData()
{
    // Method implementation
}

// Bad Practice
public void load_user_data()
{
    // Method implementation
}
```