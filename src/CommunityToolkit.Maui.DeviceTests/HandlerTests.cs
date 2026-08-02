using Xunit;

namespace CommunityToolkit.Maui.DeviceTests;

public class HandlerTests
{
	static ContentPage GetTestPage()
	{
		var window = Application.Current?.Windows[0];
		Assert.NotNull(window);
		Assert.NotNull(window.Page);
		return (ContentPage)window.Page;
	}

	[Fact]
	public void LabelHandlerIsCreated()
	{
		var label = new Label { Text = "Test" };
		var page = GetTestPage();

		page.Content = label;

		Assert.NotNull(label.Handler);
		Assert.NotNull(label.Handler.PlatformView);
	}

	[Fact]
	public void ButtonHandlerIsCreated()
	{
		var button = new Button { Text = "Click Me" };
		var page = GetTestPage();

		page.Content = button;

		Assert.NotNull(button.Handler);
		Assert.NotNull(button.Handler.PlatformView);
	}

	[Fact]
	public void EntryHandlerIsCreated()
	{
		var entry = new Entry { Text = "Hello" };
		var page = GetTestPage();

		page.Content = entry;

		Assert.NotNull(entry.Handler);
		Assert.NotNull(entry.Handler.PlatformView);
	}

	[Fact]
	public void StackLayoutHandlerIsCreated()
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

		Assert.NotNull(layout.Handler);
		Assert.NotNull(layout.Handler.PlatformView);
	}
}
