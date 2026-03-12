using Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;

namespace CommunityToolkit.Maui.Views;

public partial class Popup
{
    partial void OnPlatformPopupOpened()
    {
        Microsoft.Maui.Controls.Application.Current?.On<Microsoft.Maui.Controls.PlatformConfiguration.Android>().UseWindowSoftInputModeAdjust(WindowSoftInputModeAdjust.Resize);
    }

    partial void OnPlatformPopupClosed()
    {
        Microsoft.Maui.Controls.Application.Current?.On<Microsoft.Maui.Controls.PlatformConfiguration.Android>().UseWindowSoftInputModeAdjust(WindowSoftInputModeAdjust.Unspecified);
    }
}