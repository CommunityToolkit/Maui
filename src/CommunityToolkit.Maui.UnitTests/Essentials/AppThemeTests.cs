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
		Application.Current.OpenWindow(window);

		SetAppTheme(initialAppTheme, Application.Current);

		Assert.Equal(initialAppTheme, Application.Current.RequestedTheme);
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

	[Fact]
	public void AppThemeResourceRemovesExistingDynamicResourceForStaticThemeValue()
	{
		ArgumentNullException.ThrowIfNull(Application.Current);

		Application.Current.Resources["TextColor"] = Colors.Green;
		label.SetDynamicResource(Label.TextColorProperty, "TextColor");

		label.TextColor.Should().Be(Colors.Green);

		AppThemeObject resource = new()
		{
			Light = Colors.Blue,
			Dark = Colors.Purple
		};

		label.SetAppTheme(Label.TextColorProperty, resource);

		label.TextColor.Should().Be(Colors.Blue);

		SetAppTheme(AppTheme.Dark, Application.Current);

		label.TextColor.Should().Be(Colors.Purple);

		Application.Current.Resources["TextColor"] = Colors.Red;

		label.TextColor.Should().Be(Colors.Purple);
	}

	protected override void Dispose(bool isDisposing)
	{
		base.Dispose(isDisposing);
	}

	static void SetAppTheme(in AppTheme theme, Application app)
	{
		app.UserAppTheme = theme;
	}

}