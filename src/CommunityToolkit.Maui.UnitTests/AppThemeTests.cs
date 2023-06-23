using CommunityToolkit.Maui.Extensions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests;

public class AppThemeTests : BaseTest
{
	class MockAppInfo : IAppInfo
	{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		public string PackageName { get; set; }

		public string Name { get; set; }

		public string VersionString { get; set; }

		public Version Version { get; set; }

		public string BuildString { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

		public LayoutDirection RequestedLayoutDirection { get; set; }

		public void ShowSettingsUI()
		{
		}

		public AppTheme RequestedTheme { get; set; }

		public AppPackagingModel PackagingModel { get; set; }
	}

	MockAppInfo mockAppInfo;
	Application app;

	public AppThemeTests()
	{
		AppInfo.SetCurrent(mockAppInfo = new MockAppInfo() { RequestedTheme = AppTheme.Light });
		Application.Current = app = new Application();
	}

	[Fact]
	public void AppThemeColorUsesCorrectColorForTheme()
	{
		var color = new AppThemeColor
		{
			Light = Colors.Green,
			Dark = Colors.Red
		};
		var label = new Label
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
		var color = new AppThemeColor
		{
			Light = Colors.Green,
			Default = Colors.Blue
		};
		var label = new Label
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
		var color = new AppThemeColor
		{
			Default = Colors.Blue,
			Dark = Colors.Red
		};
		var label = new Label
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
		var label = new Label();
		var resource = new AppThemeResource
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

