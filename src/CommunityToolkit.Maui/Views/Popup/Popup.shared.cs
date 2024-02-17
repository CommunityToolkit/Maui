using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Controls.StyleSheets;
using LayoutAlignment = Microsoft.Maui.Primitives.LayoutAlignment;
using Style = Microsoft.Maui.Controls.Style;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// Represents a small View that pops up at front the Page. Implements <see cref="IPopup"/>.
/// </summary>
[ContentProperty(nameof(Content))]
public partial class Popup : Element, IPopup, IWindowController, IPropertyPropagationController, IResourcesProvider, IStyleSelectable, IStyleElement
{
	/// <summary>
	///  Backing BindableProperty for the <see cref="Content"/> property.
	/// </summary>
	public static readonly BindableProperty ContentProperty = BindableProperty.Create(nameof(Content), typeof(View), typeof(Popup), propertyChanged: OnContentChanged);

	/// <summary>
	///  Backing BindableProperty for the <see cref="Color"/> property.
	/// </summary>
	public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(Color), typeof(Popup), Colors.LightGray, propertyChanged: OnColorChanged);

	/// <summary>
	///  Backing BindableProperty for the <see cref="Size"/> property.
	/// </summary>
	public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(Size), typeof(Popup), default(Size));

	/// <summary>
	///  Backing BindableProperty for the <see cref="CanBeDismissedByTappingOutsideOfPopup"/> property.
	/// </summary>
	public static readonly BindableProperty CanBeDismissedByTappingOutsideOfPopupProperty = BindableProperty.Create(nameof(CanBeDismissedByTappingOutsideOfPopup), typeof(bool), typeof(Popup), true);

	/// <summary>
	///  Backing BindableProperty for the <see cref="VerticalOptions"/> property.
	/// </summary>
	public static readonly BindableProperty VerticalOptionsProperty = BindableProperty.Create(nameof(VerticalOptions), typeof(LayoutAlignment), typeof(Popup), LayoutAlignment.Center);

	/// <summary>
	///  Backing BindableProperty for the <see cref="HorizontalOptions"/> property.
	/// </summary>
	public static readonly BindableProperty HorizontalOptionsProperty = BindableProperty.Create(nameof(HorizontalOptions), typeof(LayoutAlignment), typeof(Popup), LayoutAlignment.Center);

	/// <summary>
	///  Backing BindableProperty for the <see cref="Style" /> property. 
	/// </summary>
	public static readonly BindableProperty StyleProperty = BindableProperty.Create(nameof(Style), typeof(Style), typeof(Popup), default(Style), propertyChanged: (bindable, oldValue, newValue) => ((Popup)bindable).mergedStyle.Style = (Style)newValue);

	readonly WeakEventManager dismissWeakEventManager = new();
	readonly WeakEventManager openedWeakEventManager = new();
	readonly Lazy<PlatformConfigurationRegistry<Popup>> platformConfigurationRegistry;
	readonly MergedStyle mergedStyle;

	TaskCompletionSource popupDismissedTaskCompletionSource = new();
	TaskCompletionSource<object?> resultTaskCompletionSource = new();
	Window window;
	ResourceDictionary resources = new();

	/// <summary>
	/// Instantiates a new instance of <see cref="Popup"/>.
	/// </summary>
	public Popup()
	{
		platformConfigurationRegistry = new Lazy<PlatformConfigurationRegistry<Popup>>(() => new(this));
		((IResourceDictionary)resources).ValuesChanged += OnResourcesChanged;

		VerticalOptions = HorizontalOptions = LayoutAlignment.Center;
		window = Window;
		mergedStyle = new MergedStyle(GetType(), this);
	}

	/// <summary>
	/// Dismissed event is invoked when the popup is closed.
	/// </summary>
	public event EventHandler<PopupClosedEventArgs> Closed
	{
		add => dismissWeakEventManager.AddEventHandler(value);
		remove => dismissWeakEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Opened event is invoked when the popup is opened.
	/// </summary>
	public event EventHandler<PopupOpenedEventArgs> Opened
	{
		add => openedWeakEventManager.AddEventHandler(value);
		remove => openedWeakEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Gets the final result of the dismissed popup.
	/// </summary>
	public Task<object?> Result => resultTaskCompletionSource.Task;

	/// <summary>
	/// Gets or sets the <see cref="View"/> content to render in the Popup.
	/// </summary>
	/// <remarks>
	/// The View can be or type: <see cref="View"/>, <see cref="ContentPage"/> or <see cref="NavigationPage"/>
	/// </remarks>
	public virtual View? Content
	{
		get => (View?)GetValue(ContentProperty);
		set => SetValue(ContentProperty, value);
	}

	/// <summary>
	/// Gets or sets the <see cref="Color"/> of the Popup.
	/// </summary>
	/// <remarks>
	/// This color sets the native background color of the <see cref="Popup"/>, which is
	/// independent of any background color configured in the actual View.
	/// </remarks>
	public Color Color
	{
		get => (Color)GetValue(ColorProperty);
		set => SetValue(ColorProperty, value);
	}

	/// <summary>
	/// Gets or sets the <see cref="LayoutOptions"/> for positioning the <see cref="Popup"/> vertically on the screen.
	/// </summary>
	public LayoutAlignment VerticalOptions
	{
		get => (LayoutAlignment)GetValue(VerticalOptionsProperty);
		set => SetValue(VerticalOptionsProperty, value);
	}

	/// <summary>
	/// Gets or sets the <see cref="LayoutOptions"/> for positioning the <see cref="Popup"/> horizontally on the screen.
	/// </summary>
	public LayoutAlignment HorizontalOptions
	{
		get => (LayoutAlignment)GetValue(HorizontalOptionsProperty);
		set => SetValue(HorizontalOptionsProperty, value);
	}

	/// <summary>
	/// Gets or sets the <see cref="Size"/> of the Popup Display.
	/// </summary>
	/// <remarks>
	/// The Popup will always try to constrain the actual size of the <see cref="Popup" />
	/// to the <see cref="Popup" /> of the View unless a <see cref="Size"/> is specified.
	/// If the <see cref="Popup" /> contains <see cref="LayoutOptions"/> a <see cref="Size"/>
	/// will be required. This will allow the View to have a concept of <see cref="Size"/>
	/// that varies from the actual <see cref="Size"/> of the <see cref="Popup" />
	/// </remarks>
	public Size Size
	{
		get => (Size)GetValue(SizeProperty);
		set => SetValue(SizeProperty, value);
	}

	/// <summary>
	/// Gets or sets a value indicating whether the popup can be dismissed by tapping outside of the Popup.
	/// </summary>
	/// <remarks>
	/// When true and the user taps outside of the popup it will dismiss.
	/// On Android - when false the hardware back button is disabled.
	/// </remarks>
	public bool CanBeDismissedByTappingOutsideOfPopup
	{
		get => (bool)GetValue(CanBeDismissedByTappingOutsideOfPopupProperty);
		set => SetValue(CanBeDismissedByTappingOutsideOfPopupProperty, value);
	}

	/// <summary>
	/// Gets or sets the <see cref="Style"/> of the Popup.
	/// </summary>
	public Style Style
	{
		get => (Style)GetValue(StyleProperty);
		set => SetValue(StyleProperty, value);
	}

	/// <summary>
	/// Gets or sets the <see cref="View"/> anchor.
	/// </summary>
	/// <remarks>
	/// The Anchor is where the Popup will render closest to. When an Anchor is configured
	/// the popup will appear centered over that control or as close as possible.
	/// </remarks>
	public View? Anchor { get; set; }

	/// <summary>
	/// Property that represents the Window that's showing the Popup.
	/// </summary>
	public Window Window
	{
		get => window;
		set
		{
			window = value;

			if (Content is IWindowController controller)
			{
				controller.Window = value;
			}
		}
	}

	/// <summary>
	/// Property that represent Resources of Popup.
	/// </summary>
	public ResourceDictionary Resources
	{
		get => resources;
		set
		{
			ArgumentNullException.ThrowIfNull(value);

			if (resources == value)
			{
				return;
			}

			OnPropertyChanging();
			((IResourceDictionary)resources).ValuesChanged -= OnResourcesChanged;

			resources = value;

			OnResourcesChanged(value);
			((IResourceDictionary)resources).ValuesChanged += OnResourcesChanged;

			OnPropertyChanged();
		}
	}

	/// <summary>
	/// Property that represent Style Class of Popup.
	/// </summary>
	[System.ComponentModel.TypeConverter(typeof(ListStringTypeConverter))]
	public IList<string> StyleClass
	{
		get => mergedStyle.StyleClass;
		set => mergedStyle.StyleClass = value;
	}

	/// <summary>
	/// Gets or sets the result that will return when user taps outside of the Popup.
	/// </summary>
	protected object? ResultWhenUserTapsOutsideOfPopup { get; set; }

	/// <inheritdoc/>
	IView? IPopup.Anchor => Anchor;

	/// <inheritdoc/>
	IView? IPopup.Content => Content;

	/// <inheritdoc/>
	TaskCompletionSource IAsynchronousHandler.HandlerCompleteTCS => popupDismissedTaskCompletionSource;

	/// <inheritdoc/>
	bool IResourcesProvider.IsResourcesCreated => resources is not null;

	/// <summary>
	/// Resets the Popup.
	/// </summary>
	public void Reset()
	{
		resultTaskCompletionSource = new();
		popupDismissedTaskCompletionSource = new();
	}

	/// <summary>
	/// Close the current popup.
	/// </summary>
	/// <remarks>
	/// <see cref="Close(object?)"/> is an <see langword="async"/> <see langword="void"/> method, commonly referred to as a fire-and-forget method.
	/// It will complete and return to the calling thread before the operating system has dismissed the <see cref="Popup"/> from the screen.
	/// If you need to pause the execution of your method until the operating system has dismissed the <see cref="Popup"/> from the screen, use instead <see cref="CloseAsync(object?, CancellationToken)"/>.
	/// </remarks>
	/// <param name="result">The result to return.</param>
	public async void Close(object? result = null) => await CloseAsync(result, CancellationToken.None);

	/// <summary>
	/// Close the current popup.
	/// </summary>
	/// <remarks>
	/// Returns once the operating system has dismissed the <see cref="IPopup"/> from the page
	/// </remarks>
	/// <param name="result">The result to return.</param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	public async Task CloseAsync(object? result = null, CancellationToken token = default)
	{
		await OnClosed(result, false, token);
		resultTaskCompletionSource.TrySetResult(result);
	}

	internal override void OnParentResourcesChanged(IEnumerable<KeyValuePair<string, object>> values)
	{
		if (values is null)
		{
			return;
		}

		if (!((IResourcesProvider)this).IsResourcesCreated || Resources.Count == 0)
		{
			base.OnParentResourcesChanged(values);
			return;
		}

		var innerKeys = new HashSet<string>(StringComparer.Ordinal);
		var changedResources = new List<KeyValuePair<string, object>>();
		foreach (var keyValuePair in Resources)
		{
			innerKeys.Add(keyValuePair.Key);
		}

		foreach (var keyValuePair in values)
		{
			if (innerKeys.Add(keyValuePair.Key))
			{
				changedResources.Add(keyValuePair);
			}
			else if (keyValuePair.Key.StartsWith(Style.StyleClassPrefix, StringComparison.Ordinal))
			{
				if (Resources[keyValuePair.Key] is List<Style> innerStyle)
				{
					var mergedClassStyles = new List<Style>(innerStyle);
					if (keyValuePair.Value is List<Style> parentStyle)
					{
						mergedClassStyles.AddRange(parentStyle);
					}
					changedResources.Add(new KeyValuePair<string, object>(keyValuePair.Key, mergedClassStyles));
				}
			}
		}
		if (changedResources.Count != 0)
		{
			OnResourcesChanged(changedResources);
		}
	}

	/// <summary>
	/// Invokes the <see cref="Opened"/> event.
	/// </summary>
	internal virtual void OnOpened() =>
		openedWeakEventManager.HandleEvent(this, PopupOpenedEventArgs.Empty, nameof(Opened));

	/// <summary>
	/// Invokes the <see cref="Closed"/> event.
	/// </summary>
	/// <param name="result">Sets the <see cref="PopupClosedEventArgs"/> Property of <see cref="PopupClosedEventArgs.Result"/>.</param>
	/// <param name="wasDismissedByTappingOutsideOfPopup">Sets the <see cref="PopupClosedEventArgs"/> Property of <see cref="PopupClosedEventArgs.WasDismissedByTappingOutsideOfPopup"/>/>.</param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	protected virtual async Task OnClosed(object? result, bool wasDismissedByTappingOutsideOfPopup, CancellationToken token = default)
	{
		((IPopup)this).OnClosed(result);
		((IResourceDictionary)resources).ValuesChanged -= OnResourcesChanged;

		RemoveBinding(Popup.ContentProperty);
		RemoveBinding(Popup.ColorProperty);
		RemoveBinding(Popup.SizeProperty);
		RemoveBinding(Popup.CanBeDismissedByTappingOutsideOfPopupProperty);
		RemoveBinding(Popup.VerticalOptionsProperty);
		RemoveBinding(Popup.HorizontalOptionsProperty);
		RemoveBinding(Popup.StyleProperty);

		await popupDismissedTaskCompletionSource.Task.WaitAsync(token);

		Parent?.RemoveLogicalChild(this);

		dismissWeakEventManager.HandleEvent(this, new PopupClosedEventArgs(result, wasDismissedByTappingOutsideOfPopup), nameof(Closed));
	}

	/// <summary>
	/// Invoked when the popup is dismissed by tapping outside of the popup.
	/// </summary>
	protected internal virtual async Task OnDismissedByTappingOutsideOfPopup(CancellationToken token = default)
	{
		await OnClosed(ResultWhenUserTapsOutsideOfPopup, true, token);
		resultTaskCompletionSource.TrySetResult(ResultWhenUserTapsOutsideOfPopup);
	}

	/// <summary>
	///<inheritdoc/>
	/// </summary>
	protected override void OnBindingContextChanged()
	{
		base.OnBindingContextChanged();

		if (Content is not null)
		{
			SetInheritedBindingContext(Content, BindingContext);
			Content.Parent = this;
		}
	}

	static void OnContentChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var popup = (Popup)bindable;
		popup.OnBindingContextChanged();
	}

	static void OnColorChanged(BindableObject bindable, object oldValue, object newValue)
	{
		ArgumentNullException.ThrowIfNull(newValue);
	}

	void IPopup.OnClosed(object? result) => Handler?.Invoke(nameof(IPopup.OnClosed), result);

	void IPopup.OnOpened() => OnOpened();

	async void IPopup.OnDismissedByTappingOutsideOfPopup() => await OnDismissedByTappingOutsideOfPopup(CancellationToken.None);

	void IPropertyPropagationController.PropagatePropertyChanged(string propertyName) =>
		PropertyPropagationExtensions.PropagatePropertyChanged(propertyName, this, ((IVisualTreeElement)this).GetVisualChildren());

	IReadOnlyList<IVisualTreeElement> IVisualTreeElement.GetVisualChildren() => Content is null ? [] : [Content];
}