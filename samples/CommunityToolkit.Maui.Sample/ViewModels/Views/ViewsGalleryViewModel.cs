using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.Pages.Views;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views
{
    public class ViewsGalleryViewModel : BaseGalleryViewModel
    {
        protected override IEnumerable<SectionModel> CreateItems() => new[]
        {
           new SectionModel(typeof(DockLayoutPage), "DockLayout",
               "Makes it easy to dock content in all four directions (top, bottom, left and right)"),

           new SectionModel(typeof(HexLayoutPage), "HexLayout",
                "A Layout that arranges the elements in a honeycomb pattern")
        };
    }
}