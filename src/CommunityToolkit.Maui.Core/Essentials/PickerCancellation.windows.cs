#if NET11_0_OR_GREATER
using System.Runtime.InteropServices;
using Windows.Foundation;

namespace CommunityToolkit.Maui.Storage;

// The Windows App SDK pickers check their token only before they create their dialog and after the dialog closes (FileSavePicker.cpp and
// FolderPicker.cpp of microsoft/WindowsAppSDK 2.x), so IAsyncInfo.Cancel() alone leaves the dialog open until the person closes it.
// This closes the dialog as its title bar's X does.
static partial class PickerCancellation
{
	const string commonDialogClass = "#32770";
	const uint gwOwner = 4;
	const uint wmClose = 0x0010;

	// A cancel can arrive after the picker checked its token but before its dialog is on screen, so the dialog is looked for until the pick ends.
	static readonly TimeSpan dialogPollInterval = TimeSpan.FromMilliseconds(50);

	internal static async Task<TResult> PickAsync<TResult>(IntPtr ownerWindow, Func<IAsyncOperation<TResult>> startPick, CancellationToken cancellationToken)
	{
		// A dialog that the app had already opened on the window, such as a message box, is not the picker's.
		var dialogsBeforePick = FindDialogs(ownerWindow);

		var operation = startPick();
		var pick = operation.AsTask();

		await using (cancellationToken.Register(() =>
		{
			// Cancel() still matters: before the picker creates its dialog it then shows none, and after the dialog closes it discards a pick
			// that raced the cancel.
			operation.Cancel();
			_ = CloseNewDialogsUntilPickEndsAsync(ownerWindow, dialogsBeforePick, pick);
		}))
		{
			return await pick;
		}
	}

	static async Task CloseNewDialogsUntilPickEndsAsync(IntPtr ownerWindow, HashSet<IntPtr> dialogsBeforePick, Task pick)
	{
		// Nobody awaits this loop, and an exception from it would surface on the finalizer thread; a dialog that could not be closed
		// leaves the pick pending, which the person ends by closing the dialog, exactly as before this loop existed.
		try
		{
			while (!pick.IsCompleted)
			{
				foreach (var dialog in FindDialogs(ownerWindow))
				{
					if (!dialogsBeforePick.Contains(dialog) && IsWindowVisible(dialog))
					{
						// Posted, not sent: the dialog runs its modal loop on the picker's background thread, and a send would hold the canceling
						// thread, often the UI thread, until that loop answers. Posted on every poll because a save dialog behind its
						// "Confirm Save As" prompt may not close until the prompt is answered.
						PostMessage(dialog, wmClose, 0, 0);
					}
				}

				await Task.WhenAny(pick, Task.Delay(dialogPollInterval)).ConfigureAwait(false);
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Trace.WriteLine($"Closing the picker dialog failed: {ex}");
		}
	}

	static HashSet<IntPtr> FindDialogs(IntPtr ownerWindow)
	{
		var dialogs = new HashSet<IntPtr>();
		var processId = (uint)Environment.ProcessId;
		var window = IntPtr.Zero;
		while ((window = FindWindowEx(IntPtr.Zero, window, commonDialogClass, null)) != IntPtr.Zero)
		{
			if (GetWindow(window, gwOwner) == ownerWindow
				&& GetWindowThreadProcessId(window, out var windowProcessId) is not 0
				&& windowProcessId == processId)
			{
				dialogs.Add(window);
			}
		}

		return dialogs;
	}

	[LibraryImport("user32.dll", EntryPoint = "FindWindowExW", StringMarshalling = StringMarshalling.Utf16)]
	private static partial IntPtr FindWindowEx(IntPtr parentWindow, IntPtr childAfter, string className, string? windowName);

	[LibraryImport("user32.dll")]
	private static partial IntPtr GetWindow(IntPtr window, uint command);

	[LibraryImport("user32.dll")]
	private static partial uint GetWindowThreadProcessId(IntPtr window, out uint processId);

	[LibraryImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool IsWindowVisible(IntPtr window);

	[LibraryImport("user32.dll", EntryPoint = "PostMessageW")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool PostMessage(IntPtr window, uint message, nint wParam, nint lParam);
}
#endif