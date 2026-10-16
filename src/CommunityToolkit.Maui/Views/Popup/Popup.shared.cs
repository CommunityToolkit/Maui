
using CommunityToolkit.Maui.Extensions;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// Represents a small View that pops up at front the Page.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="DefaultPopupSettings"/> passed to <see cref="Options.SetPopupDefaults(DefaultPopupSettings)"/> provide the default values of <see cref="Margin"/>, <see cref="Padding"/>, <see cref="HorizontalOptions"/>, <see cref="VerticalOptions"/> and <see cref="CanBeDismissedByTappingOutsideOfPopup"/>.
/// They are default values, not local values, so a <see cref="Style"/> (explicit, implicit, <c>StyleClass</c> or <c>DynamicResource</c>) can set these properties.
/// </para>
/// <para>
/// <see cref="VisualElement.BackgroundColor"/> is not set by default, and returns <see langword="null"/> until a value, a binding or a <see cref="Style"/> sets it.
/// While it is <see langword="null"/> and the <see cref="Popup"/> has no <see cref="VisualElement.Background"/>, the <see cref="Popup"/> is displayed with <see cref="DefaultPopupSettings.BackgroundColor"/>, or with <see cref="DefaultPopupSettings.BackgroundColorDark"/> while the app uses the dark theme (<see cref="AppTheme.Dark"/>), and follows changes of the theme.
/// </para>
/// <para>
/// When a <see cref="View"/> that is not a <see cref="Popup"/> is displayed, the <see cref="Popup"/> created to contain it binds its <see cref="Margin"/>, <see cref="HorizontalOptions"/>, <see cref="VerticalOptions"/>, <see cref="VisualElement.Background"/>, <see cref="VisualElement.BackgroundColor"/>
/// and, when the <see cref="View"/> has a padding, <see cref="Padding"/> to those of the <see cref="View"/>, so a <see cref="Style"/> cannot set them.
/// A margin or padding of the <see cref="View"/> that is 0 on every side gives the <see cref="Popup"/> <see cref="DefaultPopupSettings.Margin"/> or <see cref="DefaultPopupSettings.Padding"/> instead.
/// </para>
/// <para>
/// While the <see cref="Popup"/> is displayed, a <see cref="VisualElement.WidthRequest"/> or <see cref="VisualElement.HeightRequest"/> larger than the space inside the window
/// (minus <see cref="Margin"/> and the stroke of <see cref="IPopupOptions.Shape"/>) is limited to that space, so that the <see cref="Popup"/> keeps its <see cref="Margin"/> to the edges of the window.
/// The values of <see cref="VisualElement.WidthRequest"/> and <see cref="VisualElement.HeightRequest"/> do not change.
/// </para>
/// </remarks>
public partial class Popup : ContentView, IPaddingElement, IView
{
	Size maximumDisplaySize = new(double.PositiveInfinity, double.PositiveInfinity);

	/// <summary>
	/// Event occurs when <see cref="Popup"/> is opened.
	/// </summary>
	/// <remarks>
	/// Raised once each time the <see cref="Popup"/> is shown. It is not raised again when a page or another <see cref="Popup"/> displayed above this <see cref="Popup"/> is closed.
	/// </remarks>
	public event EventHandler? Opened;

	/// <summary>
	/// Event occurs when <see cref="Popup"/> is closed.
	/// </summary>
	public event EventHandler? Closed;

