using CommunityToolkit.Maui.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Shapes;

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
	public static async Task ShowPopup<T>(this INavigation navigation, T popup, PopupOptions<T> options)
		where T:Popup
	{
		TaskCompletionSource<PopupResult> taskCompletionSource = new();

		var view = new Grid()
		{
			BackgroundColor = null
		};
		view.Children.Add(new Border()
		{
			Content = popup,
			Background = popup.Background,
			BackgroundColor = popup.BackgroundColor,
			WidthRequest = popup.WidthRequest,
			HeightRequest = popup.HeightRequest,
			VerticalOptions = LayoutOptions.Center,
			StrokeShape = new RoundRectangle() { CornerRadius = new CornerRadius(15) },
			Margin = 30,
			Padding = 15
		});
		var popupContainer = new PopupContainer(popup, taskCompletionSource)
		{
			BackgroundColor = options.BackgroundColor ?? Color.FromRgba(0, 0, 0, 0.4), // https://rgbacolorpicker.com/rgba-to-hex,
			CanBeDismissedByTappingOutsideOfPopup = options.CanBeDismissedByTappingOutsideOfPopup,
			Content = view,
			BindingContext = popup.BindingContext
		};

		popupContainer.Appearing += (s, e) => options.OnOpened?.Invoke(popup);
		popupContainer.Disappearing += (s, e) => options.OnClosed?.Invoke(popup);

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