namespace CommunityToolkit.Maui;

/// <summary>
/// Represents a color that is aware of the operating system theme.
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
	/// The <see cref="Color"/> that is used when the operating system uses light theme.
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
	/// The <see cref="Color"/> that is used when the operating system uses dark theme.
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
	/// The <see cref="Color"/> that is used when the current theme is unspecified or
	/// when a value is not provided for <see cref="Light"/> or <see cref="Dark"/>.
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
