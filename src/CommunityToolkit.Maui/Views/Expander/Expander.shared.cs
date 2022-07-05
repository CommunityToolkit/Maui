using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// Allows collapse and expand content.
/// </summary>
public class Expander : View, IExpander
{
	readonly WeakEventManager expandedChangedEventManager = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="Expander"/> class.
	/// </summary>
	public Expander()
	{
		Unloaded += OnExpanderUnloaded;
	}

	/// <summary>
	/// Backing BindableProperty for the <see cref="Header"/> property.
	/// </summary>
	public static readonly BindableProperty HeaderProperty = BindableProperty.Create(nameof(Header), typeof(IView), typeof(Expander));

	/// <summary>
	/// Backing BindableProperty for the <see cref="Content"/> property.
	/// </summary>
	public static readonly BindableProperty ContentProperty = BindableProperty.Create(nameof(Content), typeof(IView), typeof(Expander));

	/// <summary>
	/// Backing BindableProperty for the <see cref="IsExpanded"/> property.
	/// </summary>
	public static readonly BindableProperty IsExpandedProperty = BindableProperty.Create(nameof(IsExpanded), typeof(bool), typeof(Expander));

	/// <summary>
	/// Backing BindableProperty for the <see cref="Direction"/> property.
	/// </summary>
	public static readonly BindableProperty DirectionProperty = BindableProperty.Create(nameof(Direction), typeof(ExpandDirection), typeof(Expander));

	/// <summary>
	/// Event occurred when IsExpanded changed.
	/// </summary>
	public event EventHandler<ExpandedChangedEventArgs> ExpandedChanged
	{
		add => expandedChangedEventManager.AddEventHandler(value);
		remove => expandedChangedEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// The <see cref="IView"/> that is used to show header of the <see cref="Expander"/>. This is a bindable property.
	/// </summary>
	public IView Header
	{
		get => (IView)GetValue(HeaderProperty);
		set => SetValue(HeaderProperty, value);
	}

	/// <summary>
	/// The <see cref="IView"/> that is used to show content of the <see cref="Expander"/>. This is a bindable property.
	/// </summary>
	public IView Content
	{
		get => (IView)GetValue(ContentProperty);
		set => SetValue(ContentProperty, value);
	}

	/// <summary>
	/// True if <see cref="Expander"/> is expanded. This is a bindable property.
	/// </summary>
	public bool IsExpanded
	{
		get => (bool)GetValue(IsExpandedProperty);
		set => SetValue(IsExpandedProperty, value);
	}

	/// <summary>
	/// The <see cref="ExpandDirection"/> that is used to define expand direction of the <see cref="Expander"/>. This is a bindable property.
	/// </summary>
	public ExpandDirection Direction
	{
		get => (ExpandDirection)GetValue(DirectionProperty);
		set => SetValue(DirectionProperty, value);
	}

	void OnExpanderUnloaded(object? sender, EventArgs e)
	{
		Unloaded -= OnExpanderUnloaded;
		Handler?.DisconnectHandler();
	}

	/// <inheritdoc />
	protected override void OnBindingContextChanged()
	{
		base.OnBindingContextChanged();
		((View)Header).BindingContext = BindingContext;
		((View)Content).BindingContext = BindingContext;
	}

#if IOS || MACCATALYST
	/// <inheritdoc />
	protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
	{
		var headerSize = Header.Measure(widthConstraint, heightConstraint);
		if (IsExpanded)
		{
			var contentSize = Content.Measure(widthConstraint, heightConstraint);
			return new Size(Math.Max(headerSize.Width, contentSize.Width), headerSize.Height + contentSize.Height);
		}

		return headerSize;
	}
#endif

	void IExpander.ExpandedChanged(bool isExpanded)
	{ 
		expandedChangedEventManager.HandleEvent(this, new ExpandedChangedEventArgs(isExpanded), nameof(ExpandedChanged));
#if IOS || MACCATALYST
		InvalidateMeasure();
#endif
	}
}