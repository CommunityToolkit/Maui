using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.ViewModels.Alerts;
using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;
using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.ViewModels;

public class MainGalleryViewModel : BaseGalleryViewModel
{
	public MainGalleryViewModel()
		: base(new[]
		{
			 SectionModel.Create<AlertsGalleryViewModel>("Alerts", Color.FromArgb("#EF6950"),
				 "Alerts allow you display alerts to your user"),

			 SectionModel.Create<BehaviorsGalleryViewModel>("Behaviors", Color.FromArgb("#8E8CD8"),
				 "Behaviors lets you add functionality to user interface controls without having to subclass them. Behaviors are written in code and added to controls in XAML or code"),

			 SectionModel.Create<ConvertersGalleryViewModel>("Converters", Color.FromArgb("#EA005E"),
				 "Converters let you convert bindings of a certain type to a different value, based on custom logic"),

			 SectionModel.Create<ExtensionsGalleryViewModel>("Extensions", Color.FromArgb("#00EA56"),
				 "Extensions lets you add methods to existing types without creating a new derived type, recompiling, or otherwise modifying the original type."),
		})
	{
	}
}