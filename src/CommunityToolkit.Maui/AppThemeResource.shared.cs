namespace CommunityToolkit.Maui;

/// <summary>
/// Represents a resource that is aware of the operating system theme.
/// </summary>
public class AppThemeResource
{
	/// <summary>
	/// The <see cref="object"/> that is used when the operating system uses light theme.
	/// </summary>
	public object? Light { get; set; }

	/// <summary>
	/// The <see cref="object"/> that is used when the operating system uses dark theme.
	/// </summary>
	public object? Dark { get; set; }

	/// <summary>
	/// The <see cref="object"/> that is used when the current theme is unspecified or
	/// when a value is not provided for <see cref="Light"/> or <see cref="Dark"/>.
	/// </summary>
	public object? Default { get; set; }

	internal BindingBase GetBinding()
	{
		var binding = new AppThemeBinding();

		if (Light is not null)
		{
			binding.Light = Light;
		}

		if (Dark is not null)
		{
			binding.Dark = Dark;
		}

		if (Default is not null)
		{
			binding.Default = Default;
		}

		return binding;
	}
}
