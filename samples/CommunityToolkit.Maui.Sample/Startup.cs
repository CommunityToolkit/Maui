using Microsoft.Maui;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Controls.Xaml;

[assembly: XamlCompilationAttribute(XamlCompilationOptions.Compile)]

namespace CommunityToolkit.Maui.Sample
{
    public static class Startup
    {
        public static MauiApp Create()
        {
            var builder = MauiApp.CreateBuilder();
            builder.UseMauiApp<App>();

            return builder.Build();
        }
    }
}