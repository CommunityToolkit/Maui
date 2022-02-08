using CommunityToolkit.Maui.Markup;

namespace CommunityToolkit.Maui.Sample.Pages;

public class WelcomePage : BasePage
{
	public WelcomePage()
	{
		Content = new VerticalStackLayout
		{
			Spacing = 12,

			Children =
			{
				new Label { Text = "Welcome to the .NET MAUI Community Toolkit" }
					.Font(size: 32).TextCenter(),

				new Label { Text = "Explore features using the flyout menu in the top left" }
					.Font(size: 16).TextCenter()
			}
		}.CenterHorizontal();
	}
}


