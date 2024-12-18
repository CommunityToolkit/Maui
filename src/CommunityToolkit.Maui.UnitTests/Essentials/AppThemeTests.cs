using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Essentials;

public class AppThemeTests : BaseHandlerTest
{
	readonly MockAppInfo mockAppInfo;
	readonly Label label = new();

	public AppThemeTests()
	{
		const AppTheme initialAppTheme = AppTheme.Light;
		AppInfo.SetCurrent(mockAppInfo = new()
		{
			RequestedTheme = initialAppTheme
		});

		ArgumentNullException.ThrowIfNull(Application.Current);

		Application.Current.Windows[0].Page = new ContentPage
		{
			Content = label
		};

		SetAppTheme(initialAppTheme, Application.Current);

		Assert.Equal(initialAppTheme, Application.Current.PlatformAppTheme);
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

		Assert.Equal("Light Theme", label.Text);

		SetAppTheme(AppTheme.Dark, Application.Current);

		Assert.Equal("Dark Theme", label.Text);
	}

	void SetAppTheme(in AppTheme theme, in IApplication app)
	{
		mockAppInfo.RequestedTheme = theme;
		app.ThemeChanged();
	}
}