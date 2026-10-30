using Android.Views;

namespace CommunityToolkit.Maui.Core.Extensions;

static class AndroidViewGroupExtensions
{
	// Not FindViewWithTag: it searches every descendant, so it would also return an app view that carries the same tag
	public static View? FindChildWithTag(this ViewGroup viewGroup, string tag)
	{
		for (var i = 0; i < viewGroup.ChildCount; i++)
		{
			var child = viewGroup.GetChildAt(i);
			if (child?.Tag?.ToString() == tag)
			{
				return child;
			}
		}

		return null;
	}
}