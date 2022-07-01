namespace CommunityToolkit.Maui.Sample.ViewModels.ViewControls;
public static class AvatarData
{
	static AvatarData()
	{
		Avatars = new List<AvatarModel>
		{
			new AvatarModel
			{
				Text = "SW", // Sam Worthington
				TextColor = Colors.Blue,
				BackgroundColor = Colors.Yellow,
				ImageSource = "https://clipground.com/images/avatar-movie-clipart.png", // Using URL image
			},

			new AvatarModel
			{
				Text = "ZS", // Zoe Saldana
				TextColor = Colors.Yellow,
				BackgroundColor = Colors.Blue,
				ImageSource = "avatar_icon.png", // Using local MauiImage
			},

			new AvatarModel
			{
				Text = "SW", // Sigourney Weaver
				TextColor = Colors.Yellow,
				BackgroundColor = Colors.Green,
			},

			new AvatarModel
			{
				Text = "SL", // Stephen Lang
				TextColor = Colors.White,
				BackgroundColor = Colors.Black,
			},
		};
	}

	public static List<AvatarModel> Avatars { get; private set; }
}
