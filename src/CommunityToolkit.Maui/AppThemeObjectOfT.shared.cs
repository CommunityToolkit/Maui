namespace CommunityToolkit.Maui;

/// <summary>
/// Represents an object that is aware of the operating system theme.
/// </summary>
public abstract class AppThemeObject<T>
{
	/// <summary>
	/// The <see cref="object"/> that is used when the operating system uses light theme.
	/// </summary>
	public T? Light { get; set; }

	/// <summary>
	/// The <see cref="object"/> that is used when the operating system uses dark theme.
	/// </summary>
	public T? Dark { get; set; }

	/// <summary>
	/// The <see cref="object"/> that is used when the current theme is unspecified or
	/// when a value is not provided for <see cref="Light"/> or <see cref="Dark"/>.
	/// </summary>
	public T? Default { get; set; }

	public virtual BindingBase GetBinding()
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
