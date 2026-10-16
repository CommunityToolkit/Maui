namespace CommunityToolkit.Maui.DeviceTests;

public abstract partial class StatusBarTestBase
{
	/// <summary>
	/// Hosts <paramref name="page"/> in the runner's window (the page's view controller becomes a child of the root view controller),
	/// waits until it is loaded, runs <paramref name="action"/> and removes the page again (Unloaded).
	/// </summary>
	protected Task AttachPageAndRun(Page page, Func<IPlatformViewHandler, Task> action) => AttachViewAndRun(page, action);

	/// <summary>
	/// Hosts <paramref name="view"/> in the runner's window, waits until it raised Loaded (which is when MAUI attaches a PlatformBehavior;
	/// AttachAndRun itself only waits for a layout pass), runs <paramref name="action"/> and removes the view again (Unloaded).
	/// </summary>
	protected Task AttachViewAndRun(VisualElement view, Func<IPlatformViewHandler, Task> action) =>
		AttachAndRun(view, async handler =>
		{
			// The device tests share a busy machine: give the view generously long to raise Loaded.
			await view.OnLoadedAsync(TimeSpan.FromSeconds(10));
			await action(handler);
		});
}