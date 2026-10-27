using Android.App;
using Android.Views;
using Microsoft.Maui.Platform;
using AndroidWindow = Android.Views.Window;

namespace CommunityToolkit.Maui.Views;

public partial class Popup
{
    /// <summary>
    /// Stores the native window and soft input mode that were active before the popup was opened.
    /// </summary>
    AndroidWindow? popupWindow;

    /// <summary>
    /// Stores the previous Android soft input mode before the popup changes it, so it can be restored afterward.
    /// </summary>
    SoftInput? previousSoftInputMode;

    partial void OnPlatformPopupOpened()
    {
        if (Handler?.PlatformView is not Android.Views.View platformView || platformView.Context?.GetActivity() is not Activity activity)
        {
            return;
        }

        var window = activity.Window;

        if (window is null)
        {
            return;
        }

        if (window.Attributes?.SoftInputMode is not { } softInputMode)
        {
            return;
        }

        popupWindow = window;
        previousSoftInputMode ??= softInputMode;
        window.SetSoftInputMode(SoftInput.AdjustResize);
    }

    partial void OnPlatformPopupClosed()
    {
        if (popupWindow is not AndroidWindow window || previousSoftInputMode is not { } softInputMode)
        {
            return;
        }

        window.SetSoftInputMode(softInputMode);
        popupWindow = null;
        previousSoftInputMode = null;
    }
}