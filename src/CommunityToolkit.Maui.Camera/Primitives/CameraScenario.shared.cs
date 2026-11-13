namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Base class for output based processing for camera related activities.
/// </summary>
public abstract class CameraScenario : BindableObject, IDisposable
{
	private protected CameraScenario()
	{
	}
	
	/// <summary>
	/// Called when the scenario is attached to the platform layer.
	/// </summary>
	public virtual void OnAttached()
	{
	}
	
	/// <summary>
	/// Called when the scenario is detached from the platform layer.
	/// </summary>
	public virtual void OnDetached()
	{
	}

	/// <summary>
	/// Disposes the scenario.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Disposes the scenario.
	/// </summary>
	/// <param name="disposing">Whether the scenario is being disposed.</param>
	protected virtual void Dispose(bool disposing)
	{
	}
}
