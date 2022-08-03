namespace CommunityToolkit.Maui.Core.Views.OnScreenSize;


#if (WINDOWS)
/// <summary>
/// Platform-specifics for getting specific screen information.
/// </summary>
public static partial class OnScreenSizePlatform
{
	/// <summary>
	/// Returns how many horizontal/vertical pixels-per-inches the current device screen has.
	/// </summary>
	/// <returns></returns>
	public static bool TryGetPixelPerInches(out double xdpi, out double ydpi)
	{
		xdpi = 0;
		ydpi = 0;
		return false;
	}
}
#endif