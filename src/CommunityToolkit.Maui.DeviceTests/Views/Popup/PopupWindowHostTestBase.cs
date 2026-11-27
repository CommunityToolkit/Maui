namespace CommunityToolkit.Maui.DeviceTests;

// The platform files host the Window that these popup tests navigate in, with .NET MAUI's own navigation.
public abstract partial class PopupWindowHostTestBase : MauiWindowHostTestBase
{
	protected override async ValueTask DisposeAsyncCore()
	{
		try
		{
			await base.DisposeAsyncCore();
		}
		finally
		{
			ReleaseNavigationSemaphoreHeldByCloseAsyncThatNeverCompleted();
		}
	}

	// A CloseAsync that never completes keeps the static semaphore of PopupPage, so every popup test that ran after a failed one would fail in its own arrange step
	static void ReleaseNavigationSemaphoreHeldByCloseAsyncThatNeverCompleted()
	{
		var navigationSemaphore = PopupDismissedByNavigationScenarios.GetNavigationSemaphore();
		if (navigationSemaphore.CurrentCount == 0)
		{
			navigationSemaphore.Release();
		}
	}
}
