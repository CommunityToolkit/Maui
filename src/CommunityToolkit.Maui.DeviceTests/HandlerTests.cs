using Microsoft.Maui.Platform;
using Xunit;

namespace CommunityToolkit.Maui.DeviceTests;

public class HandlerTests
{
	/// <summary>
	/// Creates a handler for the given element using the application's <see cref="IMauiContext"/>.
	/// This creates the handler and its platform view directly, without navigating away from or
	/// replacing the current page (so the visual test runner page stays visible).
	/// Must run on the main thread: on Windows/Android, handler and platform-view creation
	/// requires the UI thread.
	/// </summary>
	static IPlatformViewHandler CreateHandler(IElement element)
	{
		var context = Application.Current?.Handler?.MauiContext;
		Assert.NotNull(context);

		var handler = element.ToHandler(context);
		return Assert.IsAssignableFrom<IPlatformViewHandler>(handler);
	}

	[Fact]
	public async Task LabelHandlerIsCreated()
	{
		var label = new Label { Text = "Test" };
		var handler = await MainThread.InvokeOnMainThreadAsync(() => CreateHandler(label));

		Assert.NotNull(handler);
		Assert.NotNull(handler.PlatformView);
	}

	[Fact]
	public async Task ButtonHandlerIsCreated()
	{
		var button = new Button { Text = "Click Me" };
		var handler = await MainThread.InvokeOnMainThreadAsync(() => CreateHandler(button));

		Assert.NotNull(handler);
		Assert.NotNull(handler.PlatformView);
	}

	[Fact]
	public async Task EntryHandlerIsCreated()
	{
		var entry = new Entry { Text = "Hello" };
		var handler = await MainThread.InvokeOnMainThreadAsync(() => CreateHandler(entry));

		Assert.NotNull(handler);
		Assert.NotNull(handler.PlatformView);
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

		var handler = await MainThread.InvokeOnMainThreadAsync(() => CreateHandler(layout));

		Assert.NotNull(handler);
		Assert.NotNull(handler.PlatformView);
	}
}
