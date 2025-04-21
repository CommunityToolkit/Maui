using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using Microsoft.Maui.Controls.Shapes;

namespace CommunityToolkit.Maui.Views;

sealed partial class PopupPage<T>(Popup<T> popup, IPopupOptions popupOptions) : PopupPage(popup, popupOptions)
{
	public PopupPage(View view, IPopupOptions popupOptions)
		: this(view as Popup<T> ?? CreatePopupFromView<Popup<T>>(view), popupOptions)
	{
	}

	public Task Close(PopupResult<T> result, CancellationToken token = default) => base.Close(result, token);
}

partial class PopupPage : ContentPage
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

		// Only set the content if parent constructor hasn't set the content already; don't override content if it already exists
		base.Content ??= new PopupPageLayout(popup, popupOptions);

		tapOutsideOfPopupCommand = new Command(async () =>
		{
			popupOptions.OnTappingOutsideOfPopup?.Invoke();
			await Close(new PopupResult(true));
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

	public async Task Close(PopupResult result, CancellationToken token = default)
	{
		token.ThrowIfCancellationRequested();

		var popupPageToClose = Navigation.ModalStack.OfType<PopupPage>().LastOrDefault(popupPage => popupPage.Content == Content);

		if (popupPageToClose is null)
		{
			throw new InvalidOperationException($"Unable to close popup: could not locate {nameof(PopupPage)}. If using a custom implementation of {nameof(Popup)}, override the {nameof(Close)} method");
		}

		if (Navigation.ModalStack[^1] is Microsoft.Maui.Controls.Page currentVisibleModalPage
			&& currentVisibleModalPage != popupPageToClose)
		{
			throw new InvalidOperationException($"Unable to close Popup because it is blocked by the Modal Page {currentVisibleModalPage.GetType().FullName}. Please call `{nameof(Navigation)}.{nameof(Navigation.PopModalAsync)}()` to first remove {currentVisibleModalPage.GetType().FullName} from the {nameof(Navigation.ModalStack)}");
		}

		await Navigation.PopModalAsync(false).WaitAsync(token);

		popupClosedEventManager.HandleEvent(this, result, nameof(PopupClosed));
	}

	// Prevent the Android Back Button from dismissing the Popup if CanBeDismissedByTappingOutsideOfPopup is true
	protected override bool OnBackButtonPressed()
	{
		if (popupOptions.CanBeDismissedByTappingOutsideOfPopup)
		{
			return base.OnBackButtonPressed();
		}

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
		popup.SetBinding(BackgroundColorProperty, static (View view) => view.BackgroundColor, source: view, mode: BindingMode.OneWay);
		popup.SetBinding(Popup.MarginProperty, static (View view) => view.Margin, source: view, mode: BindingMode.OneWay);
		popup.SetBinding(Popup.BackgroundProperty, static (View view) => view.Background, source: view, mode: BindingMode.OneWay);
		popup.SetBinding(Popup.BackgroundColorProperty, static (View view) => view.BackgroundColor, source: view, mode: BindingMode.OneWay);
		popup.SetBinding(Popup.VerticalOptionsProperty, static (View view) => view.VerticalOptions, source: view, mode: BindingMode.OneWay);
		popup.SetBinding(Popup.HorizontalOptionsProperty, static (View view) => view.HorizontalOptions, source: view, mode: BindingMode.OneWay);

		if (view is IPaddingElement paddingElement)
		{
			popup.SetBinding(Popup.PaddingProperty, static (IPaddingElement paddingElement) => paddingElement.Padding, source: paddingElement, mode: BindingMode.OneWay);
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

	internal sealed partial class PopupPageLayout : Grid
	{
		public PopupPageLayout(in Popup popupContent, in IPopupOptions options)
		{
			Background = BackgroundColor = null;

			var border = new Border
			{
				BackgroundColor = popupContent.BackgroundColor ??= PopupDefaults.BackgroundColor,
				Content = popupContent
			};
			
			// Bind `Popup` values through to Border using OneWay Bindings 
			border.SetBinding(Border.MarginProperty, static (Popup popup) => popup.Margin, source: popupContent, mode: BindingMode.OneWay);
			border.SetBinding(Border.PaddingProperty, static (Popup popup) => popup.Padding, source: popupContent, mode: BindingMode.OneWay);
			border.SetBinding(Border.BackgroundProperty, static (Popup popup) => popup.Background, source: popupContent, mode: BindingMode.OneWay);
			border.SetBinding(Border.BackgroundColorProperty, static (Popup popup) => popup.BackgroundColor, source: popupContent, mode: BindingMode.OneWay);
			border.SetBinding(Border.VerticalOptionsProperty, static (Popup popup) => popup.VerticalOptions, source: popupContent, mode: BindingMode.OneWay);
			border.SetBinding(Border.HorizontalOptionsProperty, static (Popup popup) => popup.HorizontalOptions, source: popupContent, mode: BindingMode.OneWay);
			
			// Bind `PopupOptions` values through to Border using OneWay Bindings
			border.SetBinding(Border.ShadowProperty, static (IPopupOptions options) => options.Shadow, source: options, mode: BindingMode.OneWay);
			border.SetBinding(Border.StrokeShapeProperty, static (IPopupOptions options) => options.Shape, source: options, mode: BindingMode.OneWay);
			border.SetBinding(Border.StrokeProperty, static (IPopupOptions options) => options.Shape, source: options, converter: new BorderStrokeConverter(), mode: BindingMode.OneWay);
			border.SetBinding(Border.StrokeThicknessProperty, static (IPopupOptions options) => options.Shape, source: options, converter: new BorderStrokeThicknessConverter(), mode: BindingMode.OneWay);

			Children.Add(border);
		}

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
}