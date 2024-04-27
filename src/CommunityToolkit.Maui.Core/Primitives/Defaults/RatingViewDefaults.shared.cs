namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Default Values for RatingView
/// </summary>
public static class RatingViewDefaults
{
	/// <summary>
	/// Default value for <see cref="CurrentRating"/>
	/// </summary>
	public const int CurrentRating = 0;
	
	/// <summary>
	/// Default value for <see cref="MaximumRating"/>
	/// </summary>
	public const int MaximumRating = 5;
	
	/// <summary>
	/// Default value for <see cref="Size"/>
	/// </summary>
	public const double Size = 20.0;
	
	/// <summary>
	/// Default value for <see cref="Spacing"/>
	/// </summary>
	public const double Spacing = 10.0;
	
	/// <summary>
	/// Default value for <see cref="StrokeThickness"/>
	/// </summary>
	public const double StrokeThickness = 7.0;
	
	/// <summary>
	/// Default value for <see cref="IsEnabled"/>
	/// </summary>
	public const bool IsEnabled = true;
	
	/// <summary>
	/// Default value for <see cref="FilledBackgroundColor"/>
	/// </summary>
	public static Color FilledBackgroundColor { get; } = Colors.Yellow;
	
	/// <summary>
	/// Default value for <see cref="EmptyBackgroundColor"/>
	/// </summary>
	public static Color EmptyBackgroundColor { get; } = Colors.Transparent;
	
	/// <summary>
	/// Default value for <see cref="StrokeColor"/>
	/// </summary>
	public static Color StrokeColor { get; } = Colors.Grey;
}