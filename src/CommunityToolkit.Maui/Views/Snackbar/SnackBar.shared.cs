#if !(__IOS__ || __MACCATALYST__ || __ANDROID__)
using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui.UI.Views.Options;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.UI.Views
{
    class SnackBar
    {
        internal ValueTask Show(VisualElement sender, SnackBarOptions arguments)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
#endif
