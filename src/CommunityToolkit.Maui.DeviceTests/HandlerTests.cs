using Xunit;

namespace CommunityToolkit.Maui.DeviceTests;

public class HandlerTests
{
	static ContentPage GetTestPage()
	{
		var window = Application.Current?.Windows[0];
		Assert.NotNull(window);

		// Create a dedicated page for each test so the main results page stays clean.
		var page = new ContentPage();
		window.Page = page;
		return page;
	}

	static async Task WaitForHandlerAsync(VisualElement element)
	{
		// Handlers are created during the platform render pass, which happens after the
		// current synchronous code completes. Poll until the handler is attached.
		for (var attempt = 0; attempt < 100 && element.Handler is null; attempt++)
		{
			await Task.Delay(50);
		}
	}

	[Fact]
	public async Task LabelHandlerIsCreated()
	{
		var label = new Label { Text = "Test" };
		var page = GetTestPage();

		page.Content = label;
		await WaitForHandlerAsync(label);

		Assert.NotNull(label.Handler);
		Assert.NotNull(label.Handler.PlatformView);
	}

	[Fact]
	public async Task ButtonHandlerIsCreated()
	{
		var button = new Button { Text = "Click Me" };
		var page = GetTestPage();

		page.Content = button;
		await WaitForHandlerAsync(button);

		Assert.NotNull(button.Handler);
		Assert.NotNull(button.Handler.PlatformView);
	}

	[Fact]
	public async Task EntryHandlerIsCreated()
	{
		var entry = new Entry { Text = "Hello" };
		var page = GetTestPage();

		page.Content = entry;
		await WaitForHandlerAsync(entry);

		Assert.NotNull(entry.Handler);
		Assert.NotNull(entry.Handler.PlatformView);
	}

	[Fact]
	public async Task StackLayoutHandlerIsCreated()
	{
		var layout = new VerticalStackLayout
		{
			Children =
			{
				new Label { Text = "Child 1" },
				new Label { Text = "Child 2" }
			}
		};

		var page = GetTestPage();

		page.Content = layout;
		await WaitForHandlerAsync(layout);

		Assert.NotNull(layout.Handler);
		Assert.NotNull(layout.Handler.PlatformView);
	}
}
