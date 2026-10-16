using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace CommunityToolkit.Maui.DeviceTests;

// Windows part of the base class of the FileSaver, FolderPicker and Badge tests: the toolkit's pickers attach their dialog to the process's
// main window (Process.MainWindowHandle) and fail when there is none, and their dialogs are common dialogs of the process.
public abstract partial class FilesTestBase
{
	/// <summary>How long a test that needs a person (RequiresUserInteraction) waits for the answer.</summary>
	protected static readonly TimeSpan personTimeout = TimeSpan.FromMinutes(2);

	const int hideWindow = 0;
	const int showWindowWithoutActivating = 4;
	const string commonDialogClass = "#32770";

	delegate bool EnumWindowsProc(IntPtr window, IntPtr parameter);

	/// <summary>True while Windows reports a main window for the process, which is what the toolkit's pickers check.</summary>
	protected static bool ProcessHasMainWindow()
	{
		using var process = Process.GetCurrentProcess();
		return process.MainWindowHandle != IntPtr.Zero;
	}

	/// <summary>
	/// Hides every visible top-level window of the process until the test ends (they are shown again, without being activated, by the
	/// cleanup), so that Process.MainWindowHandle is zero: the toolkit's "no window" case. Returns false when Windows still reports a
	/// main window, in which case the caller skips instead of letting the toolkit show the real dialog.
	/// </summary>
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

	/// <summary>The number of visible common dialogs (the file and folder pickers) that the process has open.</summary>
	protected static int CountOpenDialogs() =>
		FindTopLevelWindows(static window => IsWindowVisible(window) && ClassName(window) == commonDialogClass).Count;

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

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	static extern int GetClassName(IntPtr window, StringBuilder className, int maxCount);
}