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

	static readonly Lock windowTurnsLock = new();
	static readonly Dictionary<IntPtr, (SemaphoreSlim Turn, int Picks)> windowTurns = [];

	internal static async Task<TResult> PickAsync<TResult>(IntPtr ownerWindow, Func<IAsyncOperation<TResult>> startPick, CancellationToken cancellationToken)
	{
		// Nothing marks a picker's dialog as its own, so a cancel can only close the dialogs that are new on the window since its pick started.
		// One pick at a time per window keeps another pick's dialog out of that set.
		var turn = EnterWindow(ownerWindow);
		try
		{
			// Not ConfigureAwait(false): the picker remembers the focused window of the thread that starts it and restores that focus after its dialog.
			await turn.WaitAsync(cancellationToken);
			try
			{
				return await PickInTurnAsync(ownerWindow, startPick, cancellationToken);
			}
			finally
			{
				turn.Release();
			}
		}
		finally
		{
			LeaveWindow(ownerWindow);
		}
	}

	static async Task<TResult> PickInTurnAsync<TResult>(IntPtr ownerWindow, Func<IAsyncOperation<TResult>> startPick, CancellationToken cancellationToken)
	{
		// A dialog that the app had already opened on the window, such as a message box, is not the picker's.
		var dialogsBeforePick = FindDialogs(ownerWindow);

		var operation = startPick();
		var pick = operation.AsTask();
		var closing = Task.CompletedTask;

		try
		{
			await using (cancellationToken.Register(() =>
			{
				// Cancel() still matters: before the picker creates its dialog it then shows none, and after the dialog closes it discards a pick
				// that raced the cancel.
				operation.Cancel();
				closing = CloseNewDialogsUntilPickEndsAsync(ownerWindow, dialogsBeforePick, pick);
			}))
			{
				return await pick;
			}
		}
		finally
		{
			// The next pick on this window starts when this one returns, and this pick's loop must not reach that pick's dialog. Disposing the
			// registration waited for a callback that was still running, so closing is final here.
			await closing;
		}
	}

	static async Task CloseNewDialogsUntilPickEndsAsync(IntPtr ownerWindow, HashSet<IntPtr> dialogsBeforePick, Task pick)
	{
		// PickInTurnAsync awaits this loop in a finally, where an exception would replace the pick's own result; a dialog that could not be
		// closed leaves the pick pending, which the person ends by closing the dialog, exactly as before this loop existed.
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

	static SemaphoreSlim EnterWindow(IntPtr ownerWindow)
	{
		lock (windowTurnsLock)
		{
			if (!windowTurns.TryGetValue(ownerWindow, out var entry))
			{
				entry = (new SemaphoreSlim(1, 1), 0);
			}

			windowTurns[ownerWindow] = (entry.Turn, entry.Picks + 1);
			return entry.Turn;
		}
	}

	static void LeaveWindow(IntPtr ownerWindow)
	{
		lock (windowTurnsLock)
		{
			var entry = windowTurns[ownerWindow];
			if (entry.Picks > 1)
			{
				windowTurns[ownerWindow] = (entry.Turn, entry.Picks - 1);
				return;
			}

			// An app can open and close many windows, so a window keeps its entry only while a pick on it runs or waits.
			windowTurns.Remove(ownerWindow);
			entry.Turn.Dispose();
		}
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