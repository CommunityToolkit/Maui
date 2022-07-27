namespace CommunityToolkit.Maui.Helpers;

#pragma warning disable IDE0040 // Add accessibility modifiers
internal static class DoubleExtensions
#pragma warning restore IDE0040 // Add accessibility modifiers
{

	public static double RoundUp(this double value)
	{
		var valueUp = Math.Ceiling(value * 100) / 100;
		
		var valueRounded = Math.Round(valueUp,1);
		
		return (double)valueRounded;
	}	
}