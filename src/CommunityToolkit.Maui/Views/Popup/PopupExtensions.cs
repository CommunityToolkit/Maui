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
	/// <returns></returns>
	public static async Task ShowPopup<T>(this INavigation navigation, Popup popup, PopupOptions<T> options)
		where T:Popup
	{
		TaskCompletionSource<PopupResult> taskCompletionSource = new();

		var view = new Grid()
		{
			new Border()
			{
				Content = popup
			}
		};
		var popupContainer = new PopupContainer(popup, taskCompletionSource)
		{
			BackgroundColor = options.BackgroundColor ?? Color.FromRgba(0, 0, 0, 0.4), // https://rgbacolorpicker.com/rgba-to-hex,
			CanBeDismissedByTappingOutsideOfPopup = options.CanBeDismissedByTappingOutsideOfPopup,
			Content = view,
			BindingContext = popup.BindingContext
		};

		view.BindingContext = popup.BindingContext;

		if (options.CanBeDismissedByTappingOutsideOfPopup)
		{
			view.GestureRecognizers.Add(new TapGestureRecognizer()
			{
				Command = new Command(async () =>
				{
					options.OnTappingOutsideOfPopup?.Invoke();
					await popupContainer.Close(new PopupResult(true));
				})
			});
		}

		await navigation.PushModalAsync(popupContainer);
		await taskCompletionSource.Task;
	}
}