using CommunityToolkit.Maui.Effects;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;

namespace CommunityToolkit.Maui.Hosting
{
    public static class AppHostBuilderExtensions
    {
        public static IAppHostBuilder ConfigureCommunityToolkit(this IAppHostBuilder builder)
        {
            builder.ConfigureEffects(builder =>
            {
                builder.Add<RemoveBorderRoutingEffect, RemoveBorderPlatformEffect>();
                builder.Add<SelectAllTextRoutingEffect, SelectAllTextPlatformEffect>();
            });

            return builder;
        }
    }
}