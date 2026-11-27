using Application = Microsoft.Maui.Controls.Application;
using Window = Microsoft.Maui.Controls.Window;

namespace CommunityToolkit.Maui.DeviceTests;

// The runner on Windows is a real .NET MAUI app, so each scenario opens a Window of its own in it with .NET MAUI's own navigation.
public abstract partial class PopupWindowHostTestBase
{
	static readonly TimeSpan pageLoadTimeout = TimeSpan.FromSeconds(10);

	protected Task RunInShellAsync(Func<Shell, Task> scenario)
	{
		SetupBuilder();
		var shell = PopupDismissedByNavigationScenarios.CreateShell(out var firstPage);

		return RunInWindowAsync(shell, firstPage, _ => scenario(shell));
	}

	protected Task RunInContentPageAsync(Func<ContentPage, Task> scenario, Action<MauiAppBuilder>? configureBuilder = null)
	{
		SetupBuilder(configureBuilder);
		var page = new ContentPage();

		return RunInWindowAsync(page, page, _ => scenario(page));
	}

	protected Task RunInTwoWindowsAsync(Func<ContentPage, ContentPage, Task> scenario)
	{
		SetupBuilder();
		var pageOfFirstWindow = new ContentPage();
		var pageOfSecondWindow = new ContentPage();

		return RunInWindowAsync(pageOfFirstWindow, pageOfFirstWindow, _ =>
			RunInWindowAsync(pageOfSecondWindow, pageOfSecondWindow, _ => scenario(pageOfFirstWindow, pageOfSecondWindow)));
	}

	protected Task RunInWindowAsync(Page root, ContentPage loadedPage, Func<Window, Task> scenario) =>
		InvokeOnMainThreadAsync(async () =>
		{
			var application = Application.Current ?? throw new InvalidOperationException("The runner has no .NET MAUI application.");
			var window = new Window(root);
			application.OpenWindow(window);

			try
			{
				await loadedPage.OnLoadedAsync(pageLoadTimeout);
				await scenario(window);
			}
			finally
			{
				// Modal pages are not under window.Page; a live handler would arrange against the window's service scope after CloseWindow disposes it
				foreach (var modalPage in window.Navigation.ModalStack.ToArray())
				{
					modalPage.DisconnectHandlers();
				}

				window.Page?.DisconnectHandlers();
				root.DisconnectHandlers();
				application.CloseWindow(window);
			}
		});

	// WinUI has no native state to compare: the popup is a .NET MAUI modal page there, which the scenarios already assert through the modal stack
	protected static Task NoNativeStateToAssertAsync(bool isPresented) => Task.CompletedTask;
}