using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Essentials;

public class AppThemeSharedSourceTests : BaseViewTest
{
	readonly Label label = new();

	public AppThemeSharedSourceTests()
	{
		ArgumentNullException.ThrowIfNull(Application.Current);
		var page = new ContentPage { Content = label };
		CreateViewHandler<MockPageHandler>(page);
		Application.Current.OpenWindow(new Window { Page = page });

		Application.Current.UserAppTheme = initialAppTheme;
	}

	// AppThemeColor and AppThemeObject used to get one theme source per generic argument; they now share one.
	[Fact]
	public void AppThemeResourcesOfDifferentGenericArguments_FollowTheSameThemeChanges()
	{
		ArgumentNullException.ThrowIfNull(Application.Current);

		label.SetAppThemeColor(Label.TextColorProperty, new AppThemeColor { Light = Colors.Blue, Dark = Colors.Purple });
		label.SetAppTheme(Label.TextProperty, new AppThemeObject { Light = "Light", Dark = "Dark" });
		label.SetBinding(Label.BackgroundColorProperty, new AppThemeColor { Light = Colors.White, Dark = Colors.Black }.GetBinding());

		label.TextColor.Should().Be(Colors.Blue);
		label.Text.Should().Be("Light");
		label.BackgroundColor.Should().Be(Colors.White);

		Application.Current.UserAppTheme = AppTheme.Dark;

		label.TextColor.Should().Be(Colors.Purple);
		label.Text.Should().Be("Dark");
		label.BackgroundColor.Should().Be(Colors.Black);

		Application.Current.UserAppTheme = AppTheme.Light;

		label.TextColor.Should().Be(Colors.Blue);
		label.Text.Should().Be("Light");
		label.BackgroundColor.Should().Be(Colors.White);
	}
}
