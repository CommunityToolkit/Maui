using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using Microsoft.Maui.Controls.Shapes;

namespace CommunityToolkit.Maui.Views;

sealed partial class PopupPage<T>(Popup<T> popup, IPopupOptions popupOptions)
	: PopupPage(popup, popupOptions)
{
	public PopupPage(View view, IPopupOptions popupOptions)
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
	readonly WeakEventManager popupClosedEventManager = new();

	public PopupPage(View view, IPopupOptions popupOptions)
		: this(view as Popup ?? CreatePopupFromView<Popup>(view), popupOptions)
	{
		ArgumentNullException.ThrowIfNull(view);
	}

	public PopupPage(Popup popup, IPopupOptions popupOptions)
	{
		ArgumentNullException.ThrowIfNull(popup);
		ArgumentNullException.ThrowIfNull(popupOptions);

		this.popup = popup;
		this.popupOptions = popupOptions;

		// Only set the content if the parent constructor hasn't set the content already; don't override content if it already exists
		base.Content ??= new PopupPageLayout(popup, popupOptions);

		tapOutsideOfPopupCommand = new Command(async () =>
		{
			popupOptions.OnTappingOutsideOfPopup?.Invoke();
			await CloseAsync(new PopupResult(true));
		}, () => popupOptions.CanBeDismissedByTappingOutsideOfPopup);

		Content.GestureRecognizers.Add(new TapGestureRecognizer { Command = tapOutsideOfPopupCommand });

		if (popupOptions is BindableObject bindablePopupOptions)
		{
			bindablePopupOptions.PropertyChanged += HandlePopupPropertyChanged;
		}

		this.SetBinding(BindingContextProperty, static (Popup x) => x.BindingContext, source: popup, mode: BindingMode.OneWay);
		this.SetBinding(BackgroundColorProperty, static (IPopupOptions options) => options.PageOverlayColor, source: popupOptions, mode: BindingMode.OneWay);

		Shell.SetPresentationMode(this, PresentationMode.ModalNotAnimated);
		On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.OverFullScreen);
	}

	public event EventHandler<IPopupResult> PopupClosed
	{
		add => popupClosedEventManager.AddEventHandler(value);
		remove => popupClosedEventManager.RemoveEventHandler(value);
	}

	// Prevent Content from being set by external class
	// Casts `PopupPage.Content` to return typeof(PopupPageLayout)
	internal new PopupPageLayout Content => (PopupPageLayout)base.Content;

	public async Task CloseAsync(PopupResult result, CancellationToken token = default)
	{
		// We first call `.ThrowIfCancellationRequested()` to ensure we don't throw one of the `InvalidOperationException`s (below) if the `CancellationToken` has already been canceled.
		// This ensures we throw the correct `OperationCanceledException` in the rare scenario where a developer cancels the token, then manually calls `Navigation.PopModalAsync()` before calling `Popup.CloseAsync()`
		// It may feel a bit redundant, given that we again call `ThrowIfCancellationRequested` later in this method, however, this ensures we propagate the correct Exception to the developer.
		token.ThrowIfCancellationRequested();

		var popupPageToClose = Navigation.ModalStack.OfType<PopupPage>().LastOrDefault(popupPage => popupPage.Content == Content);

		if (popupPageToClose is null)
		{
			throw new PopupNotFoundException();
		}

		if (Navigation.ModalStack[^1] is Microsoft.Maui.Controls.Page currentVisibleModalPage
			&& currentVisibleModalPage != popupPageToClose)
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

		popupClosedEventManager.HandleEvent(this, result, nameof(PopupClosed));
	}

	protected override bool OnBackButtonPressed()
	{
		// Only close the Popup if PopupOptions.CanBeDismissedByTappingOutsideOfPopup is true
		if (popupOptions.CanBeDismissedByTappingOutsideOfPopup)
		{
			CloseAsync(new PopupResult(true), CancellationToken.None).SafeFireAndForget();
		}

		// Always return true to let the Android Operating System know that we are manually handling the Navigation request from the Android Back Button
		return true;
	}

	protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
	{
		popup.NotifyPopupIsClosed();
		base.OnNavigatedFrom(args);
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
			BackgroundColor = view.BackgroundColor ??= PopupDefaults.BackgroundColor,
			Content = view
		};
		popup.SetBinding(BackgroundProperty, static (View view) => view.Background, source: view, mode: BindingMode.OneWay);
		popup.SetBinding(BindingContextProperty, static (View view) => view.BindingContext, source: view, mode: BindingMode.OneWay);
		popup.SetBinding(BackgroundColorProperty, static (View view) => view.BackgroundColor, source: view, mode: BindingMode.OneWay);
		popup.SetBinding(Popup.MarginProperty, static (View view) => view.Margin, source: view, mode: BindingMode.OneWay);
		popup.SetBinding(Popup.VerticalOptionsProperty, static (View view) => view.VerticalOptions, source: view, mode: BindingMode.OneWay, converter: new VerticalOptionsConverter());
		popup.SetBinding(Popup.HorizontalOptionsProperty, static (View view) => view.HorizontalOptions, source: view, mode: BindingMode.OneWay, converter: new HorizontalOptionsConverter());

		if (view is IPaddingElement paddingElement)
		{
			popup.SetBinding(Popup.PaddingProperty, static (IPaddingElement paddingElement) => paddingElement.Padding, source: paddingElement, mode: BindingMode.OneWay, converter: new PaddingConverter());
		}

		return popup;
	}

	void HandlePopupPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(IPopupOptions.CanBeDismissedByTappingOutsideOfPopup))
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

	internal sealed partial class PopupPageLayout : Grid
	{
		public PopupPageLayout(in Popup popupContent, in IPopupOptions options)
		{
			Background = BackgroundColor = null;

			Border = new Border
			{
				BackgroundColor = popupContent.BackgroundColor ??= PopupDefaults.BackgroundColor,
				Content = popupContent
			};
			Border.GestureRecognizers.Add(new TapGestureRecognizer()); // Blocks `tapOutsideOfPopupCommand` from closing the Popup when the content is tapped

			// Bind `Popup` values through to Border using OneWay Bindings 
			Border.SetBinding(Border.MarginProperty, static (Popup popup) => popup.Margin, source: popupContent, mode: BindingMode.OneWay);
			Border.SetBinding(Border.PaddingProperty, static (Popup popup) => popup.Padding, source: popupContent, mode: BindingMode.OneWay);
			Border.SetBinding(Border.BackgroundProperty, static (Popup popup) => popup.Background, source: popupContent, mode: BindingMode.OneWay);
			Border.SetBinding(Border.BackgroundColorProperty, static (Popup popup) => popup.BackgroundColor, source: popupContent, mode: BindingMode.OneWay);
			Border.SetBinding(Border.VerticalOptionsProperty, static (Popup popup) => popup.VerticalOptions, source: popupContent, mode: BindingMode.OneWay);
			Border.SetBinding(Border.HorizontalOptionsProperty, static (Popup popup) => popup.HorizontalOptions, source: popupContent, mode: BindingMode.OneWay);

			// Bind `PopupOptions` values through to Border using OneWay Bindings
			Border.SetBinding(Border.ShadowProperty, static (IPopupOptions options) => options.Shadow, source: options, mode: BindingMode.OneWay);
			Border.SetBinding(Border.StrokeProperty, static (IPopupOptions options) => options.Shape, source: options, converter: new BorderStrokeConverter(), mode: BindingMode.OneWay);
			Border.SetBinding(Border.StrokeShapeProperty, static (IPopupOptions options) => options.Shape, source: options, mode: BindingMode.OneWay);
			Border.SetBinding(Border.StrokeThicknessProperty, static (IPopupOptions options) => options.Shape, source: options, converter: new BorderStrokeThicknessConverter(), mode: BindingMode.OneWay);

			Children.Add(Border);
		}

		public Border Border { get; }

		sealed partial class BorderStrokeThicknessConverter : BaseConverterOneWay<Shape?, double>
		{
			public override double DefaultConvertReturnValue { get; set; } = PopupOptionsDefaults.BorderStrokeThickness;

			public override double ConvertFrom(Shape? value, CultureInfo? culture) => value?.StrokeThickness ?? 0;
		}

		sealed partial class BorderStrokeConverter : BaseConverterOneWay<Shape?, Brush?>
		{
			public override Brush? DefaultConvertReturnValue { get; set; } = PopupOptionsDefaults.BorderStroke;

			public override Brush? ConvertFrom(Shape? value, CultureInfo? culture) => value?.Stroke;
		}
	}

	sealed partial class PaddingConverter : BaseConverterOneWay<Thickness, Thickness>
	{
		public override Thickness DefaultConvertReturnValue { get; set; } = PopupDefaults.Padding;

		public override Thickness ConvertFrom(Thickness value, CultureInfo? culture) => value == default ? PopupDefaults.Padding : value;
	}

	sealed partial class HorizontalOptionsConverter : BaseConverterOneWay<LayoutOptions, LayoutOptions>
	{
		public override LayoutOptions DefaultConvertReturnValue { get; set; } = PopupDefaults.HorizontalOptions;

		public override LayoutOptions ConvertFrom(LayoutOptions value, CultureInfo? culture) => value == LayoutOptions.Fill ? PopupDefaults.HorizontalOptions : value;
	}

	sealed partial class VerticalOptionsConverter : BaseConverterOneWay<LayoutOptions, LayoutOptions>
	{
		public override LayoutOptions DefaultConvertReturnValue { get; set; } = PopupDefaults.VerticalOptions;

		public override LayoutOptions ConvertFrom(LayoutOptions value, CultureInfo? culture) => value == LayoutOptions.Fill ? PopupDefaults.VerticalOptions : value;
	}
}