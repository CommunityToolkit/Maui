using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace CommunityToolkit.Maui.Extensions;

/// <summary>
/// Enables users to respond to culture changes at runtime.
/// </summary>
public class LocalizationResourceManager : INotifyPropertyChanged
{
	ResourceManager? resourceManager;
	CultureInfo currentCulture;

	/// <summary>
	/// Initialize a new instance of <see cref="LocalizationResourceManager"/>.
	/// </summary>
	LocalizationResourceManager()
	{
		currentCulture = CultureInfo.CurrentCulture;
	}

	/// <summary>
	/// Instance of <see cref="LocalizationResourceManager"/>.
	/// </summary>
	public static LocalizationResourceManager Current { get; } = new();

	/// <summary>
	/// Current culture.
	/// </summary>
	public CultureInfo CurrentCulture
	{
		get => currentCulture;
		set
		{
			currentCulture = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
		}
	}

	/// <summary>
	/// Get resource object by name.
	/// </summary>
	/// <param name="resourceKey">Resource name.</param>
	/// <returns>Resource object if exist, otherwise empty byte array.</returns>
	public object this[string resourceKey] => GetValue(resourceKey);

	/// <summary>
	/// Get resource object by name.
	/// </summary>
	/// <param name="resourceKey">Resource name.</param>
	/// <returns>Resource object if exist, otherwise empty byte array.</returns>
	/// <exception cref="NullReferenceException">In case Init method is not called.</exception>
	public object GetValue(string resourceKey)
	{
		if (resourceManager is null)
		{
			throw new NullReferenceException("Call Init method first");
		}

		return resourceManager.GetObject(resourceKey, CurrentCulture) ?? Array.Empty<byte>();
	}

	/// <summary>
	/// <inheritdoc />
	/// </summary>
	public event PropertyChangedEventHandler? PropertyChanged;
	
	/// <summary>
	/// Initialize Resource manager.
	/// </summary>
	/// <param name="manager"><see cref="ResourceManager"/>.</param>
	public void Init(ResourceManager manager)
	{
		resourceManager = manager;
	}

	/// <summary>
	/// Initialize Resource manager and its culture.
	/// </summary>
	/// <param name="manager"><see cref="ResourceManager"/>.</param>
	/// <param name="cultureInfo"><see cref="CultureInfo"/>.</param>
	public void Init(ResourceManager manager, CultureInfo cultureInfo)
	{
		Init(manager);
		CurrentCulture = cultureInfo;
	}
}