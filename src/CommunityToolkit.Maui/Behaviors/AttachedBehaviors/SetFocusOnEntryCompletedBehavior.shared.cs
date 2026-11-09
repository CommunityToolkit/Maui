namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// The <see cref="SetFocusOnEntryCompletedBehavior"/> is an attached property for entries that allows the user to specify what <see cref="VisualElement"/> should gain focus after the user completes that entry.
/// </summary>
[AttachedBindableProperty<VisualElement>("NextElement", PropertyChangedMethodName = nameof(OnNextElementChanged))]
public partial class SetFocusOnEntryCompletedBehavior : BaseBehavior<VisualElement>
{
	static void OnNextElementChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var entry = (Entry)bindable;
		var weakEntry = new WeakReference<Entry>(entry);
		entry.Completed += CompletedHandler;

		void CompletedHandler(object? sender, EventArgs e)
		{
			if (weakEntry.TryGetTarget(out var origEntry))
			{
				GetNextElement(origEntry)?.Focus();
			}
		}
	}
}