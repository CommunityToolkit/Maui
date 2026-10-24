namespace CommunityToolkit.Maui.DeviceTests;

public abstract partial class StatusBarTestBase
{
	protected Task AttachPageAndRun(Page page, Func<IPlatformViewHandler, Task> action) => AttachViewAndRun(page, action);

	// AttachAndRun only waits for a layout pass, but MAUI attaches a PlatformBehavior when the view raises Loaded.
	protected Task AttachViewAndRun(VisualElement view, Func<IPlatformViewHandler, Task> action) =>
		AttachAndRun(view, async handler =>
		{
			// The device tests share a busy machine: give the view generously long to raise Loaded.
			await view.OnLoadedAsync(TimeSpan.FromSeconds(10));
			await action(handler);
		});
}