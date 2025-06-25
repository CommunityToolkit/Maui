using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Essentials;

public class AppThemeTests : BaseViewTest
{
	readonly Label label = new();
	readonly Window window;

	public AppThemeTests()
	{
		ArgumentNullException.ThrowIfNull(Application.Current);
		var page = new ContentPage() { Content = label };
		window = new Window
		{
			Page = page
		};
		CreateViewHandler<MockPageHandler>(page);
		Application.Current.AddWindow(window);

		SetAppTheme(initialAppTheme, Application.Current);

		Assert.Equal(initialAppTheme, Application.Current.PlatformAppTheme);
	}

	protected override void Dispose(bool isDisposing)
	{
		Application.Current?.RemoveWindow(window);
		base.Dispose(isDisposing);
	}

	[Fact]
	public void AppThemeColorUsesCorrectColorForTheme()
	{
		ArgumentNullException.ThrowIfNull(Application.Current);

		AppThemeColor color = new()
		{
			Light = Colors.Green,
			Dark = Colors.Red
		};

		label.SetAppThemeColor(Label.TextColorProperty, color);

		Assert.Equal(Colors.Green, label.TextColor);

		SetAppTheme(AppTheme.Dark, Application.Current);

		Assert.Equal(Colors.Red, label.TextColor);
	}

	[Fact]
	public void AppThemeColorUsesDefaultColorWhenDarkColorNotSet()
	{
		ArgumentNullException.ThrowIfNull(Application.Current);

		AppThemeColor color = new()
		{
			Light = Colors.Green,
			Default = Colors.Blue
		};

		label.SetAppThemeColor(Label.TextColorProperty, color);

		Assert.Equal(Colors.Green, label.TextColor);

		SetAppTheme(AppTheme.Dark, Application.Current);

		Assert.Equal(Colors.Blue, label.TextColor);
	}

	[Fact]
	public void AppThemeColorUsesDefaultColorWhenLightColorNotSet()
	{
		ArgumentNullException.ThrowIfNull(Application.Current);

		AppThemeColor color = new()
		{
			Default = Colors.Blue,
			Dark = Colors.Red
		};

		label.SetAppThemeColor(Label.TextColorProperty, color);

		Assert.Equal(Colors.Blue, label.TextColor);

		SetAppTheme(AppTheme.Dark, Application.Current);

		Assert.Equal(Colors.Red, label.TextColor);
	}

	[Fact]
	public void AppThemeResourceUpdatesLabelText()
	{
		ArgumentNullException.ThrowIfNull(Application.Current);

		AppThemeObject resource = new()
		{
			Light = "Light Theme",
			Dark = "Dark Theme"
		};

		label.SetAppTheme(Label.TextProperty, resource);

		label.Text.Should().Be("Light Theme");

		SetAppTheme(AppTheme.Dark, Application.Current);

		label.Text.Should().Be("Dark Theme");
	}

	void SetAppTheme(in AppTheme theme, in IApplication app)
	{
		mockAppInfo.RequestedTheme = theme;
		app.ThemeChanged();
	}
}