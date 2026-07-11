namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Default Values for RangeSlider
/// </summary>
public static class RangeSliderDefaults
{
	/// <summary>
	/// Default width request
	/// </summary>
	public const double WidthRequest = 200;

	/// <summary>
	/// Default for Minimum Value
	/// </summary>
	public const double MinimumValue = 0;

	/// <summary>
	/// Default for Maximum Value
	/// </summary>
	public const double MaximumValue = 1;

	/// <summary>
	/// Default for Lower Value
	/// </summary>
	public const double LowerValue = 0;

	/// <summary>
	/// Default for Upper Value
	/// </summary>
	public const double UpperValue = 1;

	/// <summary>
	/// Default for Step Size
	/// </summary>
	public const double StepSize = 0;

	/// <summary>
	/// Default value for Lower Thumb Color
	/// </summary>
	public static Color LowerThumbColor { get; } = Colors.Gray;

	/// <summary>
	/// Default value for Upper Thumb Color
	/// </summary>
	public static Color UpperThumbColor { get; } = Colors.Gray;

	/// <summary>
	/// Default value for Track Color
	/// </summary>
	public static Color InnerTrackColor { get; } = Colors.Pink;

	/// <summary>
	/// Default value for Track Size
	/// </summary>
	public const double InnerTrackSize = 6;

	/// <summary>
	/// Default value for Inner Track Radius
	/// </summary>
	public static CornerRadius InnerTrackCornerRadius { get; } = new(3);

	/// <summary>
	/// Default value for Outer Track Color
	/// </summary>
	public static Color OuterTrackColor { get; } = Colors.LightGray;

	/// <summary>
	/// Default value for Outer Track Size
	/// </summary>
	public const double OuterTrackSize = 6;

	/// <summary>
	/// Default value for Outer Track Radius
	/// </summary>
	public static CornerRadius OuterTrackCornerRadius { get; } = new(3);

	/// <summary>
	/// Default value for Focus Mode
	/// </summary>
	public const RangeSliderFocusMode FocusMode = RangeSliderFocusMode.Default;
}