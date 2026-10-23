using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Views;

/// <inheritdoc cref="IExpander"/>
/// <remarks>
/// <para>
/// The <see cref="Expander"/> lays out its <see cref="Header"/> and <see cref="Content"/> in a <see cref="Grid"/> of its own. An implicit
/// <see cref="Style"/> for <see cref="Grid"/>, for example one in App.xaml, does not apply to that <see cref="Grid"/> unless the
/// <see cref="Style"/> sets <see cref="Style.ApplyToDerivedTypes"/>, and even then the <see cref="VisualElement.Background"/> and
/// <see cref="VisualElement.BackgroundColor"/> of that <see cref="Grid"/> stay <see langword="null"/>, so the background of the
/// <see cref="Expander"/> shows behind the <see cref="Header"/> and the <see cref="Content"/>.
/// </para>
/// <para>
/// Keyboard and screen reader users can expand and collapse the <see cref="Expander"/> through its <see cref="Header"/> whenever a tap on the
/// <see cref="Header"/> can: while the <see cref="Header"/> is visible, enabled and not input transparent.
/// On Windows, Tab stops at the <see cref="Expander"/>, Enter and Space toggle it, and UI Automation exposes it as a button with the
/// ExpandCollapse and Invoke patterns, named after the <see cref="Header"/>.
/// On Android, TalkBack announces the <see cref="Header"/> as collapsed or expanded and offers its expand or collapse action.
/// On iOS and Mac Catalyst, VoiceOver reads the whole <see cref="Header"/> as one button that a double tap toggles, and announces it as
/// collapsed or expanded from version 18.
/// </para>
/// </remarks>
[ContentProperty(nameof(Content))]
[RequiresUnreferencedCode("Calls Microsoft.Maui.Controls.Binding.Binding(String, BindingMode, IValueConverter, Object, String, Object)")]
public partial class Expander : ContentView, IExpander
{
	readonly WeakEventManager tappedEventManager = new();

