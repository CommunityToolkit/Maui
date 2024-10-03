using System.Diagnostics.CodeAnalysis;
namespace CommunityToolkit.Maui.Extensions;

static class ElementExtensions
{
	public static bool TryFindParent<T>(this Element? child, [NotNullWhen(true)] out T? parent) where T : VisualElement
	{
		while (child is not null)
		{
			if (child.Parent is T element)
			{
				parent = element;
				return true;
			}

			child = child.Parent;
		}

		parent = null;
		return false;
	}
}