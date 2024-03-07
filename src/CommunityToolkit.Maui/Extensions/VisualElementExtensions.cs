namespace CommunityToolkit.Maui.Extensions;

static class VisualElementExtensions
{
	public static void AbortAnimations(this VisualElement element, params string[] otherAnimationNames)
	{
		ArgumentNullException.ThrowIfNull(element);
		ArgumentNullException.ThrowIfNull(otherAnimationNames);

		element.CancelAnimations();
		element.AbortAnimation(nameof(ColorAnimationExtensions.BackgroundColorTo));

		foreach (var name in otherAnimationNames)
		{
			element.AbortAnimation(name);
		}
	}
}