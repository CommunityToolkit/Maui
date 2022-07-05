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
				AvatarBackgroundColor = Colors.Yellow,
				ImageSource = "https://clipground.com/images/avatar-movie-clipart.png", // Using URL image
				BorderWidth = 4,
				AvatarPadding = 5,
				AvatarWidthRequest = 64,
				AvatarHeightRequest = 64,
				FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
				CornerRadius = new CornerRadius(32),
				BorderColor = Colors.Red,
				FontAttributes = FontAttributes.Bold | FontAttributes.Italic,
			},

			new AvatarModel
			{
				Text = "ZS", // Zoe Saldana
				TextColor = Colors.Yellow,
				AvatarBackgroundColor = Colors.Blue,
				ImageSource = "avatar_icon.png", // Using local MauiImage
				BorderWidth = 2,
				AvatarPadding = 10,
				AvatarWidthRequest = 64,
				AvatarHeightRequest = 64,
				FontSize = Device.GetNamedSize(NamedSize.Default, typeof(Label)),
				CornerRadius = new CornerRadius(32),
				BorderColor = Colors.Yellow,
				FontAttributes = FontAttributes.Bold,
			},

			new AvatarModel
			{
				Text = "SW", // Sigourney Weaver
				TextColor = Colors.Yellow,
				AvatarBackgroundColor = Colors.Green,
				BorderWidth = 3,
				AvatarPadding = 5,
				AvatarWidthRequest = 48,
				AvatarHeightRequest = 64,
				FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
				CornerRadius = new CornerRadius(16,16,0,0),
				BorderColor = Colors.Pink,
				FontAttributes = FontAttributes.Bold,
			},

			new AvatarModel
			{
				Text = "SL", // Stephen Lang
				TextColor = Colors.White,
				AvatarBackgroundColor = Colors.Black,
				BorderWidth = Thickness.Zero,
				AvatarPadding = Thickness.Zero,
				AvatarWidthRequest = 64,
				AvatarHeightRequest = 48,
				FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
				CornerRadius = new CornerRadius(0,0,16,16),
				BorderColor = Colors.Red,
				FontAttributes = FontAttributes.Italic,
			},

			new AvatarModel
			{
				Text = "Michelle Rodriguez", // Michelle Rodriguez
				TextColor = Colors.Yellow,
				AvatarBackgroundColor = Colors.Gray,
				BorderWidth = 1,
				AvatarPadding = 7,
				FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
				CornerRadius = new CornerRadius(0,0,16,16),
				BorderColor = Colors.Orange,
				FontAttributes = FontAttributes.Italic,
				AvatarWidthRequest = 128,
				AvatarHeightRequest = 72,
			},
		};
	}

	public static List<AvatarModel> Avatars { get; private set; }
}