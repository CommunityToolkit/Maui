using Foundation;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace CommunityToolkit.Maui.Sample
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}