namespace CommunityToolkit.Maui.Sample.Pages;

public class WelcomePage : BasePage
{
	public WelcomePage()
	{
		Title = "Welcome";

		Content = new VerticalStackLayout
		{
			Children =
			{
#if ANDROID
				new CommunityToolkit.Maui.Views.DrawingView()
				{
					WidthRequest = 300,
					HeightRequest = 300,
					BackgroundColor = Colors.Yellow,
					DefaultLineColor = Colors.Blue,
					DefaultLineWidth = 10,
				}
#endif
			}
		};
	}
}


