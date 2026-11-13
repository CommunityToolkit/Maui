using System.Diagnostics.CodeAnalysis;
using Microsoft.Maui.Controls;
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

	public static bool TryFindParentPlatformView<T>(this Element? child, [NotNullWhen(true)] out T? parentPlatformView)
	{
		while (child is not null)
		{
			if (child.Parent?.Handler?.PlatformView is T element)
			{
				parentPlatformView = element;
				return true;
			}

			child = child.Parent;
		}

		parentPlatformView = default;
		return false;
	}
}