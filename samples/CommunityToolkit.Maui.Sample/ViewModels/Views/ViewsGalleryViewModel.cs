using System.Collections.Generic;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.Pages.Views;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public class ViewsGalleryViewModel : BaseGalleryViewModel
{
	protected override IEnumerable<SectionModel> CreateItems() => new[]
	{
		new SectionModel(typeof(SnackBarPage), "SnackBar/Toast", "Show SnackBar, Toast etc")
	};
}