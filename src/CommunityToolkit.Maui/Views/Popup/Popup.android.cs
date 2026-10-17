using Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;

namespace CommunityToolkit.Maui.Views;

public partial class Popup
{
    /// <summary>
    /// Stores the soft input mode that was active before the popup was opened so it can be restored when the popup closes.
    /// </summary>
    WindowSoftInputModeAdjust? previousSoftInputMode;

    partial void OnPlatformPopupOpened()
    {
        var android = Microsoft.Maui.Controls.Application.Current?.On<Microsoft.Maui.Controls.PlatformConfiguration.Android>();

        if (android is null)
        {
            return;
        }

        previousSoftInputMode = android.GetWindowSoftInputModeAdjust();

        android.UseWindowSoftInputModeAdjust(WindowSoftInputModeAdjust.Resize);
    }

    partial void OnPlatformPopupClosed()
    {
        var android = Microsoft.Maui.Controls.Application.Current?.On<Microsoft.Maui.Controls.PlatformConfiguration.Android>();

        if (android is null)
        {
            return;
        }

        android.UseWindowSoftInputModeAdjust(previousSoftInputMode ?? WindowSoftInputModeAdjust.Pan);
    }
}