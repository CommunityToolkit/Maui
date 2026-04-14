using CommunityToolkit.Maui.Core;

namespace DivStart;

public partial class Application : Microsoft.Maui.Controls.Application
{
    public Application()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
        => new(new AppShell());
    #region Android Status Bar Manipulation
    /// <summary>
    /// Sets the status bar color and appearance based on the current application theme.
    /// </summary>
    /// <remarks>This method determines whether the application is using a dark or light theme and updates the
    /// status bar accordingly. It is typically used to ensure the status bar matches the overall app appearance for
    /// better user experience.</remarks>
    public static void SetStatusBar()
    {
        bool isDark = Application.Current!.UserAppTheme == AppTheme.Dark || (Application.Current.UserAppTheme == AppTheme.Unspecified && Application.Current.RequestedTheme == AppTheme.Dark);
        SetStatusBar(isDark ? Colors.Black : Colors.White, darkIcons: !isDark);
    }
    /// <summary>
    /// Sets the status bar background color and icon style for the application.
    /// </summary>
    /// <remarks>This method is effective only on Android platforms. On other platforms, calling this method
    /// has no effect.</remarks>
    /// <param name="backgroundColor">The color to apply to the status bar background. Cannot be null.</param>
    /// <param name="darkIcons">A value indicating whether to use dark icons on the status bar. Set to <see langword="true"/> for dark icons;
    /// otherwise, <see langword="false"/> for light icons.</param>
    public static void SetStatusBar(Color backgroundColor, bool? darkIcons = null)
    {
	    if (backgroundColor == null)
	    {
            return;
		    
	    }
#if ANDROID
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CommunityToolkit.Maui.Core.Platform.StatusBar.SetColor(backgroundColor);
            if (darkIcons is not null)
            {
                //CommunityToolkit.Maui.Core.Platform.StatusBar.SetStyle(darkIcons.Value ? StatusBarStyle.DarkContent : StatusBarStyle.LightContent);
            }
        });
#endif
    }
    #endregion
}