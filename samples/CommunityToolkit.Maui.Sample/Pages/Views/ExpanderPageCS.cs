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
		var expander2 = new Expander2
		{
			HorizontalOptions = LayoutOptions.Center,
			Header = new Label
			{
				Text = "Baboon (shared)",
				FontAttributes = FontAttributes.Bold
			}
		};

		var content = new VerticalStackLayout()
		{
			Padding = new Thickness(10)
		};

		content.Children.Add(new Image
		{
			Source = "http://upload.wikimedia.org/wikipedia/commons/thumb/f/fc/Papio_anubis_%28Serengeti%2C_2009%29.jpg/200px-Papio_anubis_%28Serengeti%2C_2009%29.jpg",
			Aspect = Aspect.AspectFit,
			HeightRequest = 120,
			WidthRequest = 120
		});

		content.Children.Add(new Label
		{
			Text = "Baboons are African and Arabian Old World monkeys belonging to the genus Papio, part of the subfamily Cercopithecinae.",
			FontAttributes = FontAttributes.Italic
		});
		var content2 = new VerticalStackLayout()
		{
			Padding = new Thickness(10)
		};

		content2.Children.Add(new Image
		{
			Source = "http://upload.wikimedia.org/wikipedia/commons/thumb/f/fc/Papio_anubis_%28Serengeti%2C_2009%29.jpg/200px-Papio_anubis_%28Serengeti%2C_2009%29.jpg",
			Aspect = Aspect.AspectFit,
			HeightRequest = 120,
			WidthRequest = 120
		});

		content2.Children.Add(new Label
		{
			Text = "Baboons are African and Arabian Old World monkeys belonging to the genus Papio, part of the subfamily Cercopithecinae.",
			FontAttributes = FontAttributes.Italic
		});

		expander.Content = content;
		expander2.Content = content2;

		Content = new VerticalStackLayout()
		{
			Children =
			{
				new Label(){ Text = "Expander C# Sample", HorizontalOptions = LayoutOptions.Center},
				expander,
				expander2
			}
		};
	}
}