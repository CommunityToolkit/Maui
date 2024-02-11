using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public partial class IconTintColorBehaviorViewModel : BaseViewModel
{
	const string dotnetBotImageFileName = "dotnet_bot.png";
	const string shieldImageFileName = "shield.png";

	static readonly IEnumerator<Color?> toggleableColorsEnumerator = new List<Color?> { Colors.Red, Colors.Green, null }.GetEnumerator();

	[ObservableProperty]
	string toggleableImageSource = shieldImageFileName;

	[ObservableProperty]
	Color? toggleableIconTintColor = toggleableColorsEnumerator.Current;

	[RelayCommand]
	void ToggleImageButton()
	{
		ToggleableImageSource = ToggleableImageSource switch
		{
			dotnetBotImageFileName => shieldImageFileName,
			shieldImageFileName => dotnetBotImageFileName,
			_ => throw new NotSupportedException("Invalid image source")
		};
	}

	[RelayCommand]
	void ChangeColor()
	{
		if (!toggleableColorsEnumerator.MoveNext())
		{
			toggleableColorsEnumerator.Reset();
			toggleableColorsEnumerator.MoveNext();
		}

		ToggleableIconTintColor = toggleableColorsEnumerator.Current;
	}
}