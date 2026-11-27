using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Sample.Pages.Views.Popup;

static class PopupResultOrExceptionDescriber
{
	public static string Describe(PopupResultOrException resultOrException) => resultOrException switch
	{
		IPopupResult { WasDismissedByTappingOutsideOfPopup: true } => "Dismissed: tapped outside, back button or navigation",
		IPopupResult<string> popupResult => $"Closed with result: {popupResult.Result ?? "null"}",
		IPopupResult => "Popup closed",
		OperationCanceledException => "Canceled",
		Exception exception => $"{exception.GetType().Name}: {exception.Message}"
	};
}