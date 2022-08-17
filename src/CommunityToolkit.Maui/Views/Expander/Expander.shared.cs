using System.Windows.Input;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using Microsoft.Maui.Graphics;
using static System.Math;

namespace CommunityToolkit.Maui.Views;

[ContentProperty(nameof(Content))]
public class Expander : Grid, IExpander
{
	IGestureRecognizer tapGestureRecognizer;
	
	const string expandAnimationName = nameof(expandAnimationName);

	const uint defaultAnimationLength = 250;

	readonly WeakEventManager tappedEventManager = new();

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

	public event EventHandler<Core.ExpandedChangedEventArgs> ExpandedChanged
	{
		add => tappedEventManager.AddEventHandler(value);
		remove => tappedEventManager.RemoveEventHandler(value);
	}

	public static readonly BindableProperty HeaderProperty
		= BindableProperty.Create(nameof(Header), typeof(IView), typeof(Expander), propertyChanged: OnHeaderPropertyChanged);

	public static readonly BindableProperty ContentProperty
		= BindableProperty.Create(nameof(Content), typeof(IView), typeof(Expander), propertyChanged: OnContentPropertyChanged);

	public static readonly BindableProperty IsExpandedProperty
		= BindableProperty.Create(nameof(IsExpanded), typeof(bool), typeof(Expander), default(bool), BindingMode.TwoWay, propertyChanged: OnIsExpandedPropertyChanged);

	public static readonly BindableProperty DirectionProperty
		= BindableProperty.Create(nameof(Direction), typeof(ExpandDirection), typeof(Expander), default(ExpandDirection), propertyChanged: OnDirectionPropertyChanged);
	
	public static readonly BindableProperty AnimationLengthProperty
		= BindableProperty.Create(nameof(AnimationLength), typeof(uint), typeof(Expander), defaultAnimationLength);

	public static readonly BindableProperty AnimationEasingProperty
		= BindableProperty.Create(nameof(AnimationEasing), typeof(Easing), typeof(Expander));

	public static readonly BindableProperty CommandParameterProperty
		= BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(Expander));

	public static readonly BindableProperty CommandProperty
		= BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(Expander));

	public IView? Header
	{
		get => (IView?)GetValue(HeaderProperty);
		set => SetValue(HeaderProperty, value);
	}

	public IView? Content
	{
		get => (IView?)GetValue(ContentProperty);
		set => SetValue(ContentProperty, value);
	}

	public bool IsExpanded
	{
		get => (bool)GetValue(IsExpandedProperty);
		set => SetValue(IsExpandedProperty, value);
	}

	public ExpandDirection Direction
	{
		get => (ExpandDirection)GetValue(DirectionProperty);
		set => SetValue(DirectionProperty, value);
	}

	void IExpander.ExpandedChanged(bool isExpanded)
	{
		if (Command is not null && Command.CanExecute(CommandParameter))
		{
			Command.Execute(CommandParameter);
		}

		tappedEventManager.HandleEvent(this, new Core.ExpandedChangedEventArgs(isExpanded), nameof(ExpandedChanged));
	}

	public uint AnimationLength
	{
		get => (uint)GetValue(AnimationLengthProperty);
		set => SetValue(AnimationLengthProperty, value);
	}

	public Easing AnimationEasing
	{
		get => (Easing)GetValue(AnimationEasingProperty);
		set => SetValue(AnimationEasingProperty, value);
	}

	public object? CommandParameter
	{
		get => GetValue(CommandParameterProperty);
		set => SetValue(CommandParameterProperty, value);
	}

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

	void Configure()
	{
		if (Header is null || Content is null)
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
		Children.Add(Header);
		RowDefinitions.Clear();
		ColumnDefinitions.Clear();
		switch (Direction)
		{
			case ExpandDirection.Down:
				RowDefinitions.Add(new RowDefinition(GridLength.Auto));
				SetRow(Header, 0);
				if (IsExpanded)
				{
					Children.Add(Content);
					RowDefinitions.Add(new RowDefinition(GridLength.Auto));
					SetRow(Content, 1);
				}
				break;
			case ExpandDirection.Up:
				if (IsExpanded)
				{
					Children.Add(Content);
					RowDefinitions.Add(new RowDefinition(GridLength.Auto));
					SetRow(Content, 0);
					RowDefinitions.Add(new RowDefinition(GridLength.Auto));
					SetRow(Header, 1);
				}
				else
				{
					RowDefinitions.Add(new RowDefinition());
					SetRow(Header, 0);
				}
				break;
			case ExpandDirection.Left:
				if (IsExpanded)
				{
					Children.Add(Content);
					ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
					SetColumn(Content, 0);
					ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
					SetColumn(Header, 1);
				}
				else
				{
					ColumnDefinitions.Add(new ColumnDefinition());
					SetColumn(Header, 0);
				}
				break;
			case ExpandDirection.Right:
				if (IsExpanded)
				{
					Children.Add(Content);
					ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
					SetColumn(Header, 0);
					ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
					SetColumn(Content, 1);
				}
				else
				{
					ColumnDefinitions.Add(new ColumnDefinition());
					SetColumn(Header, 0);
				}
				break;
		}
	}

	void UpdateSize()
	{ 
		var parent = Parent;
		while (parent is not null)
		{
			if (parent is Cell cell)
			{
				cell.ForceUpdateSize();
			}

			if (parent is CollectionView collectionView)
			{
				collectionView.InvalidateMeasureInternal(Microsoft.Maui.Controls.Internals.InvalidationTrigger.MeasureChanged);
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