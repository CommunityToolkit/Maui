namespace CommunityToolkit.Maui.DeviceTests;

public abstract partial class StatusBarTestBase
{
	/// <summary>The message of the NotSupportedException that StatusBar.SetColor and SetStyle throw on this platform.</summary>
	protected const string unsupportedStatusBarMessage = "WinUI does not currently support changing the Windows status bar color";

	/// <summary>Hosts <paramref name="page"/> in a new WinUI window and runs <paramref name="action"/> while it is loaded.</summary>
	protected Task AttachPageAndRun(Page page, Func<IPlatformViewHandler, Task> action) => AttachViewAndRun(page, action);

	/// <summary>Hosts <paramref name="view"/> in a new WinUI window, waits until it raised Loaded and runs <paramref name="action"/> while it is loaded.</summary>
	protected Task AttachViewAndRun(VisualElement view, Func<IPlatformViewHandler, Task> action) =>
		AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync(TimeSpan.FromSeconds(10));
			await action(handler);
		});

	// Windows has no status bar code in the toolkit (StatusBar.windows.cs only throws): nothing to reset.
	protected partial Task ResetNativeStateAsync() => Task.CompletedTask;
}