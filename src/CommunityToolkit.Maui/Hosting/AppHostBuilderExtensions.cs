using CommunityToolkit.Maui.UI.Views;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;

namespace CommunityToolkit.Maui.Hosting
{
    public static class AppHostBuilderExtensions
    {
        public static IAppHostBuilder ConfigureCommunityToolkit(this IAppHostBuilder builder)
        {
            builder.ConfigureMauiHandlers(builder =>
            {
                builder.AddHandler<DockLayout, LayoutHandler>();
                builder.AddHandler<HexLayout, LayoutHandler>();
            });

            return builder;
        }
    }
}