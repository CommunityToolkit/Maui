using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class IconTintColorBehaviorPage : BasePage<IconTintColorBehaviorViewModel>
{
	public IconTintColorBehaviorPage(IconTintColorBehaviorViewModel iconTintColorBehaviorViewModel)
		: base(iconTintColorBehaviorViewModel)
	{
		InitializeComponent();
	}

	void ChangeSource_Clicked(System.Object sender, System.EventArgs e)
	{
		var imageSource = (ImageButton)sender;
		var selectedImage = imageSource.Source as FileImageSource;

		if (selectedImage is not null)
		{
			if (selectedImage.File == "dotnet_bot.png")
			{
				UpdateImage.Source = "shield.png";
			}
			else
			{
				UpdateImage.Source = "dotnet_bot.png";
			}
		}
	}
}