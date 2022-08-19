namespace CommunityToolkit.Maui.Extensions;

static class DoubleExtensions
{
	public static bool EqualsTo(this double value1, double value2)
	{
		const double TOLERANCE = 0.0000001;
		return Math.Abs(value1 - value2) < TOLERANCE;
	}
}