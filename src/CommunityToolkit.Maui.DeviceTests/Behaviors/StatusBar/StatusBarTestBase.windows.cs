namespace CommunityToolkit.Maui.DeviceTests;

public abstract partial class StatusBarTestBase
{
	protected const string unsupportedStatusBarMessage = "WinUI does not currently support changing the Windows status bar color";

	protected Task AttachPageAndRun(Page page, Func<IPlatformViewHandler, Task> action) => AttachViewAndRun(page, action);

	protected Task AttachViewAndRun(VisualElement view, Func<IPlatformViewHandler, Task> action) =>
		AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync(TimeSpan.FromSeconds(10));
			await action(handler);
		});

	// Windows has no status bar code in the toolkit (StatusBar.windows.cs only throws): nothing to reset.
	protected partial Task ResetNativeStateAsync() => Task.CompletedTask;
}