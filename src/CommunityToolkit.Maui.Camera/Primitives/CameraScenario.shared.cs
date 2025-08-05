namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Base class for output based processing for camera related activities.
/// </summary>
public abstract class CameraScenario : BindableObject
{
	/// <summary>
	/// Gets whether the scenario has been initialized.
	/// </summary>
	public bool IsInitialized { get; protected set; }

	/// <summary>
	/// Initializes the scenario.
	/// </summary>
	public virtual void Initialize()
	{
		IsInitialized = true;
	}
}