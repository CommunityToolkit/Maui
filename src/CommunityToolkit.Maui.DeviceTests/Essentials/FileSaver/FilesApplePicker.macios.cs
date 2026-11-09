using System.Reflection;
using Foundation;
using Microsoft.Maui.ApplicationModel;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

// The picker is the system's real UIDocumentPickerViewController, but nobody taps it in the headless runner, so the tests answer it through
// its delegate, the call that UIKit makes when the user picks or cancels.
public static class FilesApplePicker
{
	public static Task<bool> IsPickerPresentedAsync() => MainThread.InvokeOnMainThreadAsync(() => Platform.GetCurrentUIViewController() is UIDocumentPickerViewController);

	public static async Task<UIDocumentPickerViewController> WaitForPickerAsync()
	{
		await AssertionExtensions.AssertEventuallyAsync(IsPickerPresentedAsync, timeout: 15000, message: "The document picker was not presented.");
		return await MainThread.InvokeOnMainThreadAsync(() => (UIDocumentPickerViewController)Platform.GetCurrentUIViewController()!);
	}

	// UIKit presents a document picker some time after it was asked to, so the picker counts as shown only once its presentation is done.
	public static Task WaitUntilShownAsync(UIDocumentPickerViewController picker) =>
		AssertionExtensions.AssertEventuallyAsync(() => MainThread.InvokeOnMainThreadAsync(() => picker.PresentingViewController is not null && !picker.IsBeingPresented), timeout: 15000, message: "The document picker was not shown.");

	public static async Task<UIDocumentPickerViewController> WaitForPickerOnTopOfAsync(UIDocumentPickerViewController picker)
	{
		UIDocumentPickerViewController? onTop = null;
		await AssertionExtensions.AssertEventuallyAsync(() => MainThread.InvokeOnMainThreadAsync(() =>
		{
			onTop = picker.PresentedViewController as UIDocumentPickerViewController;
			return onTop is { PresentingViewController: not null, IsBeingPresented: false };
		}), timeout: 15000, message: "No second document picker was shown on top of the first one.");

		return onTop!;
	}

	public static Task<object> GetDelegateAsync(UIDocumentPickerViewController picker) =>
		MainThread.InvokeOnMainThreadAsync(() => (object)picker.Delegate);

	// Like UIKit, a pick and a cancel dismiss the picker before they call the delegate.
	public static async Task SimulateUserPicksAsync(UIDocumentPickerViewController picker, params NSUrl[] urls)
	{
		await DismissAsync(picker);
		await MainThread.InvokeOnMainThreadAsync(() => picker.Delegate.DidPickDocument(picker, urls));
	}

	public static async Task SimulateUserCancelsAsync(UIDocumentPickerViewController picker)
	{
		await DismissAsync(picker);
		await MainThread.InvokeOnMainThreadAsync(() => picker.Delegate.WasCancelled(picker));
	}

	// Microsoft.iOS stores the handlers in two fields of the delegate class behind the events, so they are read from those fields.
	public static (int Picked, int Cancelled) CountSubscribers(object pickerDelegate) =>
		(CountHandlers(pickerDelegate, "didPickDocument1"), CountHandlers(pickerDelegate, "wasCancelled"));

	public static async Task DismissAnyPickerAsync()
	{
		for (var attempts = 0; attempts < 5; attempts++)
		{
			UIViewController? top;
			try
			{
				top = Platform.GetCurrentUIViewController();
			}
			catch (NullReferenceException)
			{
				// MAUI throws when no window has a root view controller; there is nothing to dismiss then.
				return;
			}

			if (top is not UIDocumentPickerViewController picker)
			{
				return;
			}

			await picker.DismissViewControllerAsync(false);
		}
	}

	static Task DismissAsync(UIDocumentPickerViewController picker) =>
		MainThread.InvokeOnMainThreadAsync(() => picker.DismissViewControllerAsync(false));

	static int CountHandlers(object pickerDelegate, string field)
	{
		var type = pickerDelegate.GetType();
		var info = type.GetField(field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
			?? throw new InvalidOperationException($"{type.FullName} has no field {field}. Fields: {string.Join(", ", type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Select(declared => declared.Name))}.");

		return (info.GetValue(pickerDelegate) as Delegate)?.GetInvocationList().Length ?? 0;
	}
}