using System.Collections.ObjectModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

/// <summary>Sample view model for Avatar View page.</summary>
public partial class AvatarViewViewModel : BaseViewModel
{
#pragma warning disable CS0612 // Type or member is obsolete, this is however still used in Microsoft.Maui

	public ObservableCollection<Models.AvatarModel> AvatarList { get; } = new()
		{
			new Models.AvatarModel
			{
				Text = "SW", // Sam Worthington
				TextColor = Colors.Blue,
				BackgroundColor = Colors.Yellow,
				ImageSource = "https://clipground.com/images/avatar-movie-clipart.png", // Using URL image
				BorderWidth = 4,
				Padding = 5,
				WidthRequest = 64,
				HeightRequest = 64,
				FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
				CornerRadius = new CornerRadius(32),
				BorderColor = Colors.Red,
				FontAttributes = FontAttributes.Bold | FontAttributes.Italic,
			},

			new Models.AvatarModel
			{
				Text = "ZS", // Zoe Saldana
				TextColor = Colors.Yellow,
				BackgroundColor = Colors.Blue,
				ImageSource = "avatar_icon.png", // Using local MauiImage
				BorderWidth = 2,
				Padding = 10,
				WidthRequest = 64,
				HeightRequest = 64,
				FontSize = Device.GetNamedSize(NamedSize.Default, typeof(Label)),
				CornerRadius = new CornerRadius(32),
				BorderColor = Colors.Yellow,
				FontAttributes = FontAttributes.Bold,
			},

			new Models.AvatarModel
			{
				Text = "SW", // Sigourney Weaver
				TextColor = Colors.Yellow,
				BackgroundColor = Colors.Green,
				BorderWidth = 3,
				Padding = 5,
				WidthRequest = 48,
				HeightRequest = 64,
				FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
				CornerRadius = new CornerRadius(16, 16, 0, 0),
				BorderColor = Colors.Pink,
				FontAttributes = FontAttributes.Bold,
			},

			new Models.AvatarModel
			{
				Text = "SL", // Stephen Lang
				TextColor = Colors.White,
				BackgroundColor = Colors.Black,
				BorderWidth = Thickness.Zero,
				Padding = Thickness.Zero,
				WidthRequest = 64,
				HeightRequest = 48,
				FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
				CornerRadius = new CornerRadius(0, 0, 16, 16),
				BorderColor = Colors.Red,
				FontAttributes = FontAttributes.Italic,
			},

			new Models.AvatarModel
			{
				Text = "Michelle Rodriguez", // Michelle Rodriguez
				TextColor = Colors.Yellow,
				BackgroundColor = Colors.Gray,
				BorderWidth = 1,
				Padding = 1,
				FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
				CornerRadius = new CornerRadius(16, 0, 16, 0),
				BorderColor = Colors.Orange,
				FontAttributes = FontAttributes.Italic,
				WidthRequest = 128,
				HeightRequest = 72,
			},
		};

#pragma warning restore CS0612 // Type or member is obsolete, this is however still used in Microsoft.Maui
}