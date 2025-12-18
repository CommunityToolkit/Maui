using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using Microsoft.Maui.Controls.Shapes;
using NavigationPage = Microsoft.Maui.Controls.NavigationPage;
using Page = Microsoft.Maui.Controls.Page;

namespace CommunityToolkit.Maui.Views;

sealed partial class PopupPage<T>(Popup<T> popup, IPopupOptions? popupOptions)
	: PopupPage(popup, popupOptions)
{
	public PopupPage(View view, IPopupOptions? popupOptions)
		: this(view as Popup<T> ?? CreatePopupFromView<Popup<T>>(view), popupOptions)
	{
	}

	public Task CloseAsync(PopupResult<T> result, CancellationToken token = default) => base.CloseAsync(result, token);
}

partial class PopupPage : ContentPage, IQueryAttributable
{
	readonly Popup popup;
	readonly IPopupOptions popupOptions;
	readonly Command tapOutsideOfPopupCommand;

	public PopupPage(View view, IPopupOptions? popupOptions)
		: this(view as Popup ?? CreatePopupFromView<Popup>(view), popupOptions)
	{
		ArgumentNullException.ThrowIfNull(view);
	}

	public PopupPage(Popup popup, IPopupOptions? popupOptions)
	{
		ArgumentNullException.ThrowIfNull(popup);

		this.popup = popup;
		this.popupOptions = popupOptions ??= Options.DefaultPopupOptionsSettings;

		tapOutsideOfPopupCommand = new Command(async () =>
		{
			popupOptions.OnTappingOutsideOfPopup?.Invoke();
			await CloseAsync(new PopupResult(true));
		}, () => GetCanBeDismissedByTappingOutsideOfPopup(popup, popupOptions));

		var popupPageLayout = new PopupPageLayout(popup, popupOptions, () => TryExecuteTapOutsideOfPopupCommand());
		base.Content = popupPageLayout;

		popup.PropertyChanged += HandlePopupPropertyChanged;
		if (popupOptions is BindableObject bindablePopupOptions)
		{
			bindablePopupOptions.PropertyChanged += HandlePopupOptionsPropertyChanged;
		}

		this.SetBinding(BindingContextProperty, static (Popup x) => x.BindingContext, source: popup, mode: BindingMode.OneWay);
		this.SetBinding(BackgroundColorProperty, static (IPopupOptions options) => options.PageOverlayColor, source: popupOptions, mode: BindingMode.OneWay);

		Shell.SetPresentationMode(this, PresentationMode.ModalNotAnimated);
		On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.OverFullScreen);
		NavigationPage.SetHasNavigationBar(this, false);
	}

	public event EventHandler<IPopupResult>? PopupClosed;

	// Prevent Content from being set by external class
	// Casts `PopupPage.Content` to return typeof(PopupPageLayout)
	internal new PopupPageLayout Content => (PopupPageLayout)base.Content;

	public async Task CloseAsync(PopupResult result, CancellationToken token = default)
	{
		// We first call `.ThrowIfCancellationRequested()` to ensure we don't throw one of the `InvalidOperationException`s (below) if the `CancellationToken` has already been canceled.
		// This ensures we throw the correct `OperationCanceledException` in the rare scenario where a developer cancels the token, then manually calls `Navigation.PopModalAsync()` before calling `Popup.CloseAsync()`
		// It may feel a bit redundant, given that we again call `ThrowIfCancellationRequested` later in this method, however, this ensures we propagate the correct Exception to the developer.
		token.ThrowIfCancellationRequested();

		// Handle edge case where a Popup was pushed inside a custom IPageContainer (e.g. a NavigationPage) on the Modal Stack
		var customPageContainer = Navigation.ModalStack.OfType<IPageContainer<Page>>().LastOrDefault();
		if (customPageContainer is not null && customPageContainer.CurrentPage is not PopupPage)
		{
			throw new PopupNotFoundException();
		}

		var popupPageToClose = customPageContainer?.CurrentPage as PopupPage
							   ?? Navigation.ModalStack.OfType<PopupPage>().LastOrDefault()
							   ?? throw new PopupNotFoundException();

		// PopModalAsync will pop the last (top) page from the ModalStack
		// Ensure that the PopupPage the user is attempting to close is the last (top) page on the Modal stack before calling Navigation.PopModalAsync
		if (Navigation.ModalStack[^1] is IPageContainer<Page> { CurrentPage: PopupPage visiblePopupPageInCustomPageContainer }
			 && visiblePopupPageInCustomPageContainer.Content != Content)
		{
			throw new PopupBlockedException(visiblePopupPageInCustomPageContainer);
		}
		else if (Navigation.ModalStack[^1] is ContentPage currentVisibleModalPage
				 && currentVisibleModalPage.Content != Content)
		{
			throw new PopupBlockedException(currentVisibleModalPage);
		}

		// We call `.ThrowIfCancellationRequested()` again to avoid a race condition where a developer cancels the CancellationToken after we check for an InvalidOperationException
		// At first glance, it may look redundant given that we are using `.WaitAsync(token)` in the next step,
		// However, `Navigation.PopModalAsync()` may return a completed Task, and when a completed Task is returned, `.WaitAsync(token)` is never invoked.
		// In other words, `.WaitAsync(token)` may not throw an `OperationCanceledException` as expected which is why we call `.ThrowIfCancellationRequested()` again here
		// Here's the .NET MAUI Source code demonstrating that `Navigation.PopModalAsync()` sometimes returns `Task.FromResult()`: https://github.com/dotnet/maui/blob/e5c252ec7f430cbaf28c8a815a249e3270b49844/src/Controls/src/Core/NavigationProxy.cs#L192-L196
		token.ThrowIfCancellationRequested();
		await Navigation.PopModalAsync(false).WaitAsync(token);

		// Clean up Popup resources
		Content.TapGestureGestureOverlay.GestureRecognizers.Clear();
		popup.PropertyChanged -= HandlePopupPropertyChanged;

		PopupClosed?.Invoke(this, result);
		popup.NotifyPopupIsClosed();
	}

	protected override bool OnBackButtonPressed()
	{
		TryExecuteTapOutsideOfPopupCommand();

		// Always return true to let the Android Operating System know that we are manually handling the Navigation request from the Android Back Button
		return true;
	}

	protected override void OnNavigatedTo(NavigatedToEventArgs args)
	{
		base.OnNavigatedTo(args);
		popup.NotifyPopupIsOpened();
	}

	protected static T CreatePopupFromView<T>(in View view) where T : Popup, new()
	{
		ArgumentNullException.ThrowIfNull(view);

		var popup = new T
		{
			Content = view
		};
		popup.SetBinding(BackgroundProperty, static (View view) => view.Background, source: view, mode: BindingMode.OneWay);
		popup.SetBinding(BindingContextProperty, static (View view) => view.BindingContext, source: view, mode: BindingMode.OneWay);
		popup.SetBinding(BackgroundColorProperty, static (View view) => view.BackgroundColor, source: view, mode: BindingMode.OneWay);
		popup.SetBinding(Popup.MarginProperty, static (View view) => view.Margin, source: view, mode: BindingMode.OneWay, converter: new MarginConverter());
		popup.SetBinding(Popup.VerticalOptionsProperty, static (View view) => view.VerticalOptions, source: view, mode: BindingMode.OneWay);
		popup.SetBinding(Popup.HorizontalOptionsProperty, static (View view) => view.HorizontalOptions, source: view, mode: BindingMode.OneWay);

		if (view is IPaddingElement paddingElement)
		{
			popup.SetBinding(Popup.PaddingProperty, static (IPaddingElement paddingElement) => paddingElement.Padding, source: paddingElement, mode: BindingMode.OneWay, converter: new PaddingConverter());
		}

		return popup;
	}

	internal bool TryExecuteTapOutsideOfPopupCommand()
	{
		if (!tapOutsideOfPopupCommand.CanExecute(null))
		{
			return false;
		}

		tapOutsideOfPopupCommand.Execute(null);
		return true;
	}

	// Only dismiss when a user taps outside Popup when **both** Popup.CanBeDismissedByTappingOutsideOfPopup and PopupOptions.CanBeDismissedByTappingOutsideOfPopup are true
	// If either value is false, do not dismiss Popup
	static bool GetCanBeDismissedByTappingOutsideOfPopup(in Popup popup, in IPopupOptions popupOptions) => popup.CanBeDismissedByTappingOutsideOfPopup & popupOptions.CanBeDismissedByTappingOutsideOfPopup;

	void HandlePopupOptionsPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(IPopupOptions.CanBeDismissedByTappingOutsideOfPopup))
		{
			tapOutsideOfPopupCommand.ChangeCanExecute();
		}
	}

	void HandlePopupPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == Popup.CanBeDismissedByTappingOutsideOfPopupProperty.PropertyName)
		{
			tapOutsideOfPopupCommand.ChangeCanExecute();
		}
	}

	void IQueryAttributable.ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (popup is IQueryAttributable popupIQueryAttributable)
		{
			popupIQueryAttributable.ApplyQueryAttributes(query);
		}

		if (popup.Content is IQueryAttributable popupContentIQueryAttributable)
		{
			popupContentIQueryAttributable.ApplyQueryAttributes(query);
		}
	}

	internal sealed partial class PopupGestureOverlay : BoxView
	{
		public PopupGestureOverlay()
		{
			BackgroundColor = Colors.Transparent;
			Background = Brush.Transparent;
		}
	}

	internal sealed partial class PopupPageLayout : Grid
	{
		readonly Action tryExecuteTapOutsideOfPopupCommand;

		public PopupPageLayout(in Popup popupContent, in IPopupOptions options, in Action tryExecuteTapOutsideOfPopupCommand)
		{
			this.tryExecuteTapOutsideOfPopupCommand = tryExecuteTapOutsideOfPopupCommand;
			Background = BackgroundColor = null;

			PopupBorder = new Border
			{
				BackgroundColor = popupContent.BackgroundColor ??= Options.DefaultPopupSettings.BackgroundColor,
				Content = popupContent
			};

			// Bind `Popup` values through to Border using OneWay Bindings 
			PopupBorder.SetBinding(Border.MarginProperty, static (Popup popup) => popup.Margin, source: popupContent, mode: BindingMode.OneWay, converter: new MarginConverter());
			PopupBorder.SetBinding(Border.BackgroundProperty, static (Popup popup) => popup.Background, source: popupContent, mode: BindingMode.OneWay);
			PopupBorder.SetBinding(Border.BackgroundColorProperty, static (Popup popup) => popup.BackgroundColor, source: popupContent, mode: BindingMode.OneWay, converter: new BackgroundColorConverter());
			PopupBorder.SetBinding(Border.VerticalOptionsProperty, static (Popup popup) => popup.VerticalOptions, source: popupContent, mode: BindingMode.OneWay, converter: new VerticalOptionsConverter());
			PopupBorder.SetBinding(Border.HorizontalOptionsProperty, static (Popup popup) => popup.HorizontalOptions, source: popupContent, mode: BindingMode.OneWay, converter: new HorizontalOptionsConverter());

			// Bind `PopupOptions` values through to Border using OneWay Bindings
			PopupBorder.SetBinding(Border.ShadowProperty, static (IPopupOptions options) => options.Shadow, source: options, mode: BindingMode.OneWay);
			PopupBorder.SetBinding(Border.StrokeProperty, static (IPopupOptions options) => options.Shape, source: options, mode: BindingMode.OneWay, converter: new BorderStrokeConverter());
			PopupBorder.SetBinding(Border.StrokeShapeProperty, static (IPopupOptions options) => options.Shape, source: options, mode: BindingMode.OneWay);
			PopupBorder.SetBinding(Border.StrokeThicknessProperty, static (IPopupOptions options) => options.Shape, source: options, mode: BindingMode.OneWay, converter: new BorderStrokeThicknessConverter());

			var overlayTapGestureRecognizer = new TapGestureRecognizer();
			overlayTapGestureRecognizer.Tapped += HandleOverlayTapped;
			TapGestureGestureOverlay = new PopupGestureOverlay();
			TapGestureGestureOverlay.GestureRecognizers.Add(overlayTapGestureRecognizer);

			Children.Add(TapGestureGestureOverlay);
			Children.Add(PopupBorder);
		}

		void HandleOverlayTapped(object? sender, TappedEventArgs e)
		{
			ArgumentNullException.ThrowIfNull(sender);

			var position = e.GetPosition(this);

			if (position is null)
			{
				return;
			}

			// Execute tapOutsideOfPopupCommand only if tap occurred outside the PopupBorder 
			if (PopupBorder.Bounds.Contains(position.Value) is false)
			{
				tryExecuteTapOutsideOfPopupCommand();
			}
		}

		public Border PopupBorder { get; }
		public PopupGestureOverlay TapGestureGestureOverlay { get; }

		sealed partial class BorderStrokeThicknessConverter : BaseConverterOneWay<Shape?, double>
		{
			public override double DefaultConvertReturnValue { get; set; } = Options.DefaultPopupOptionsSettings.Shape?.StrokeThickness ?? 0;

			public override double ConvertFrom(Shape? value, CultureInfo? culture) => value?.StrokeThickness ?? 0;
		}

		sealed partial class BorderStrokeConverter : BaseConverterOneWay<Shape?, Brush?>
		{
			public override Brush? DefaultConvertReturnValue { get; set; } = Options.DefaultPopupOptionsSettings.Shape?.Stroke;

			public override Brush? ConvertFrom(Shape? value, CultureInfo? culture) => value?.Stroke;
		}

		sealed partial class HorizontalOptionsConverter : BaseConverterOneWay<LayoutOptions, LayoutOptions>
		{
			public override LayoutOptions DefaultConvertReturnValue { get; set; } = Options.DefaultPopupSettings.HorizontalOptions;

			public override LayoutOptions ConvertFrom(LayoutOptions value, CultureInfo? culture) => value == LayoutOptions.Fill ? Options.DefaultPopupSettings.HorizontalOptions : value;
		}

		sealed partial class VerticalOptionsConverter : BaseConverterOneWay<LayoutOptions, LayoutOptions>
		{
			public override LayoutOptions DefaultConvertReturnValue { get; set; } = Options.DefaultPopupSettings.VerticalOptions;

			public override LayoutOptions ConvertFrom(LayoutOptions value, CultureInfo? culture) => value == LayoutOptions.Fill ? Options.DefaultPopupSettings.VerticalOptions : value;
		}

		sealed partial class BackgroundColorConverter : BaseConverterOneWay<Color?, Color>
		{
			public override Color DefaultConvertReturnValue { get; set; } = Options.DefaultPopupSettings.BackgroundColor;

			public override Color ConvertFrom(Color? value, CultureInfo? culture) => value ?? Options.DefaultPopupSettings.BackgroundColor;
		}
	}

	sealed partial class PaddingConverter : BaseConverterOneWay<Thickness, Thickness>
	{
		public override Thickness DefaultConvertReturnValue { get; set; } = Options.DefaultPopupSettings.Padding;

		public override Thickness ConvertFrom(Thickness value, CultureInfo? culture) => value == default ? Options.DefaultPopupSettings.Padding : value;
	}

	sealed partial class MarginConverter : BaseConverterOneWay<Thickness, Thickness>
	{
		public override Thickness DefaultConvertReturnValue { get; set; } = Options.DefaultPopupSettings.Margin;

		public override Thickness ConvertFrom(Thickness value, CultureInfo? culture) => value == default ? Options.DefaultPopupSettings.Margin : value;
	}
}