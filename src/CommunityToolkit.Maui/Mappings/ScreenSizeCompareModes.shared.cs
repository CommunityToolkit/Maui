namespace CommunityToolkit.Maui.Mappings;

/// <summary>
/// Comparison modes.
/// Used to determine how a <see cref="SizeMappingInfo.DiagonalSize"/> should be compared against an actual device diagonal size.
/// </summary>
public enum ScreenSizeCompareModes
{
	/// <summary>
	/// Smaller than or equals to the current device diagonal screen size.
	/// </summary>
	SmallerOrEqualsTo = 0,
	/// <summary>
	/// Compares the current device diagonal screen size that is running the app to match the exact diagonal screen size provided
	/// </summary>
	SpecificSize = 1
}