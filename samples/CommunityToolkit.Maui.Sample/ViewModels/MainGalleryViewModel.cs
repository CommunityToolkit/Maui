using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.Pages.Alerts;
using CommunityToolkit.Maui.Sample.Pages.Behaviors;
using CommunityToolkit.Maui.Sample.Pages.Converters;
using CommunityToolkit.Maui.Sample.Pages.Extensions;

namespace CommunityToolkit.Maui.Sample.ViewModels;

public class MainGalleryViewModel : BaseGalleryViewModel
{
	public MainGalleryViewModel(AlertsGalleryPage alertsGalleryPage,
								BehaviorsGalleryPage behaviorsGalleryPage,
								ConvertersGalleryPage convertersGalleryPage,
								ExtensionsGalleryPage extensionsGalleryPage)
		: base(new[]
		{
			 new SectionModel(alertsGalleryPage, "Alerts", Color.FromArgb("#EF6950"),
				 "Alerts allow you display alerts to your user"),

			 new SectionModel(behaviorsGalleryPage, "Behaviors", Color.FromArgb("#8E8CD8"),
				 "Behaviors lets you add functionality to user interface controls without having to subclass them. Behaviors are written in code and added to controls in XAML or code"),

			 new SectionModel(convertersGalleryPage, "Converters", Color.FromArgb("#EA005E"),
				 "Converters let you convert bindings of a certain type to a different value, based on custom logic"),

			 new SectionModel(extensionsGalleryPage, "Extensions", Color.FromArgb("#00EA56"),
				 "Extenions lets you add methods to existing types without creating a new derived type, recompiling, or otherwise modifying the original type."),
		})
	{
	}
}