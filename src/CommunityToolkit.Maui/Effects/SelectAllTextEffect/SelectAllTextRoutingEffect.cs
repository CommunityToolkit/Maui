using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Effects
{
    public class SelectAllTextRoutingEffect : RoutingEffect
    {
        public SelectAllTextRoutingEffect()
        {
            #region Required work-around to prevent linker from removing the platform-specific implementation
            if (DateTime.Now.Ticks < 0)
                _ = new SelectAllTextPlatformEffect();
            #endregion
        }
    }
}