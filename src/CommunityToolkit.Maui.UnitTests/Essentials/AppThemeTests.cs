using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Essentials;

public class AppThemeTests : BaseTest
{
	readonly MockAppInfo mockAppInfo;
	readonly Application app;

	public AppThemeTests()
	{
		AppInfo.SetCurrent(mockAppInfo = new() { RequestedTheme = AppTheme.Light });
		Application.Current = app = new Application();
	}

	[Fact]
	public void AppThemeColorUsesCorrectColorForTheme()
	{
		AppThemeColor color = new()
		{
			Light = Colors.Green,
			Dark = Colors.Red
		};

		Label label = new()
		{
			Text = "Green on Light, Red on Dark"
		};

		label.SetAppThemeColor(Label.TextColorProperty, color);

		Application.Current = null;

		Assert.Equal(Colors.Green, label.TextColor);

		SetAppTheme(AppTheme.Dark);

		Assert.Equal(Colors.Red, label.TextColor);
	}

	[Fact]
	public void AppThemeColorUsesDefaultColorWhenDarkColorNotSet()
	{
		AppThemeColor color = new()
		{
			Light = Colors.Green,
			Default = Colors.Blue
		};

		Label label = new()
		{
			Text = "Green on Light, Red on Dark"
		};

		label.SetAppThemeColor(Label.TextColorProperty, color);

		Application.Current = null;

		Assert.Equal(Colors.Green, label.TextColor);

		SetAppTheme(AppTheme.Dark);

		Assert.Equal(Colors.Blue, label.TextColor);
	}

	[Fact]
	public void AppThemeColorUsesDefaultColorWhenLightColorNotSet()
	{
		AppThemeColor color = new()
		{
			Default = Colors.Blue,
			Dark = Colors.Red
		};

		Label label = new()
		{
			Text = "Green on Light, Red on Dark"
		};

		label.SetAppThemeColor(Label.TextColorProperty, color);

		Application.Current = null;

		Assert.Equal(Colors.Blue, label.TextColor);

		SetAppTheme(AppTheme.Dark);

		Assert.Equal(Colors.Red, label.TextColor);
	}

	[Fact]
	public void AppThemeResourceUpdatesLabelText()
	{
		Label label = new();

		AppThemeObject resource = new()
		{
			Light = "Light Theme",
			Dark = "Dark Theme"
		};

		label.SetAppTheme(Label.TextProperty, resource);

		Application.Current = null;
		Assert.Equal("Light Theme", label.Text);

		SetAppTheme(AppTheme.Dark);

		Assert.Equal("Dark Theme", label.Text);
	}

	void SetAppTheme(AppTheme theme)
	{
		mockAppInfo.RequestedTheme = theme;
		((IApplication)app).ThemeChanged();
	}
}