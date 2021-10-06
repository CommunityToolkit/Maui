using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.UI.Views.Options;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;

namespace CommunityToolkit.Maui.Sample.Pages.Views
{
	public partial class SnackBarPage : BasePage
	{
		public SnackBarPage() => InitializeComponent();

		async void DisplaySnackBarClicked(object? sender, EventArgs args)
		{
			var result = await this.DisplaySnackBarAsync(GenerateLongText(5), "Run action", () =>
			{
				Debug.WriteLine("SnackBar action button clicked");
				return Task.CompletedTask;
			});
			StatusText.Text = result ? "SnackBar is closed by user" : "SnackBar is closed by timeout";
		}

		async void DisplaySnackBarWithPadding(object? sender, EventArgs args)
		{
			var options = new SnackBarOptions()
			{
				BackgroundColor = Color.FromArgb("#CC0000"),
				MessageOptions = new MessageOptions
				{
					Message = "msg",
					Foreground = Colors.White,
					Font = Font.SystemFontOfSize(16),
					Padding = new Thickness(10, 20, 30, 40)
				}
			};

			await this.DisplaySnackBarAsync(options);
		}

		async void DisplayToastClicked(object? sender, EventArgs args)
		{
			await this.DisplayToastAsync(GenerateLongText(5));
			StatusText.Text = "Toast is closed by timeout";
		}

		async void DisplayToastAnchoredClicked(object? sender, EventArgs args)
		{
			var messageOptions = new MessageOptions
			{
				Message = "Anchored toast",
				Foreground = Colors.Black
			};

			var options = new ToastOptions
			{
				MessageOptions = messageOptions,
				Duration = TimeSpan.FromMilliseconds(5000),
				CornerRadius = new Thickness(10, 20, 30, 40),
				BackgroundColor = Colors.LightBlue
			};

			await Anchor1.DisplayToastAsync(options);
		}

		async void DisplaySnackbarAnchoredClicked(object? sender, EventArgs args)
		{
			var messageOptions = new MessageOptions
			{
				Message = GenerateLongText(5),
				Foreground = Colors.Black
			};

			var options = new SnackBarOptions
			{
				MessageOptions = messageOptions,
				Duration = TimeSpan.FromMilliseconds(5000),
				BackgroundColor = Colors.LightBlue,
				Actions = new List<SnackBarActionOptions>
				{
					new SnackBarActionOptions
					{
						ForegroundColor = Colors.Red,
						BackgroundColor = Colors.Green,
						Font = Font.OfSize("Times New Roman", 15),
						Padding = new Thickness(10, 20, 30, 40),
						Text = "Action1",
						Action = () =>
						{
							Debug.WriteLine("1");
							return Task.CompletedTask;
						}
					}
				}
			};

			var result = await Anchor2.DisplaySnackBarAsync(options);
			StatusText.Text = result ? "SnackBar is closed by user" : "SnackBar is closed by timeout";
		}

		async void DisplaySnackBarAdvancedClicked(object? sender, EventArgs args)
		{
			const string smileIcon = "\uf118";
			var options = new SnackBarOptions
			{
				MessageOptions = new MessageOptions
				{
					Foreground = Colors.DeepSkyBlue,
					Font = Font.OfSize("FARegular", 40),
					Padding = new Thickness(10, 20, 30, 40),
					Message = smileIcon
				},
				CornerRadius = new Thickness(10, 20, 30, 40),
				Duration = TimeSpan.FromMilliseconds(5000),
				BackgroundColor = Colors.Coral,
				IsRtl = CultureInfo.CurrentCulture.TextInfo.IsRightToLeft,
				Actions = new List<SnackBarActionOptions>
				{
					new SnackBarActionOptions
					{
						ForegroundColor = Colors.Red,
						BackgroundColor = Colors.Green,
						Font = Font.OfSize("Times New Roman", 15),
						Padding = new Thickness(10, 20, 30, 40),
						Text = "Action1",
						Action = () =>
						{
							Debug.WriteLine("1");
							return Task.CompletedTask;
						}
					},
					new SnackBarActionOptions
					{
						ForegroundColor = Colors.Green,
						BackgroundColor = Colors.Red,
						Font = Font.OfSize("Times New Roman", 20),
						Padding = new Thickness(40, 30, 20, 10),
						Text = "Action2",
						Action = () =>
						{
							Debug.WriteLine("2");
							return Task.CompletedTask;
						}
					}
				}
			};
			var result = await this.DisplaySnackBarAsync(options);
			StatusText.Text = result ? "SnackBar is closed by user" : "SnackBar is closed by timeout";
		}

		string GenerateLongText(int stringDuplicationTimes)
		{
			const string message = "It is a very long message to test multiple strings. A B C D E F G H I I J K L M N O P Q R S T U V W X Y Z";
			var result = new StringBuilder();
			for (var i = 0; i < stringDuplicationTimes; i++)
			{
				result.AppendLine(message);
			}

			return result.ToString();
		}
	}
}