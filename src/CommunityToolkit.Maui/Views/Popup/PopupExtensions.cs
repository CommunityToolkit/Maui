using CommunityToolkit.Maui.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// 
/// </summary>
public static class PopupExtensions
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="navigation"></param>
	/// <param name="popup"></param>
	/// <param name="options"></param>
	/// <param name="onTappingOutsideOfPopup"></param>
	/// <returns></returns>
	public static async Task ShowPopup(this INavigation navigation, Popup popup, PopupOptions options, Action? onTappingOutsideOfPopup = null)
	{
		TaskCompletionSource<PopupResult> taskCompletionSource = new();

		var view = new Grid()
		{
			new Border()
			{
				Content = popup
			}
		};
		var popupContainer = new PopupContainer
		{
			BackgroundColor = options.BackgroundColor ?? Color.FromRgba(0, 0, 0, 0.4), // https://rgbacolorpicker.com/rgba-to-hex,
			CanBeDismissedByTappingOutsideOfPopup = options.CanBeDismissedByTappingOutsideOfPopup,
			Content = view,
			BindingContext = popup.BindingContext
		};
		popup.SetPopup(popupContainer, options);

		view.BindingContext = popup.BindingContext;

		if (options.CanBeDismissedByTappingOutsideOfPopup)
		{
			view.GestureRecognizers.Add(new TapGestureRecognizer()
			{
				Command = new Command(async () =>
				{
					onTappingOutsideOfPopup?.Invoke();
					taskCompletionSource.SetResult(new PopupResult(true));
					await navigation.PopModalAsync();
				})
			});
		}

		await navigation.PushModalAsync(popupContainer);
		await taskCompletionSource.Task;
	}
}