using System.Reflection;
using Foundation;
using Microsoft.Maui.ApplicationModel;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// Drives the real <see cref="UIDocumentPickerViewController"/> that FileSaver and FolderPicker present, the way a person would: it
/// finds the picker on screen, answers it through its delegate (the call that UIKit makes when the user picks or cancels) and reads
/// back what the toolkit subscribed to. The picker itself is the system's; the headless runner only has no one to tap it.
/// </summary>
public static class FilesApplePicker
{
	/// <summary>True when the view controller on top of the app is a document picker.</summary>
	public static Task<bool> IsPickerPresentedAsync() => MainThread.InvokeOnMainThreadAsync(() => Platform.GetCurrentUIViewController() is UIDocumentPickerViewController);

	/// <summary>Waits until a document picker is on top and returns it.</summary>
	public static async Task<UIDocumentPickerViewController> WaitForPickerAsync()
	{
		await AssertionExtensions.AssertEventuallyAsync(IsPickerPresentedAsync, timeout: 15000, message: "The document picker was not presented.");
		return await MainThread.InvokeOnMainThreadAsync(() => (UIDocumentPickerViewController)Platform.GetCurrentUIViewController()!);
	}

	/// <summary>Waits until the picker is shown: presented, and its presentation done (UIKit presents a document picker some time after it was asked to).</summary>
	public static Task WaitUntilShownAsync(UIDocumentPickerViewController picker) =>
		AssertionExtensions.AssertEventuallyAsync(() => MainThread.InvokeOnMainThreadAsync(() => picker.PresentingViewController is not null && !picker.IsBeingPresented), timeout: 15000, message: "The document picker was not shown.");

	/// <summary>Waits until a second document picker is shown on top of <paramref name="picker"/> (a call made while it is on top presents its picker from it) and returns it.</summary>
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

	/// <summary>The object that UIKit calls back for the picker (the toolkit's event handlers live on it).</summary>
	public static Task<object> GetDelegateAsync(UIDocumentPickerViewController picker) =>
		MainThread.InvokeOnMainThreadAsync(() => (object)picker.Delegate);

	/// <summary>The user picks these URLs: UIKit dismisses the picker and calls documentPicker:didPickDocumentsAtURLs: on the delegate.</summary>
	public static async Task SimulateUserPicksAsync(UIDocumentPickerViewController picker, params NSUrl[] urls)
	{
		await DismissAsync(picker);
		await MainThread.InvokeOnMainThreadAsync(() => picker.Delegate.DidPickDocument(picker, urls));
	}

	/// <summary>The user cancels: UIKit dismisses the picker and calls documentPickerWasCancelled: on the delegate.</summary>
	public static async Task SimulateUserCancelsAsync(UIDocumentPickerViewController picker)
	{
		await DismissAsync(picker);
		await MainThread.InvokeOnMainThreadAsync(() => picker.Delegate.WasCancelled(picker));
	}

	/// <summary>
	/// How many handlers are subscribed to the picker's DidPickDocumentAtUrls and WasCancelled events. They are stored in two fields of
	/// the delegate class that Microsoft.iOS puts behind the events, so this reads those fields.
	/// </summary>
	public static (int Picked, int Cancelled) CountSubscribers(object pickerDelegate) =>
		(CountHandlers(pickerDelegate, "didPickDocument1"), CountHandlers(pickerDelegate, "wasCancelled"));

	/// <summary>Dismisses every document picker that is still on screen.</summary>
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