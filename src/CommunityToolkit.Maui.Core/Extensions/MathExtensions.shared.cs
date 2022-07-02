namespace CommunityToolkit.Maui.Core.Extensions;

/// <summary>
/// Extension methods to help with Math.
/// </summary>
public static class MathExtensions
{
	/// <summary>
	/// Check if the number is zero or NaN.
	/// </summary>
	/// <param name="number">The <see cref="double"/> that you want to check.</param>
	/// <returns>A <see cref="Boolean"/> value that indicates if the number is zero or NaN.</returns>
	public static bool IsZeroOrNaN(this double number) => number is 0 or Double.NaN;
}