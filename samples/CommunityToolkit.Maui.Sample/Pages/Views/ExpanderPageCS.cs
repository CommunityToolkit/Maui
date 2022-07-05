using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;
public class ExpanderPageCS : ContentPage
{
	public ExpanderPageCS()
	{
		var expander = new Expander
		{
			HorizontalOptions = LayoutOptions.Center,
			Header = new Label
			{
				Text = "Baboon",
				FontAttributes = FontAttributes.Bold
			}
		};

		var content = new HorizontalStackLayout()
		{
			Padding = new Thickness(10)
		};

		content.Children.Add(new Image
		{
			Source = "http://upload.wikimedia.org/wikipedia/commons/thumb/f/fc/Papio_anubis_%28Serengeti%2C_2009%29.jpg/200px-Papio_anubis_%28Serengeti%2C_2009%29.jpg",
			Aspect = Aspect.AspectFill,
			HeightRequest = 120,
			WidthRequest = 120
		});

		content.Children.Add(new Label
		{
			Text = "Baboons are African and Arabian Old World monkeys belonging to the genus Papio, part of the subfamily Cercopithecinae.",
			FontAttributes = FontAttributes.Italic
		});

		expander.Content = content;

		Content = expander;
	}
}