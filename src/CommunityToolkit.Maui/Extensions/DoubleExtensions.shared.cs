namespace CommunityToolkit.Maui.Extensions;

#pragma warning disable IDE0040 // Add accessibility modifiers
internal static class DoubleExtensions
#pragma warning restore IDE0040 // Add accessibility modifiers
{
		public static bool EqualsTo(this double value1, double value2)
		{
			const double TOLERANCE = 0.0000001;
			return Math.Abs(value1 - value2) < TOLERANCE; 
		}
}