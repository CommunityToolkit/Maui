using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;
public class ExpanderPageCS : ContentPage
{
	public ExpanderPageCS()
	{
		var content = new VerticalStackLayout()
		{
			Padding = new Thickness(10)
		};

		content.Children.Add(new Image
		{
			Source = "https://avatars.githubusercontent.com/u/9011267?v=4",
			Aspect = Aspect.AspectFit,
			HeightRequest = 120,
			WidthRequest = 120
		});

		content.Children.Add(new Label
		{
			Text = ".NET Multi-platform App UI (.NET MAUI) is a cross-platform framework for creating mobile and desktop apps with C# and XAML. Using .NET MAUI, you can develop apps that can run on Android, iOS, iPadOS, macOS, and Windows from a single shared codebase.",
			FontAttributes = FontAttributes.Italic
		});

		var expander = new Expander
		{
			HorizontalOptions = LayoutOptions.Center,
			Header = new Label
			{
				Text = ".NET MAUI",
				FontAttributes = FontAttributes.Bold
			},
			Direction = ExpandDirection.Down,
			Content = content
		};
		var expandDirectionPicker = new Picker()
		{
			SelectedIndex = 0,
			ItemsSource = Enum.GetValues<ExpandDirection>(),
			HorizontalOptions = LayoutOptions.Center
		};
		expandDirectionPicker.SelectedIndexChanged += (_, _) =>
		{
			expander.Direction = Enum.Parse<ExpandDirection>(expandDirectionPicker.SelectedIndex.ToString());
		};
		Content = new VerticalStackLayout()
		{
			Children =
			{
				new Label(){ Text = "Expander C# Sample", HorizontalOptions = LayoutOptions.Center},
				expandDirectionPicker,
				expander
			}
		};
	}
}