namespace CommunityToolkit.Maui.Core.Extensions;

/// <summary>
/// Extensions for <see cref="UIView"/>
/// </summary>
public static class UIViewExtensions
{
	/// <summary>
	/// Safe bottom edge of the guide
	/// </summary>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="view"/> is null.</exception>
	public static NSLayoutYAxisAnchor SafeBottomAnchor(this UIView view)
	{
		ArgumentNullException.ThrowIfNull(view);

		return view.SafeAreaLayoutGuide.BottomAnchor;
	}

	/// <summary>
	/// Safe horizontal center of the guide
	/// </summary>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="view"/> is null.</exception>
	public static NSLayoutXAxisAnchor SafeCenterXAnchor(this UIView view)
	{
		ArgumentNullException.ThrowIfNull(view);

		return view.SafeAreaLayoutGuide.CenterXAnchor;
	}

	/// <summary>
	/// Safe vertical center of the guide
	/// </summary>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="view"/> is null.</exception>
	public static NSLayoutYAxisAnchor SafeCenterYAnchor(this UIView view)
	{
		ArgumentNullException.ThrowIfNull(view);

		return view.SafeAreaLayoutGuide.CenterYAnchor;
	}

	/// <summary>
	/// Safe vertical extent of the guide
	/// </summary>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="view"/> is null.</exception>
	public static NSLayoutDimension SafeHeightAnchor(this UIView view)
	{
		ArgumentNullException.ThrowIfNull(view);

		return view.SafeAreaLayoutGuide.HeightAnchor;
	}

	/// <summary>
	/// Safe leading edge of the guide
	/// </summary>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="view"/> is null.</exception>
	public static NSLayoutXAxisAnchor SafeLeadingAnchor(this UIView view)
	{
		ArgumentNullException.ThrowIfNull(view);

		return view.SafeAreaLayoutGuide.LeadingAnchor;
	}

	/// <summary>
	/// Safe left edge of the guide
	/// </summary>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="view"/> is null.</exception>
	public static NSLayoutXAxisAnchor SafeLeftAnchor(this UIView view)
	{
		ArgumentNullException.ThrowIfNull(view);

		return view.SafeAreaLayoutGuide.LeftAnchor;
	}

	/// <summary>
	/// Safe right edge of the guide
	/// </summary>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="view"/> is null.</exception>
	public static NSLayoutXAxisAnchor SafeRightAnchor(this UIView view)
	{
		ArgumentNullException.ThrowIfNull(view);

		return view.SafeAreaLayoutGuide.RightAnchor;
	}

	/// <summary>
	/// Safe top edge of the guide
	/// </summary>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="view"/> is null.</exception>
	public static NSLayoutYAxisAnchor SafeTopAnchor(this UIView view)
	{
		ArgumentNullException.ThrowIfNull(view);

		return view.SafeAreaLayoutGuide.TopAnchor;
	}

	/// <summary>
	/// Safe trailing edge of the guide
	/// </summary>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="view"/> is null.</exception>
	public static NSLayoutXAxisAnchor SafeTrailingAnchor(this UIView view)
	{
		ArgumentNullException.ThrowIfNull(view);

		return view.SafeAreaLayoutGuide.TrailingAnchor;
	}

	/// <summary>
	/// Safe width edge of the guide
	/// </summary>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="view"/> is null.</exception>
	public static NSLayoutDimension SafeWidthAnchor(this UIView view)
	{
		ArgumentNullException.ThrowIfNull(view);

		return view.SafeAreaLayoutGuide.WidthAnchor;
	}
}