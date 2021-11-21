using CommunityToolkit.Maui.Controls.Snackbar;
using CommunityToolkit.Maui.UI.Views;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace CommunityToolkit.Maui.Extensions;

/// <summary>
/// Extension methods for <see cref="VisualElement"/>.
/// </summary>
public static class VisualElementExtension
{
	/// <summary>
	/// Display snackbar with the default visual configuration
	/// </summary>
	/// <param name="visualElement">Anchor element</param>
	/// <param name="message">Text of the snackbar</param>
	/// <param name="actionButtonText">Text of the snackbar button</param>
	/// <param name="action">Action of the snackbar button</param>
	/// <param name="duration">Snackbar duration</param>
	/// <returns><see cref="Snackbar"/></returns>
	public static async Task<Snackbar> DisplaySnackBarAsync(this VisualElement? visualElement, string message, string actionButtonText, Action action, TimeSpan? duration = null)
	{
		var snackBar = Snackbar.Make(message, duration, action, visualElement);
		snackBar.ActionButtonText = actionButtonText;
		await snackBar.Show();
		return snackBar;
	}

	internal static bool TryFindParentElementWithParentOfType<T>(this VisualElement? element, out VisualElement? result, out T? parent) where T : VisualElement
	{
		result = null;
		parent = null;

		while (element?.Parent != null)
		{
			if (element.Parent is not T parentElement)
			{
				element = element.Parent as VisualElement;
				continue;
			}

			result = element;
			parent = parentElement;

			return true;
		}

		return false;
	}

	internal static bool TryFindParentOfType<T>(this VisualElement? element, out T? parent) where T : VisualElement
		=> TryFindParentElementWithParentOfType(element, out _, out parent);
}
