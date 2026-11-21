namespace CommunityToolkit.Maui.Animations;

static partial class FlipProjection
{
	public static object? GetCurrent(VisualElement view)
	{
		object? projection = null;
		GetCurrentProjection(view, ref projection);
		return projection;
	}

	public static void RemoveIfStale(VisualElement view, object? projectionAtStart) => RemoveStaleProjection(view, projectionAtStart);

	static partial void GetCurrentProjection(VisualElement view, ref object? projection);

	static partial void RemoveStaleProjection(VisualElement view, object? projectionAtStart);
}