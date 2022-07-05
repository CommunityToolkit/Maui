namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// Platform Expander
/// </summary>
public partial class MauiExpander
{
	bool isExpanded;
	ExpandDirection expandDirection;
	readonly WeakEventManager weakEventManager = new();

	/// <summary>
	/// Animation duration in milliseconds
	/// </summary>
	public long AnimationDuration { get; set; } = 300;

	/// <summary>
	/// Event invoked when IsExpanded changed
	/// </summary>
	public event EventHandler<ExpanderCollapsedEventArgs> Collapsed
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}
	
	/// <summary>
	/// Returns true if expander is expanded
	/// </summary>
	public bool IsExpanded
	{
		get => isExpanded;
		set
		{
			isExpanded = value;
			UpdateContentVisibility(value);
			weakEventManager.HandleEvent(this, new ExpanderCollapsedEventArgs(!value), nameof(Collapsed));
		}
	}

	/// <summary>
	/// Gets or sets Expander expand direction
	/// </summary>
	public ExpandDirection ExpandDirection
	{
		get => expandDirection;
		set
		{
			expandDirection = value;
			Draw();
		}
	}

	partial void Draw();
	partial void UpdateContentVisibility(bool isVisible);
}