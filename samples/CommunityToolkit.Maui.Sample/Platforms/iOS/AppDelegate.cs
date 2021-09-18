using Foundation;
using Microsoft.Maui;

namespace CommunityToolkit.Maui.Sample
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => Startup.Create();
    }
}