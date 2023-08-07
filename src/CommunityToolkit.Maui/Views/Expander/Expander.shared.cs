using System.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Maui.BindablePropertySG;
using CommunityToolkit.Maui.Core;
namespace CommunityToolkit.Maui.Views;

/// <inheritdoc cref="IExpander"/>
[ContentProperty(nameof(Content))]

[BindableProperty<IView>("Header", PropertyChangedMethodName = nameof(OnHeaderPropertyChanged))]
[BindableProperty<IView>("Content", PropertyChangedMethodName = nameof(OnContentPropertyChanged))]
[BindableProperty<bool>("IsExpanded", PropertyChangedMethodName = nameof(OnIsExpandedPropertyChanged))]
[BindableProperty<object>("CommandParameter")]
[BindableProperty<ICommand>("Command")]
public partial class Expander : ContentView, IExpander
{
	/// <summary>
	/// Backing BindableProperty for the <see cref="Direction"/> property.
	/// </summary>
	public static readonly BindableProperty DirectionProperty
		= BindableProperty.Create(nameof(Direction), typeof(ExpandDirection), typeof(Expander), ExpandDirection.Down, propertyChanged: OnDirectionPropertyChanged);
	readonly WeakEventManager tappedEventManager = new();

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
	///	Triggered when the value of <see cref="IsExpanded"/> changes
	/// </summary>
	public event EventHandler<ExpandedChangedEventArgs> ExpandedChanged
	{
		add => tappedEventManager.AddEventHandler(value);
		remove => tappedEventManager.RemoveEventHandler(value);
	}

	internal TapGestureRecognizer HeaderTapGestureRecognizer { get; } = new();

	/// <summary>
	/// The Action that fires when <see cref="Header"/> is tapped.
	/// By default, this <see cref="Action"/> runs <see cref="ResizeExpanderInItemsView(TappedEventArgs)"/>.
	/// </summary>
	/// <remarks>
	/// Warning: Overriding this <see cref="Action"/> may cause <see cref="Expander"/> to work improperly when placed inside of a <see cref="CollectionView"/> and placed inside of a <see cref="ListView"/>.
	/// </remarks>
	public Action<TappedEventArgs>? HandleHeaderTapped { get; set; }

	/// <inheritdoc />
	public ExpandDirection Direction
	{
		get => (ExpandDirection)GetValue(DirectionProperty);
		set
		{
			if (!Enum.IsDefined(typeof(ExpandDirection), value))
			{
				throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(ExpandDirection));
			}

			SetValue(DirectionProperty, value);
		}
	}

	Grid ContentGrid => (Grid)base.Content;

	static void OnContentPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var expander = (Expander)bindable;
		if (newValue is View view)
		{
			view.SetBinding(IsVisibleProperty, new Binding(nameof(IsExpanded), source: bindable));

			expander.ContentGrid.Remove(oldValue);
			expander.ContentGrid.Add(newValue);
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

	void ResizeExpanderInItemsView(TappedEventArgs tappedEventArgs)
	{
		if (Header is null)
		{
			return;
		}

		Element element = this;
		var size = IsExpanded
					? Measure(double.PositiveInfinity, double.PositiveInfinity, MeasureFlags.IncludeMargins).Request
					: Header.Measure(double.PositiveInfinity, double.PositiveInfinity);

		while (element is not null)
		{
			if (element.Parent is ListView && element is Cell cell)
			{
#if IOS || MACCATALYST
				throw new NotSupportedException($"{nameof(Expander)} is not yet supported in {nameof(ListView)}");
#else
				cell.ForceUpdateSize();
#endif
			}
#if IOS || MACCATALYST || WINDOWS
			else if (element is CollectionView collectionView)
			{
				var tapLocation = tappedEventArgs.GetPosition(collectionView);
				ForceUpdateCellSize(collectionView, size, tapLocation);
			}
#endif
#if IOS || MACCATALYST
            else if (element is ScrollView scrollView)
			{
			    ((IView)scrollView).InvalidateMeasure();
			}
#endif

			element = element.Parent;
		}
	}

	void IExpander.ExpandedChanged(bool isExpanded)
	{
		if (Command?.CanExecute(CommandParameter) is true)
		{
			Command.Execute(CommandParameter);
		}

		tappedEventManager.HandleEvent(this, new ExpandedChangedEventArgs(isExpanded), nameof(ExpandedChanged));
	}
}