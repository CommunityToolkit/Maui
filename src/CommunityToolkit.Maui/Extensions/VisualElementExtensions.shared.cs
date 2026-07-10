namespace CommunityToolkit.Maui.Extensions;

static class VisualElementExtensions
{
	public static void AbortAnimations(this VisualElement element, params ReadOnlySpan<string> otherAnimationNames)
	{
		ArgumentNullException.ThrowIfNull(element);

		element.CancelAnimations();
		element.AbortAnimation(nameof(ColorAnimationExtensions.BackgroundColorTo));

		foreach (var name in otherAnimationNames)
		{
			element.AbortAnimation(name);
		}
	}
}