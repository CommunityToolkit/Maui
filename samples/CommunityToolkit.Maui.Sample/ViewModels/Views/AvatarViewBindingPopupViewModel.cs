namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Sample.Models;

/// <summary>AvatarView binding popup view model.</summary>
public sealed class AvatarViewBindingPopupViewModel
{
	public ObservableCollection<AvatarModel> AvatarList { get; } = new()
	{
		new AvatarModel
		{
			Text = "SW", // Sam Worthington
			TextColor = Colors.Blue,
			BackgroundColor = Colors.Yellow,
			ImageSource = "https://clipground.com/images/avatar-movie-clipart.png", // Using URL image
			BorderWidth = 4,
			Padding = 5,
			WidthRequest = 64,
			HeightRequest = 64,
			FontSize = 18,
			CornerRadius = new CornerRadius(32),
			BorderColor = Colors.Red,
			FontAttributes = FontAttributes.Bold | FontAttributes.Italic,
		},

		new AvatarModel
		{
			Text = "ZS", // Zoe Saldana
			TextColor = Colors.Yellow,
			BackgroundColor = Colors.Blue,
			ImageSource = "avatar_icon.png", // Using local MauiImage
			BorderWidth = 2,
			Padding = 10,
			WidthRequest = 64,
			HeightRequest = 64,
			FontSize = 16,
			CornerRadius = new CornerRadius(32),
			BorderColor = Colors.Yellow,
			FontAttributes = FontAttributes.Bold,
		},

		new AvatarModel
		{
			Text = "SW", // Sigourney Weaver
			TextColor = Colors.Yellow,
			BackgroundColor = Colors.Green,
			BorderWidth = 3,
			Padding = 5,
			WidthRequest = 48,
			HeightRequest = 64,
			FontSize = 14,
			CornerRadius = new CornerRadius(16, 16, 0, 0),
			BorderColor = Colors.Pink,
			FontAttributes = FontAttributes.Bold,
		},

		new AvatarModel
		{
			Text = "SL", // Stephen Lang
			TextColor = Colors.White,
			BackgroundColor = Colors.Black,
			BorderWidth = Thickness.Zero,
			Padding = Thickness.Zero,
			WidthRequest = 64,
			HeightRequest = 48,
			FontSize = 14,
			CornerRadius = new CornerRadius(0, 0, 16, 16),
			BorderColor = Colors.Red,
			FontAttributes = FontAttributes.Italic,
		},

		new AvatarModel
		{
			Text = "Michelle Rodriguez", // Michelle Rodriguez
			TextColor = Colors.Yellow,
			BackgroundColor = Colors.Gray,
			BorderWidth = 1,
			Padding = 1,
			FontSize = 14,
			CornerRadius = new CornerRadius(16, 0, 16, 0),
			BorderColor = Colors.Orange,
			FontAttributes = FontAttributes.Italic,
			WidthRequest = 128,
			HeightRequest = 72,
		},
	};
}