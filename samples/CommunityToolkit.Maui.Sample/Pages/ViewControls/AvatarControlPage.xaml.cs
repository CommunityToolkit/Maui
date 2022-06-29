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
		ContainerControl.Children.Add(new AvatarLayoutView
		{
			Text = "CB",  // CB = Code Behind
			TextColor = Colors.Yellow,
			WidthRequest = 50,
			HeightRequest = 50,
			AvatarWidthRequest = 50,
			AvatarHeightRequest = 50,
			AvatarBackgroundColor = Colors.Blue,
			AvatarCornerRadius = 25 // A corner radius of half the element radius results in a circle
		});
	}

	public void OnAvatarClicked(object? sender, EventArgs e)
	{
		Debug.WriteLine("Clicked");
	}
}