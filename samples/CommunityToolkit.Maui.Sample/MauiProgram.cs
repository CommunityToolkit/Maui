using Microsoft.Maui;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Controls.Xaml;

[assembly: XamlCompilationAttribute(XamlCompilationOptions.Compile)]

namespace CommunityToolkit.Maui.Sample
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder.UseMauiApp<App>();

            return builder.Build();
        }
    }
}