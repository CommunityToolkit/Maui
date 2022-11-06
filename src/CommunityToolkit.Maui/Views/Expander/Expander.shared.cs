using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls;

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

	readonly object updateExpanderLock = new();
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
		=> ((Expander)bindable).UpdateExpander();

	static void OnContentPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		=> ((Expander)bindable).UpdateExpander();

	static void OnIsExpandedPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((Expander)bindable).UpdateExpander();
		((IExpander)bindable).ExpandedChanged(((IExpander)bindable).IsExpanded);
	}

	static void OnDirectionPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		=> ((Expander)bindable).UpdateExpander();

	static Grid CreateGridLayout(in IView expanderHeader, in IView expanderContent, in ExpandDirection expandDirection, in bool isExpanded)
	{
		var grid = new Grid
		{
			expanderHeader
		};

		switch (expandDirection)
		{
			case ExpandDirection.Down:
				grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
				grid.SetRow(expanderHeader, 0);
				if (isExpanded)
				{
					grid.Children.Add(expanderContent);
					grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
					grid.SetRow(expanderContent, 1);
				}
				break;

			case ExpandDirection.Up:
				if (isExpanded)
				{
					grid.Children.Add(expanderContent);
					grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
					grid.SetRow(expanderContent, 0);
					grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
					grid.SetRow(expanderHeader, 1);
				}
				else
				{
					grid.RowDefinitions.Add(new RowDefinition());
					grid.SetRow(expanderHeader, 0);
				}
				break;
		}

		return grid;
	}

	static void SetHeaderGestures(in IView header, in IGestureRecognizer gestureRecognizer)
	{
		var headerView = (View)header;
		headerView.GestureRecognizers.Remove(gestureRecognizer);
		headerView.GestureRecognizers.Add(gestureRecognizer);
	}

	static void ForceUpdateLayoutSizeForItemsView(in Element parent)
	{
		var element = parent;

		while (element is not null)
		{
			if (element is ListView listView)
			{
				foreach (var child in listView.AllChildren.OfType<Cell>())
				{
					child.ForceUpdateSize();
				}

				listView.InvalidateMeasureInternal(Microsoft.Maui.Controls.Internals.InvalidationTrigger.MeasureChanged);
			}
			else if (element is CollectionView collectionView)
			{
				collectionView.InvalidateMeasureInternal(Microsoft.Maui.Controls.Internals.InvalidationTrigger.MeasureChanged);
			}

			element = element.Parent;
		}
	}

	void IExpander.ExpandedChanged(bool isExpanded) =>
		tappedEventManager.HandleEvent(this, new Core.ExpandedChangedEventArgs(isExpanded), nameof(ExpandedChanged));

	void UpdateExpander()
	{
		lock (updateExpanderLock) // Ensure thread safety
		{
			if (Header is null || Content is null)
			{
				return;
			}

			SetHeaderGestures(Header, tapGestureRecognizer);

			base.Content = CreateGridLayout(Header, Content, Direction, IsExpanded);

			ForceUpdateLayoutSizeForItemsView(Parent);
		}
	}
}