	/// <summary>
	/// Gets or sets the margin between the <see cref="Popup"/> and the edge of the window.
	/// </summary>
	/// <remarks>
	/// The default value is <see cref="DefaultPopupSettings.Margin"/>.
	/// While the margin is 0 on every side, the <see cref="Popup"/> is displayed with <see cref="DefaultPopupSettings.Margin"/>.
	/// </remarks>
	[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateDefaultMargin))]
	public new partial Thickness Margin { get; set; }

	/// <summary>
	/// Gets or sets the horizontal position of the <see cref="Popup"/> when displayed on screen.
	/// </summary>
	/// <remarks>
	/// The default value is <see cref="DefaultPopupSettings.HorizontalOptions"/>.
	/// While it is <see cref="LayoutOptions.Fill"/>, the <see cref="Popup"/> is displayed with <see cref="DefaultPopupSettings.HorizontalOptions"/>.
	/// </remarks>
	[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateDefaultHorizontalOptions))]
	public new partial LayoutOptions HorizontalOptions { get; set; }

	/// <summary>
	/// Gets or sets the vertical position of the <see cref="Popup"/> when displayed on screen.
	/// </summary>
	/// <remarks>
	/// The default value is <see cref="DefaultPopupSettings.VerticalOptions"/>.
	/// While it is <see cref="LayoutOptions.Fill"/>, the <see cref="Popup"/> is displayed with <see cref="DefaultPopupSettings.VerticalOptions"/>.
	/// </remarks>
	[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateDefaultVerticalOptions))]
	public new partial LayoutOptions VerticalOptions { get; set; }

	/// <inheritdoc cref="IPopupOptions.CanBeDismissedByTappingOutsideOfPopup"/>
	/// <remarks>
	/// While both this property and <see cref="IPopupOptions.CanBeDismissedByTappingOutsideOfPopup"/> are <see langword="true"/>, the <see cref="Popup"/> is dismissed when the user taps outside of it or, on Android, presses the hardware back button.
	/// While either is <see langword="false"/>, tapping outside of the <see cref="Popup"/> and the hardware back button do nothing.
	/// The default value is <see cref="DefaultPopupSettings.CanBeDismissedByTappingOutsideOfPopup"/>.
	/// </remarks>
	[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateDefaultCanBeDismissedByTappingOutsideOfPopup))]
	public partial bool CanBeDismissedByTappingOutsideOfPopup { get; set; }

	/// <summary>
	/// Gets or sets the padding between the <see cref="Popup"/> border and the <see cref="Popup"/> content.
	/// </summary>
	/// <remarks>The default value is <see cref="DefaultPopupSettings.Padding"/>.</remarks>
	public new Thickness Padding
	{
		get => base.Padding;
		set => base.Padding = value;
	}

	/// <summary>
	/// Close the Popup.
	/// </summary>
	public virtual Task CloseAsync(CancellationToken token = default) => GetPopupPage().CloseAsync(new PopupResult(false), token);

	internal void NotifyPopupIsOpened()
	{
		Opened?.Invoke(this, EventArgs.Empty);
	}

	internal void NotifyPopupIsClosed()
	{
		Closed?.Invoke(this, EventArgs.Empty);
	}

	// .NET MAUI's layout uses an explicit size (IView.Width and IView.Height) even when it is larger than the space available, so a Popup
	// whose WidthRequest or HeightRequest is larger than the window would cover the window, edge to edge, with no space left to tap outside of it.
	double IView.Width => GetExplicitSize(WidthRequestProperty, maximumDisplaySize.Width);

	double IView.Height => GetExplicitSize(HeightRequestProperty, maximumDisplaySize.Height);

	// Re-implementing IView maps its members to the public members this class declares, but Popup.Margin is the margin of the PopupBorder,
	// not of the Popup inside it: keep the implementation of View, which returns View.Margin
	Thickness IView.Margin => base.Margin;

	internal void SetMaximumDisplaySize(in Size maximumSize)
	{
		var view = (IView)this;
		var (width, height) = (view.Width, view.Height);

		maximumDisplaySize = maximumSize;

		// The platform view has its own copy of an explicit size on Windows (FrameworkElement.Width and Height)
		if (!width.Equals(view.Width))
		{
			Handler?.UpdateValue(nameof(IView.Width));
		}

		if (!height.Equals(view.Height))
		{
			Handler?.UpdateValue(nameof(IView.Height));
		}
	}

	// Same as VisualElement's IView.Width and IView.Height, but limited to maximumSize
	double GetExplicitSize(BindableProperty sizeRequestProperty, double maximumSize)
	{
		var sizeRequest = (double)GetValue(sizeRequestProperty);
		if (!IsSet(sizeRequestProperty) || sizeRequest is -1d)
		{
			return double.NaN;
		}

		return Math.Min(Math.Max(sizeRequest, 0), maximumSize);
	}

	// Padding is the inherited PaddingElement.PaddingProperty; its default value comes from IPaddingElement.PaddingDefaultValueCreator
	// Like the default values created below, it has a lower precedence than a Style, unlike a value set in the constructor
	Thickness IPaddingElement.PaddingDefaultValueCreator() => Options.DefaultPopupSettings.Padding;

	static object CreateDefaultMargin(BindableObject bindable) => Options.DefaultPopupSettings.Margin;

	static object CreateDefaultHorizontalOptions(BindableObject bindable) => Options.DefaultPopupSettings.HorizontalOptions;

	static object CreateDefaultVerticalOptions(BindableObject bindable) => Options.DefaultPopupSettings.VerticalOptions;

	static object CreateDefaultCanBeDismissedByTappingOutsideOfPopup(BindableObject bindable) => Options.DefaultPopupSettings.CanBeDismissedByTappingOutsideOfPopup;

	private protected PopupPage GetPopupPage()
	{
		var parent = Parent;

		while (parent is not null)
		{
			if (parent.Parent is PopupPage popupPage)
			{
				return popupPage;
			}

			parent = parent.Parent;
		}

		throw new PopupNotFoundException();
	}
}

/// <summary>
/// Represents a small View that pops up at front the Page.
/// </summary>
public partial class Popup<T> : Popup
{
	/// <summary>
	/// Close the Popup with a result.
	/// </summary>
	/// <param name="result">Popup result</param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	public virtual Task CloseAsync(T result, CancellationToken token = default) => GetPopupPage().CloseAsync(new PopupResult<T>(result, false), token);
}

/// <summary>
/// An <see cref="InvalidOperationException"/> thrown when attempting to close a <see cref="Popup"/> that cannot locate its associated <see cref="PopupPage"/>.
/// </summary>
public sealed class PopupNotFoundException() : InvalidPopupOperationException($"Unable to close popup: could not locate {nameof(PopupPage)}. {nameof(PopupExtensions.ShowPopup)} or {nameof(PopupExtensions.ShowPopupAsync)} must be called before {nameof(Popup.CloseAsync)}. If using a custom implementation of {nameof(Popup)}, override the {nameof(Popup.CloseAsync)} method");

/// <summary>
/// An <see cref="InvalidOperationException"/> thrown when attempting to close a <see cref="Popup"/> that is blocked by a visible modal page.
/// </summary>
public sealed class PopupBlockedException(in Page currentVisibleModalPage) : InvalidPopupOperationException($"Unable to close Popup because it is blocked by the Modal Page {currentVisibleModalPage.GetType().FullName}. Please call `{nameof(Page.Navigation)}.{nameof(Page.Navigation.PopModalAsync)}()` to first remove {currentVisibleModalPage.GetType().FullName} from the {nameof(Page.Navigation.ModalStack)}");

/// <summary>
/// An <see cref="InvalidOperationException"/> thrown when an invalid operation is performed on a <see cref="Popup"/>.
/// </summary>
public class InvalidPopupOperationException(in string message) : InvalidOperationException(message);