	/// <summary>
	/// Initialize a new instance of <see cref="Expander"/>.
	/// </summary>
	public Expander()
	{
		HandleHeaderTapped = ResizeExpanderInItemsView;
		HeaderTapGestureRecognizer.Tapped += OnHeaderTapGestureRecognizerTapped;

		base.Content = new ExpanderLayout
		{
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)
			}
		};
	}

	/// <summary>
	/// Triggered when the value of <see cref="IsExpanded"/> changes.
	/// </summary>
	public event EventHandler<ExpandedChangedEventArgs> ExpandedChanged
	{
		add => tappedEventManager.AddEventHandler(value);
		remove => tappedEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Gets or sets the direction in which the expander expands.
	/// </summary>
	[BindableProperty(PropertyChangingMethodName = nameof(OnExpandDirectionChanging), PropertyChangedMethodName = nameof(OnDirectionPropertyChanged))]
	public partial ExpandDirection Direction { get; set; } = ExpanderDefaults.Direction;

	/// <summary>
	/// Gets or sets the command to execute when the expander is expanded or collapsed.
	/// </summary>
	[BindableProperty]
	public partial ICommand? Command { get; set; }

	/// <summary>
	/// Gets or sets the parameter to pass to the <see cref="Command"/> property.
	/// </summary>
	[BindableProperty]
	public partial object? CommandParameter { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the expander is expanded.
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnIsExpandedPropertyChanged))]
	public partial bool IsExpanded { get; set; }

	/// <summary>
	/// Gets or sets the content to be expanded or collapsed.
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnContentPropertyChanged))]
	public new partial IView Content { get; set; }

	/// <summary>
	/// Gets or sets the header view of the expander.
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnHeaderPropertyChanged))]
	public partial IView Header { get; set; }

	/// <summary>
	/// The Action that fires when <see cref="Header"/> is tapped, and when the <see cref="Header"/> is activated without a tap: from the keyboard
	/// or by UI Automation on Windows, by the expand or collapse action of TalkBack on Android, and by VoiceOver on iOS and Mac Catalyst.
	/// By default, this <see cref="Action"/> runs <see cref="ResizeExpanderInItemsView(TappedEventArgs)"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// For an activation without a tap, <see cref="TappedEventArgs.Parameter"/> is <see langword="null"/>, and
	/// <see cref="TappedEventArgs.GetPosition(Element)"/> returns the center of the <see cref="Header"/> on Windows and <see langword="null"/>
	/// on the other platforms.
	/// </para>
	/// <para>
	/// Warning: Overriding this <see cref="Action"/> may cause <see cref="Expander"/> to work improperly when placed inside a <see cref="CollectionView"/> and placed inside a <see cref="ListView"/>.
	/// </para>
	/// </remarks>
	public Action<TappedEventArgs>? HandleHeaderTapped { get; set; }

	internal TapGestureRecognizer HeaderTapGestureRecognizer { get; } = new();

	// A header that a tap cannot toggle cannot be toggled from the keyboard or by assistive technology either.
	internal bool IsHeaderActivatable => Header is View { IsVisible: true, IsEnabled: true, InputTransparent: false };

	internal void HandleHeaderTap(TappedEventArgs tappedEventArgs)
	{
		IsExpanded = !IsExpanded;
		HandleHeaderTapped?.Invoke(tappedEventArgs);
	}

	// Goes through the tap, like every activation of the header, so that HandleHeaderTapped resizes the Expander inside a ListView or CollectionView.
	internal void SetIsExpandedFromHeader(bool isExpanded, TappedEventArgs tappedEventArgs)
	{
		if (IsExpanded != isExpanded)
		{
			HandleHeaderTap(tappedEventArgs);
		}
	}

	Grid ContentGrid => (Grid)base.Content;

	void IExpander.ExpandedChanged(bool isExpanded)
	{
		if (Command?.CanExecute(CommandParameter) is true)
		{
			Command.Execute(CommandParameter);
		}

		tappedEventManager.HandleEvent(this, new ExpandedChangedEventArgs(isExpanded), nameof(ExpandedChanged));
	}

	// Validation only: BindableObject stores newValue after this callback. Setting Direction here as well would store it as a local
	// value, which outranks Bindings, Styles and DynamicResources, so they could change Direction only once.
	static void OnExpandDirectionChanging(BindableObject bindable, object oldValue, object newValue)
	{
		if (newValue is not ExpandDirection enumValue || !Enum.IsDefined(enumValue))
		{
			throw new InvalidEnumArgumentException(nameof(newValue), newValue is int intValue ? intValue : -1, typeof(ExpandDirection));
		}
	}

	static void OnContentPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var expander = (Expander)bindable;
		if (newValue is View view)
		{
			view.SetBinding(IsVisibleProperty, new Binding(nameof(IsExpanded), source: expander));

			if (oldValue is IView oldView)
			{
				expander.ContentGrid.Remove(oldView);
			}

			expander.ContentGrid.Add(view);
			expander.ContentGrid.SetRow(view, expander.Direction switch
			{
				ExpandDirection.Down => 1,
				ExpandDirection.Up => 0,
				_ => throw new NotSupportedException($"{nameof(ExpandDirection)} {expander.Direction} is not yet supported")
			});
		}
	}

	static void OnHeaderPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var expander = (Expander)bindable;
		if (oldValue is View oldHeader)
		{
			oldHeader.PropertyChanged -= expander.OnHeaderViewPropertyChanged;
		}

		// Before the header joins the layout, which gives it a handler when the Expander has one: Android waits for the header's handler.
		expander.OnHeaderChanged(oldValue as View, newValue as View);

		if (newValue is View view)
		{
			view.PropertyChanged += expander.OnHeaderViewPropertyChanged;
			expander.SetHeaderGestures(view);

			if (oldValue is IView oldView)
			{
				expander.ContentGrid.Remove(oldView);
			}

			expander.ContentGrid.Add(view);

			expander.ContentGrid.SetRow(view, expander.Direction switch
			{
				ExpandDirection.Down => 0,
				ExpandDirection.Up => 1,
				_ => throw new NotSupportedException($"{nameof(ExpandDirection)} {expander.Direction} is not yet supported")
			});
		}

		expander.UpdateHeaderAccessibility();
	}

	static void OnIsExpandedPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IExpander)bindable).ExpandedChanged(((IExpander)bindable).IsExpanded);
		((Expander)bindable).UpdateHeaderAccessibility();
	}

	static void OnDirectionPropertyChanged(BindableObject bindable, object oldValue, object newValue) =>
		((Expander)bindable).HandleDirectionChanged((ExpandDirection)newValue);

	void HandleDirectionChanged(ExpandDirection expandDirection)
	{
		if (Header is null || Content is null)
		{
			return;
		}

		switch (expandDirection)
		{
			case ExpandDirection.Down:
				ContentGrid.SetRow(Header, 0);
				ContentGrid.SetRow(Content, 1);
				break;

			case ExpandDirection.Up:
				ContentGrid.SetRow(Header, 1);
				ContentGrid.SetRow(Content, 0);
				break;

			default:
				throw new NotSupportedException($"{nameof(ExpandDirection)} {expandDirection} is not yet supported");
		}
	}

	void SetHeaderGestures(in IView header)
	{
		var headerView = (View)header;
		headerView.GestureRecognizers.Remove(HeaderTapGestureRecognizer);
		headerView.GestureRecognizers.Add(HeaderTapGestureRecognizer);
	}

	void OnHeaderTapGestureRecognizerTapped(object? sender, TappedEventArgs tappedEventArgs)
	{
		HandleHeaderTap(tappedEventArgs);
	}

	void OnHeaderViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == IsVisibleProperty.PropertyName
			|| e.PropertyName == IsEnabledProperty.PropertyName
			|| e.PropertyName == InputTransparentProperty.PropertyName)
		{
			UpdateHeaderAccessibility();
		}
	}

	partial void OnHeaderChanged(View? oldHeader, View? newHeader);

	partial void UpdateHeaderAccessibility();

	void ResizeExpanderInItemsView(TappedEventArgs tappedEventArgs)
	{
		if (Header is null)
		{
			return;
		}

		Element element = this;
#if WINDOWS
		var size = IsExpanded
					? Measure(double.PositiveInfinity, double.PositiveInfinity)
					: Header.Measure(double.PositiveInfinity, double.PositiveInfinity);
#endif
		while (element is not null)
		{
#if IOS || MACCATALYST
			if (element is ListView listView)
			{
				(listView.Handler?.PlatformView as UIKit.UITableView)?.ReloadData();
			}
#endif

#if WINDOWS
			if (element.Parent is ListView listView && element is Cell cell)
			{
				cell.ForceUpdateSize();
			}
			else if (element is CollectionView collectionView)
			{
				var tapLocation = tappedEventArgs.GetPosition(collectionView);
				ForceUpdateCellSize(collectionView, size, tapLocation);
			}
#endif

			element = element.Parent;
		}
	}

	// An implicit Style only applies to its exact TargetType unless it sets ApplyToDerivedTypes, so the app's implicit Grid Style does not reach this subclass.
	// The local null Background and BackgroundColor outrank any Style, so this Grid cannot paint over the Background and BackgroundColor of the Expander.
	// CsWinRT requires partial on Windows (CsWinRT1028).
	internal sealed partial class ExpanderLayout : Grid
	{
		public ExpanderLayout()
		{
			Background = BackgroundColor = null;
		}
	}
}