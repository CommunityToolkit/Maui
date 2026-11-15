namespace CommunityToolkit.Maui.Animations;

static partial class FlipProjection
{
	public static void RemoveIfStale(VisualElement view) => RemoveStaleProjection(view);

	static partial void RemoveStaleProjection(VisualElement view);
}