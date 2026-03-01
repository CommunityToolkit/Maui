using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Views;

/// <inheritdoc cref="IExpander"/>
[ContentProperty(nameof(Content))]
[RequiresUnreferencedCode("Calls Microsoft.Maui.Controls.Binding.Binding(String, BindingMode, IValueConverter, Object, String, Object)")]
public partial class Expander : ContentView, IExpander
{
	readonly WeakEventManager expandedChangingEventManager = new();
	readonly WeakEventManager expandedChangedEventManager = new();

	/// <summary>
	/// Initialize a new instance of <see cref="Expander"/>.
	/// </summary>
	public Expander()
	{
		HandleHeaderTapped = ResizeExpanderInItemsView;
		HeaderTapGestureRecognizer.Tapped += OnHeaderTapGestureRecognizerTapped;

		base.Content = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)
			}
		};
	}

	/// <summary>
	/// Triggered when the expander is about to change
	/// </summary>
	public event EventHandler<ExpandedChangingEventArgs> ExpandedChanging
	{
		add => expandedChangingEventManager.AddEventHandler(value);
		remove => expandedChangingEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Triggered when the value of <see cref="IsExpanded"/> changes.
	/// </summary>
	public event EventHandler<ExpandedChangedEventArgs> ExpandedChanged
	{
		add => expandedChangedEventManager.AddEventHandler(value);
		remove => expandedChangedEventManager.RemoveEventHandler(value);
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
	/// Gets or sets the component that performs the expansion and collapse
	/// logic for this expander, including any optional animations.
	/// </summary>
	[BindableProperty]
	public partial IExpansionController ExpansionController { get; set; } = InstantExpansionController.Instance;

	/// <summary>
	/// The Action that fires when <see cref="Header"/> is tapped.
	/// By default, this <see cref="Action"/> runs <see cref="ResizeExpanderInItemsView(TappedEventArgs)"/>.
	/// </summary>
	/// <remarks>
	/// Warning: Overriding this <see cref="Action"/> may cause <see cref="Expander"/> to work improperly when placed inside a <see cref="CollectionView"/> and placed inside a <see cref="ListView"/>.
	/// </remarks>
	public Action<TappedEventArgs>? HandleHeaderTapped { get; set; }

	internal TapGestureRecognizer HeaderTapGestureRecognizer { get; } = new();

	Grid ContentGrid => (Grid)base.Content;

	/// <summary>
	/// Gets the <see cref="ContentView"/> that hosts the expander's content,
	/// which can be used to apply animation or transition effects during expansion and collapse.
	/// </summary>
	public ContentView? ContentHost { get; internal set; }

	static void OnExpandDirectionChanging(BindableObject bindable, object oldValue, object newValue)
	{
		var direction = (Expander)bindable;
		if (newValue is not ExpandDirection enumValue || !Enum.IsDefined(enumValue))
		{
			throw new InvalidEnumArgumentException(nameof(newValue), newValue is int intValue ? intValue : -1, typeof(ExpandDirection));
		}

		direction.Direction = enumValue;
	}

	static void OnContentPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var expander = (Expander)bindable;
		if (newValue is View view)
		{
			if (expander.ContentHost is not null)
			{
				expander.ContentGrid.Remove(expander.ContentHost);
			}
			expander.ContentHost = new ContentView { Content = view, IsClippedToBounds = true, HeightRequest = 0 };
			expander.ContentGrid.Add(expander.ContentHost);
			expander.ContentGrid.SetRow(expander.ContentHost, expander.Direction switch
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
		if (newValue is View view)
		{
			expander.SetHeaderGestures(view);

			expander.ContentGrid.Remove(oldValue);
			expander.ContentGrid.Add(newValue);

			expander.ContentGrid.SetRow(view, expander.Direction switch
			{
				ExpandDirection.Down => 0,
				ExpandDirection.Up => 1,
				_ => throw new NotSupportedException($"{nameof(ExpandDirection)} {expander.Direction} is not yet supported")
			});
		}
	}

	static void OnIsExpandedPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IExpander)bindable).ExpandedChanged(((IExpander)bindable).IsExpanded);
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
		IsExpanded = !IsExpanded;
		HandleHeaderTapped?.Invoke(tappedEventArgs);
	}

	TaskCompletionSource resizeTCS = new();

	void ResizeExpanderInItemsView(TappedEventArgs tappedEventArgs)
	{
		_ = Dispatcher.Dispatch(async () =>
		{
			resizeTCS = new();
			await resizeTCS.Task;
			ResizeExpanderInItemsView2(tappedEventArgs);
		});
	}

	void ResizeExpanderInItemsView2(TappedEventArgs tappedEventArgs)
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
			}
#endif

			element = element.Parent;
		}
	}

	void IExpander.ExpandedChanged(bool isExpanded)
	{
		_ = Dispatcher.Dispatch(async () => await ExpandedChangedAsync(!isExpanded, isExpanded));
	}

	async Task ExpandedChangedAsync(bool wasExpanded, bool isExpanded)
	{
		expandedChangingEventManager.HandleEvent(this, new ExpandedChangingEventArgs(wasExpanded, isExpanded), nameof(ExpandedChanging));

		if (ContentHost is ContentView host && Content is View view)
		{
			if (!wasExpanded && isExpanded)
			{
				view.IsVisible = true;
				await ExpansionController.OnExpandingAsync(this);
				host.HeightRequest = -1;
			}
			else
			{
				await ExpansionController.OnCollapsingAsync(this);
				host.HeightRequest = 0;
				view.IsVisible = false;
			}
		}

		resizeTCS.TrySetResult();

		if (Command?.CanExecute(CommandParameter) is true)
		{
			Command.Execute(CommandParameter);
		}

		expandedChangedEventManager.HandleEvent(this, new ExpandedChangedEventArgs(isExpanded), nameof(ExpandedChanged));
	}
}

class InstantExpansionController : IExpansionController
{
	public static InstantExpansionController Instance { get; } = new();
	public Task OnExpandingAsync(Expander expander) => Task.CompletedTask;
	public Task OnCollapsingAsync(Expander expander) => Task.CompletedTask;
}
