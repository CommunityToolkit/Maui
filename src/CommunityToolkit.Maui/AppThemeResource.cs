namespace CommunityToolkit.Maui;

/// <summary>
/// 
/// </summary>
public class AppThemeResource
{
	/// <summary>
	/// 
	/// </summary>
	public object? Light { get; set; }

	/// <summary>
	/// 
	/// </summary>
	public object? Dark { get; set; }

	/// <summary>
	/// 
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
