using System.Collections.Generic;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.Pages.Views;
using CommunityToolkit.Maui.UI.Views;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views
{
    public class ViewsGalleryViewModel : BaseGalleryViewModel
    {
        protected override IEnumerable<SectionModel> CreateItems() => new[]
        {
            new SectionModel(
                typeof(BadgeViewPage),
                nameof(BadgeView),
                "View used to notify users notifications, or status of something"),
        };
    }
}