namespace CommunityToolkit.Maui.Core;

/// <summary>
/// .NET MAUI Community Toolkit Core Options.
/// </summary>
public class Options
{
	internal static bool ShouldUseStatusBarBehaviorOnAndroidModalPage { get; private set; }

	/// <summary>
	/// Enables the use of the DialogFragment Lifecycle service for Android.
	/// </summary>
	/// <param name="value">true if yes or false if you want to implement your own.</param>
	/// <remarks>
	/// Default value is fasle.
	/// </remarks>
	public void SetShouldUseStatusBarBehaviorOnAndroidModalPage(bool value) => ShouldUseStatusBarBehaviorOnAndroidModalPage = value;
}