using System.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Views;

/// <inheritdoc cref="IExpander"/>
[ContentProperty(nameof(Content))]
public class Expander : ContentView, IExpander
{
	/// <summary>
	/// Backing BindableProperty for the <see cref="Header"/> property.
	/// </summary>
	public static readonly BindableProperty HeaderProperty
		= BindableProperty.Create(nameof(Header), typeof(IView), typeof(Expander), propertyChanged: OnHeaderPropertyChanged);

	/// <summary>
	/// Backing BindableProperty for the <see cref="Content"/> property.
	/// </summary>
	public static new readonly BindableProperty ContentProperty
		= BindableProperty.Create(nameof(Content), typeof(IView), typeof(Expander), propertyChanged: OnContentPropertyChanged);

	/// <summary>
	/// Backing BindableProperty for the <see cref="IsExpanded"/> property.
	/// </summary>
	public static readonly BindableProperty IsExpandedProperty
		= BindableProperty.Create(nameof(IsExpanded), typeof(bool), typeof(Expander), false, BindingMode.TwoWay, propertyChanged: OnIsExpandedPropertyChanged);

	/// <summary>
	/// Backing BindableProperty for the <see cref="Direction"/> property.
	/// </summary>
	public static readonly BindableProperty DirectionProperty
		= BindableProperty.Create(nameof(Direction), typeof(ExpandDirection), typeof(Expander), ExpandDirection.Down, propertyChanged: OnDirectionPropertyChanged);

	/// <summary>
	/// Backing BindableProperty for the <see cref="CommandParameter"/> property.
	/// </summary>
	public static readonly BindableProperty CommandParameterProperty
		= BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(Expander));

	/// <summary>
	/// Backing BindableProperty for the <see cref="Command"/> property.
	/// </summary>
	public static readonly BindableProperty CommandProperty
		= BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(Expander));

	readonly IGestureRecognizer tapGestureRecognizer;
	readonly WeakEventManager tappedEventManager = new();

	/// <summary>
	/// Initialize a new instance of <see cref="Expander"/>.
	/// </summary>
	public Expander()
	{
		tapGestureRecognizer = new TapGestureRecognizer
		{
			Command = new Command(() => IsExpanded = !IsExpanded)
		};

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
	///	Triggered when 
	/// </summary>
	public event EventHandler<ExpandedChangedEventArgs> ExpandedChanged
	{
		add => tappedEventManager.AddEventHandler(value);
		remove => tappedEventManager.RemoveEventHandler(value);
	}

	/// <inheritdoc />
	public IView? Header
	{
		get => (IView?)GetValue(HeaderProperty);
		set => SetValue(HeaderProperty, value);
	}

	/// <inheritdoc />
	public new IView? Content
	{
		get => (IView?)GetValue(Expander.ContentProperty);
		set => SetValue(Expander.ContentProperty, value);
	}

	/// <inheritdoc />
	public bool IsExpanded
	{
		get => (bool)GetValue(IsExpandedProperty);
		set => SetValue(IsExpandedProperty, value);
	}

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

	/// <summary>
	/// Command parameter passed to the <see cref="Command"/>
	/// </summary>
	public object? CommandParameter
	{
		get => GetValue(CommandParameterProperty);
		set => SetValue(CommandParameterProperty, value);
	}

	/// <summary>
	/// Command to execute when <see cref="IsExpanded"/> changed.
	/// </summary>
	public ICommand? Command
	{
		get => (ICommand?)GetValue(CommandProperty);
		set => SetValue(CommandProperty, value);
	}

	Grid ContentGrid => (Grid)base.Content;

	/// <inheritdoc/>
	protected override async void OnParentChanged()
	{
		base.OnParentChanged();

		await EnsureParentIsNotItemsView().ConfigureAwait(false);
	}

	static void OnContentPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var expander = (Expander)bindable;
		if (newValue is View view)
		{
			view.SetBinding(IsVisibleProperty, new Binding(nameof(Expander.IsExpanded), source: bindable));

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

	static void OnIsExpandedPropertyChanged(BindableObject bindable, object oldValue, object newValue) =>
		((IExpander)bindable).ExpandedChanged(((IExpander)bindable).IsExpanded);

	static void OnDirectionPropertyChanged(BindableObject bindable, object oldValue, object newValue) =>
		((Expander)bindable).HandleDirectionChanged((ExpandDirection)newValue);

	// Expander is not currently supported for ListView or CollectionView
	Task EnsureParentIsNotItemsView()
	{
		var parentElement = Parent;

		// The UI Thread is not required for this check
		// Run this on a background thread to avoid performance impact on the UI Thread
		return Task.Run(() =>
		{
			while (parentElement is not null)
			{
				if (parentElement is ListView or ItemsView)
				{
					// Marshall the exception to the UI Thread to ensure Exception is surfaced to the developer, stopping the app
					// Required for MacCatalyst
					Dispatcher.DispatchIfRequired(() => throw new NotSupportedException($"{nameof(Expander)} is not yet supported in {parentElement.GetType().Name}"));
				}

				parentElement = parentElement.Parent;
			}
		});
	}

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
		headerView.GestureRecognizers.Remove(tapGestureRecognizer);
		headerView.GestureRecognizers.Add(tapGestureRecognizer);
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