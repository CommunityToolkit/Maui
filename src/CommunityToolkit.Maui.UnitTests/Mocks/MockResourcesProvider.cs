using Microsoft.Maui.Controls.Internals;
namespace CommunityToolkit.Maui.UnitTests.Mocks;

// Inspired by https://github.com/dotnet/maui/blob/79695fbb7ba6517a334c795ecf0a1d6358ef309a/src/Controls/Foldable/test/MockPlatformServices.cs#L145-L176

#pragma warning disable CS0612 // Type or member is obsolete
class MockResourcesProvider : ISystemResourcesProvider
#pragma warning restore CS0612 // Type or member is obsolete
{
	public IResourceDictionary GetSystemResources()
	{
		var dictionary = new ResourceDictionary();

		Style style = new Style(typeof(Label));
		dictionary[Device.Styles.BodyStyleKey] = style;

		style = new Style(typeof(Label));
		style.Setters.Add(Label.FontSizeProperty, 50);
		dictionary[Device.Styles.TitleStyleKey] = style;

		style = new Style(typeof(Label));
		style.Setters.Add(Label.FontSizeProperty, 40);
		dictionary[Device.Styles.SubtitleStyleKey] = style;

		style = new Style(typeof(Label));
		style.Setters.Add(Label.FontSizeProperty, 30);
		dictionary[Device.Styles.CaptionStyleKey] = style;

		style = new Style(typeof(Label));
		style.Setters.Add(Label.FontSizeProperty, 20);
		dictionary[Device.Styles.ListItemTextStyleKey] = style;

		style = new Style(typeof(Label));
		style.Setters.Add(Label.FontSizeProperty, 10);
		dictionary[Device.Styles.ListItemDetailTextStyleKey] = style;

		return dictionary;
	}
}