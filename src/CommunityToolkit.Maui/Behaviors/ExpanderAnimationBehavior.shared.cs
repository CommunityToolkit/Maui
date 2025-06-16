using System.ComponentModel;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// The <see cref="ExpanderAnimationBehavior"/> is a behavior that animates an <see cref="Expander"/> when it expands or collapses.
/// </summary>
public partial class ExpanderAnimationBehavior : BaseBehavior<Expander>
{
	/// <summary>
	/// Backing BindableProperty for the <see cref="CollapsingLength"/> property.
	/// </summary>
	public static readonly BindableProperty CollapsingLengthProperty =
		BindableProperty.CreateAttached(nameof(CollapsingLength), typeof(uint), typeof(ExpanderAnimationBehavior), 250u);

	/// <summary>
	/// Backing BindableProperty for the <see cref="CollapsingEasing"/> property.
	/// </summary>
	public static readonly BindableProperty CollapsingEasingProperty =
		BindableProperty.CreateAttached(nameof(CollapsingEasing), typeof(Easing), typeof(ExpanderAnimationBehavior), Easing.Linear);

	/// <summary>
	/// Backing BindableProperty for the <see cref="ExpandingLength"/> property.
	/// </summary>
	public static readonly BindableProperty ExpandingLengthProperty =
		BindableProperty.CreateAttached(nameof(ExpandingLength), typeof(uint), typeof(ExpanderAnimationBehavior), 250u);

	/// <summary>
	/// Backing BindableProperty for the <see cref="ExpandingEasing"/> property.
	/// </summary>
	public static readonly BindableProperty ExpandingEasingProperty =
		BindableProperty.CreateAttached(nameof(ExpandingEasing), typeof(Easing), typeof(ExpanderAnimationBehavior), Easing.Linear);

	/// <summary>
	/// Length in milliseconds of the collapse animation when the <see cref="Expander"/> is collapsing.
	/// </summary>
	public uint CollapsingLength
	{
		get => (uint)GetValue(CollapsingLengthProperty);
		set => SetValue(CollapsingLengthProperty, value);
	}

	/// <summary>
	/// Easing of the <see cref="Expander"/> collapsing animation.
	/// </summary>
	public Easing CollapsingEasing
	{
		get => (Easing)GetValue(CollapsingEasingProperty);
		set => SetValue(CollapsingEasingProperty, value);
	}

	/// <summary>
	/// Length in milliseconds of the expand animation when the <see cref="Expander"/> is expanding.
	/// </summary>
	public uint ExpandingLength
	{
		get => (uint)GetValue(ExpandingLengthProperty);
		set => SetValue(ExpandingLengthProperty, value);
	}

	/// <summary>
	/// Easing of the <see cref="Expander"/> expanding animation.
	/// </summary>
	public Easing ExpandingEasing
	{
		get => (Easing)GetValue(ExpandingEasingProperty);
		set => SetValue(ExpandingEasingProperty, value);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	protected override void OnViewPropertyChanged(Expander sender, PropertyChangedEventArgs e)
	{
		base.OnViewPropertyChanged(sender, e);

		switch (e.PropertyName)
		{
			case nameof(Expander.IsExpanded):
				if (sender.IsExpanded)
				{
					AnimateContentHeight(sender, 1.0, sender.BodyContentView.Height, ExpandingLength, ExpandingEasing);
				}
				else
				{
					AnimateContentHeight(sender, sender.BodyContentView.Height, 1.0, ExpandingLength, ExpandingEasing);
				}
				break;
			case nameof(Expander.Header):
			case nameof(Expander.Content):
				sender.InvalidateMeasure();
				break;
		}
	}

	Task<bool> AnimateContentHeight(Expander expander, double fromValue, double toValue, uint length = 250, Easing? easing = null)
	{
		if (easing == null)
		{
			easing = Easing.Linear;
		}
		var tcs = new TaskCompletionSource<bool>();
		expander.ContentHeight = fromValue;
		var animation = new Animation(v => expander.ContentHeight = v, fromValue, toValue, easing);
		animation.Commit(expander, nameof(AnimateContentHeight), 16, length, finished: (f, a) => tcs.SetResult(a));
		return tcs.Task;
	}
}
