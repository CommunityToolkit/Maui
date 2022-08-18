using System.Windows.Input;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Views;

/// <inheritdoc cref="IExpander"/>
[ContentProperty(nameof(Content))]
public class Expander : StackLayout, IExpander
{
	readonly IGestureRecognizer tapGestureRecognizer;

	readonly WeakEventManager tappedEventManager = new();

	/// <summary>
	/// Initialize a new instance of <see cref="Expander"/>.
	/// </summary>
	public Expander()
	{
		tapGestureRecognizer = new TapGestureRecognizer()
		{
			Command = new Command(() =>
			{
				IsExpanded = !IsExpanded;
				((IExpander)this).ExpandedChanged(IsExpanded);
			})
		};
	}

	/// <inheritdoc cref="IExpander.ExpandedChanged"/>
	public event EventHandler<ExpandedChangedEventArgs> ExpandedChanged
	{
		add => tappedEventManager.AddEventHandler(value);
		remove => tappedEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Backing BindableProperty for the <see cref="Header"/> property.
	/// </summary>
	public static readonly BindableProperty HeaderProperty
		= BindableProperty.Create(nameof(Header), typeof(IView), typeof(Expander), propertyChanged: OnHeaderPropertyChanged);
	
	/// <summary>
	/// Backing BindableProperty for the <see cref="Content"/> property.
	/// </summary>
	public static readonly BindableProperty ContentProperty
		= BindableProperty.Create(nameof(Content), typeof(IView), typeof(Expander), propertyChanged: OnContentPropertyChanged);

	/// <summary>
	/// Backing BindableProperty for the <see cref="IsExpanded"/> property.
	/// </summary>
	public static readonly BindableProperty IsExpandedProperty
		= BindableProperty.Create(nameof(IsExpanded), typeof(bool), typeof(Expander), default(bool), BindingMode.TwoWay, propertyChanged: OnIsExpandedPropertyChanged);

	/// <summary>
	/// Backing BindableProperty for the <see cref="Direction"/> property.
	/// </summary>
	public static readonly BindableProperty DirectionProperty
		= BindableProperty.Create(nameof(Direction), typeof(ExpandDirection), typeof(Expander), default(ExpandDirection), propertyChanged: OnDirectionPropertyChanged);

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

	/// <inheritdoc />
	public IView? Header
	{
		get => (IView?)GetValue(HeaderProperty);
		set => SetValue(HeaderProperty, value);
	}

	/// <inheritdoc />
	public IView? Content
	{
		get => (IView?)GetValue(ContentProperty);
		set => SetValue(ContentProperty, value);
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
		set => SetValue(DirectionProperty, value);
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

	static void OnHeaderPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		=> ((Expander)bindable).Configure();

	static void OnContentPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		=> ((Expander)bindable).Configure();

	static void OnIsExpandedPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		=> ((Expander)bindable).Configure();

	static void OnDirectionPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		=> ((Expander)bindable).Configure();

	void IExpander.ExpandedChanged(bool isExpanded)
	{
		if (Command is not null && Command.CanExecute(CommandParameter))
		{
			Command.Execute(CommandParameter);
		}

		tappedEventManager.HandleEvent(this, new Core.ExpandedChangedEventArgs(isExpanded), nameof(ExpandedChanged));
	}

	void Configure()
	{
		if (Header is null)
		{
			return;
		}

		SetGestures();
		Layout();
		UpdateSize();
	}

	void Layout()
	{
		Children.Clear();
		switch (Direction)
		{
			case ExpandDirection.Down:
				Orientation = StackOrientation.Vertical;
				Children.Add(Header);
				if (IsExpanded && Content is not null)
				{
					Children.Add(Content);
				}
				break;
			case ExpandDirection.Up:
				Orientation = StackOrientation.Vertical;
				if (IsExpanded && Content is not null)
				{
					Children.Add(Content);
				}

				Children.Add(Header);
				break;
			case ExpandDirection.Left:
				Orientation = StackOrientation.Horizontal;
				if (IsExpanded && Content is not null)
				{
					Children.Add(Content);
				}

				Children.Add(Header);
				break;
			case ExpandDirection.Right:
				Orientation = StackOrientation.Horizontal;
				Children.Add(Header);
				if (IsExpanded && Content is not null)
				{
					Children.Add(Content);
				}

				break;
		}
	}

	void UpdateSize()
	{
		var parent = Parent;
		while (parent is not null)
		{
			switch (parent)
			{
				case Cell cell:
					cell.ForceUpdateSize();
					break;
				case CollectionView collectionView:
					collectionView.InvalidateMeasureInternal(Microsoft.Maui.Controls.Internals.InvalidationTrigger.MeasureChanged);
					break;
			}

			parent = parent.Parent;
		}
	}

	void SetGestures()
	{
		var header = Header as View;
		header?.GestureRecognizers.Remove(tapGestureRecognizer);
		header?.GestureRecognizers.Add(tapGestureRecognizer);
	}
}