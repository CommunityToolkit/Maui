namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
///	The native multimedia player that controls the native video view on Tizen.
/// </summary>
public class TizenPlayer : Tizen.Multimedia.Player
{
	bool isInitialized;

	/// <summary>
	/// Initializes a new instance of the TizenPlayer class.
	/// </summary>
	/// <param name="handle"></param>
	public TizenPlayer(IntPtr handle) : base(handle, (code, message) => { throw GetException(code, message); })
	{
	}

	/// <summary>
	/// Indicates whether or not the source is set.
	/// </summary>
	public bool IsSourceSet => base.HasSource;

	/// <summary>
	/// Initializes the Tizen native multimedia player.
	/// </summary>
	public void InitializePlayer()
	{
		if (!isInitialized)
		{
			Initialize();
			isInitialized = true;
		}
	}
}

