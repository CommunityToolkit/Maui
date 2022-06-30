using System.Diagnostics;
using CommunityToolkit.Maui.Controls;
using CommunityToolkit.Maui.Sample.ViewModels.ViewControls;

namespace CommunityToolkit.Maui.Sample.Pages.ViewControls;

public partial class AvatarControlPage : BasePage<AvatarControlViewModel>
{
	public AvatarControlPage(AvatarControlViewModel avatarControlViewModel) : base(avatarControlViewModel)
	{
		InitializeComponent();

		// Example of how to add using code (XAML free)
		ContainerControl.Children.Add(new Label
		{
			Text = "Added using code (XAML free)"
		});
		ContainerControl.Children.Add(new Avatar
		{
			Text = "CB",  // CB = Code Behind
			TextColor = Colors.Yellow,
			WidthRequest = 50,
			HeightRequest = 50,
			Margin = 0,
			Padding = 0,
			BackgroundColor = Colors.Blue,
			FontAttributes = FontAttributes.Bold | FontAttributes.Italic,
		});
	}

	public void OnAvatarClicked(object? sender, EventArgs e)
	{
		Debug.WriteLine("Clicked");
	}

	public void TapGestureRecognizer_Tapped(object sender, EventArgs e)
	{
		if (e is TappedEventArgs gesture)
		{
			if (gesture.Parameter is string parameter)
			{
				Debug.WriteLine($"Clicked with parameter: {parameter}");
				return;
			}

			Debug.WriteLine("Clicked with parameter");
			return;
		}

		Debug.WriteLine("Clicked");
	}
}