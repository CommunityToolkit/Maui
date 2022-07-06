using System.Diagnostics;
using CommunityToolkit.Maui.Sample.ViewModels.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class AvatarViewPage : BasePage<AvatarViewViewModel>
{
	public AvatarViewPage(AvatarViewViewModel avatarControlViewModel) : base(avatarControlViewModel)
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