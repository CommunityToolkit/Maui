using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace CommunityToolkit.Maui.DeviceTests;

// The toolkit's pickers attach their dialog to the process's main window (Process.MainWindowHandle) and fail when there is none, and their
// dialogs are common dialogs of the process.
public abstract partial class FilesTestBase
{
	protected static readonly TimeSpan personTimeout = TimeSpan.FromMinutes(2);

	const int hideWindow = 0;
	const int showWindowWithoutActivating = 4;
	const uint wmClose = 0x0010;
	const uint mbOk = 0;
	const string existingMessageBoxTitle = "files-test-existing";
	const string commonDialogClass = "#32770";

	delegate bool EnumWindowsProc(IntPtr window, IntPtr parameter);

	protected static bool ProcessHasMainWindow()
	{
		using var process = Process.GetCurrentProcess();
		return process.MainWindowHandle != IntPtr.Zero;
	}

	// Hiding the windows makes Process.MainWindowHandle zero, the toolkit's "no window" case. Returns false when Windows still reports a main
	// window, so that the caller skips instead of letting the toolkit show the real dialog.
	protected async Task<bool> HideTheAppWindowsAsync()
	{
		var hidden = await InvokeOnMainThreadAsync(() =>
		{
			var windows = FindTopLevelWindows(static window => IsWindowVisible(window));
			foreach (var window in windows)
			{
				ShowWindow(window, hideWindow);
			}

			return windows;
		});

		AddCleanup(() => InvokeOnMainThreadAsync(() =>
		{
			foreach (var window in hidden)
			{
				ShowWindow(window, showWindowWithoutActivating);
			}
		}));

		return !ProcessHasMainWindow();
	}

	protected static int CountOpenDialogs() =>
		FindTopLevelWindows(static window => IsWindowVisible(window) && ClassName(window) == commonDialogClass).Count;

	protected static bool IsWindowOpen(IntPtr window) => IsWindowVisible(window);

	// An app's own message box on the app's main window, which is open before the picker starts. Owned windows never become Process.MainWindowHandle,
	// so the picker still attaches to the app window. The box needs its own STA thread because MessageBoxW runs a modal loop.
	protected async Task<IntPtr> ShowAMessageBoxOnTheAppWindowAsync()
	{
		var owner = ProcessMainWindow();
		Assert.NotEqual(IntPtr.Zero, owner);
		var thread = new Thread(() => MessageBox(owner, "files-test-existing", existingMessageBoxTitle, mbOk)) { IsBackground = true };
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		AddCleanup(async () =>
		{
			var messageBox = FindWindow(commonDialogClass, existingMessageBoxTitle);
			if (messageBox != IntPtr.Zero)
			{
				// As the title bar's X does: the documented way to end an MB_OK box, which then returns IDOK.
				PostMessage(messageBox, wmClose, IntPtr.Zero, IntPtr.Zero);
			}

			// The cleanups run on the UI thread, which owns the box's owner window: a blocking Join would hold the messages that end the box.
			await AssertionExtensions.AssertEventually(() => !thread.IsAlive, timeout: 5000, message: "The message box did not close.");
		});

		await AssertionExtensions.AssertEventually(() => FindWindow(commonDialogClass, existingMessageBoxTitle) != IntPtr.Zero, timeout: 5000, message: "The message box did not open.");
		return FindWindow(commonDialogClass, existingMessageBoxTitle);
	}

	static IntPtr ProcessMainWindow()
	{
		using var process = Process.GetCurrentProcess();
		return process.MainWindowHandle;
	}

	static async Task CloseOpenDialogsAsync()
	{
		foreach (var dialog in FindTopLevelWindows(static window => IsWindowVisible(window) && ClassName(window) == commonDialogClass))
		{
			PostMessage(dialog, wmClose, IntPtr.Zero, IntPtr.Zero);
		}

		await AssertionExtensions.AssertEventually(() => CountOpenDialogs() is 0, timeout: 5000, message: "A picker dialog is still open after it was asked to close.");
	}

	static List<IntPtr> FindTopLevelWindows(Func<IntPtr, bool> predicate)
	{
		var processId = (uint)Environment.ProcessId;
		var windows = new List<IntPtr>();
		EnumWindows((window, _) =>
		{
			GetWindowThreadProcessId(window, out var owner);
			if (owner == processId && predicate(window))
			{
				windows.Add(window);
			}

			return true;
		}, IntPtr.Zero);

		return windows;
	}

	static string ClassName(IntPtr window)
	{
		var name = new StringBuilder(256);
		return GetClassName(window, name, name.Capacity) > 0 ? name.ToString() : string.Empty;
	}

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);

	[DllImport("user32.dll")]
	static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	static extern bool IsWindowVisible(IntPtr window);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	static extern bool ShowWindow(IntPtr window, int command);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll", EntryPoint = "FindWindowW", CharSet = CharSet.Unicode)]
	static extern IntPtr FindWindow(string className, string windowName);

	[DllImport("user32.dll", EntryPoint = "MessageBoxW", CharSet = CharSet.Unicode)]
	static extern int MessageBox(IntPtr owner, string text, string caption, uint type);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	static extern int GetClassName(IntPtr window, StringBuilder className, int maxCount);
}