namespace CommunityToolkit.Maui;

/// <summary>
/// 
/// </summary>
public class AppThemeColor
{
	Color? light;
	Color? dark;
	Color? @default;
	bool isLightSet;
	bool isDarkSet;
	bool isDefaultSet;

	/// <summary>
	/// 
	/// </summary>
	public Color? Light
	{
		get => light;
		set
		{
			light = value;
			isLightSet = true;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public Color? Dark
	{
		get => dark;
		set
		{
			dark = value;
			isDarkSet = true;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public Color? Default
	{
		get => @default;
		set
		{
			@default = value;
			isDefaultSet = true;
		}
	}

	internal AppThemeBinding GetBinding()
	{
		var binding = new AppThemeBinding();
		if (isDarkSet)
		{
			binding.Dark = Dark;
		}

		if (isLightSet)
		{
			binding.Light = Light;
		}

		if (isDefaultSet)
		{
			binding.Default = Default;
		}

		return binding;
	}
}

