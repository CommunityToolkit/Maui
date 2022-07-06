using System.Diagnostics;
using CommunityToolkit.Maui.Sample.ViewModels.ViewControls;

namespace CommunityToolkit.Maui.Sample.Pages.ViewControls;

public partial class AvatarControlPage : BasePage<AvatarControlViewModel>
{
	public AvatarControlPage(AvatarControlViewModel avatarControlViewModel) : base(avatarControlViewModel)
	{
		InitializeComponent();
	}

	void OnAvatarClicked(object? sender, EventArgs e)
	{
		Debug.WriteLine("Clicked");
	}

	void TapGestureRecognizer_Tapped(object sender, EventArgs e)
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