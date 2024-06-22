using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Essentials;

public class AppThemeTests : BaseHandlerTest
{
	readonly MockAppInfo mockAppInfo;
	readonly Application app;

	public AppThemeTests()
	{
		const AppTheme initialAppTheme = AppTheme.Light;
		AppInfo.SetCurrent(mockAppInfo = new() { RequestedTheme = initialAppTheme });

		Application.Current = app = new Application();

		SetAppTheme(initialAppTheme);

		Assert.Equal(initialAppTheme, app.PlatformAppTheme);
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

		app.MainPage = new ContentPage
		{
			Content = label
		};

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

		app.MainPage = new ContentPage
		{
			Content = label
		};

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

		app.MainPage = new ContentPage
		{
			Content = label
		};

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

		app.MainPage = new ContentPage
		{
			Content = label
		};

		Assert.Equal("Light Theme", label.Text);

		SetAppTheme(AppTheme.Dark);

		Assert.Equal("Dark Theme", label.Text);
	}

	void SetAppTheme(in AppTheme theme)
	{
		mockAppInfo.RequestedTheme = theme;
		((IApplication)app).ThemeChanged();
	}
}