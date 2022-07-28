namespace CommunityToolkit.Maui.Sample.Pages.Views;

using CommunityToolkit.Maui.Sample.ViewModels.Views.AvatarView;

/// <summary>AvatarView gravatar page.</summary>
public partial class AvatarViewGravatarPage : BasePage<AvatarViewGravatarViewModel>
{
	public AvatarViewGravatarPage(AvatarViewGravatarViewModel avatarViewGravatarViewModel) : base(avatarViewGravatarViewModel)
	{
		InitializeComponent();
	}

	/// <summary>Tap gesture recognizer tapped.</summary>
	/// <param name="sender">Sender object.</param>
	/// <param name="e">Event arguments.</param>
	async void OnTapGestureRecognizerTapped(object sender, EventArgs e)
	{
		Span spanClicked = (Span)sender;
		TapGestureRecognizer item = (TapGestureRecognizer)spanClicked.GestureRecognizers[0];
		string urlAddress = (string)item.CommandParameter;
		await Launcher.OpenAsync(urlAddress);
	}

}