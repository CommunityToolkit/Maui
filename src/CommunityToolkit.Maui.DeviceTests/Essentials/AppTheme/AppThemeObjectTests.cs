using CommunityToolkit.Maui.Extensions;

namespace CommunityToolkit.Maui.DeviceTests;

// The theme bindings used to read RequestedTheme through a string-path binding, which reflection resolved. They must work in a trimmed
// app and under NativeAOT, with the theme change coming from the real Application.
[Category(TestCategory.AppTheme)]
[Collection(TestCollections.DisableParallelization)]
public sealed class AppThemeObjectTests : CommunityToolkitHandlerTestBase
{
	[Fact]
	public async Task SetAppThemeColorFollowsTheAppTheme()
	{
		SetupBuilder();
		var label = new Label { Text = nameof(SetAppThemeColorFollowsTheAppTheme) };
		label.SetAppThemeColor(Label.TextColorProperty, new AppThemeColor { Light = Colors.Green, Dark = Colors.Red });

		await RunWithUserAppTheme(label, async () =>
		{
			await SetUserAppThemeAsync(AppTheme.Light);
			await AssertTextColorEventually(label, Colors.Green);

			await SetUserAppThemeAsync(AppTheme.Dark);
			await AssertTextColorEventually(label, Colors.Red);

			await SetUserAppThemeAsync(AppTheme.Light);
			await AssertTextColorEventually(label, Colors.Green);
		});
	}

	[Fact]
	public async Task GetBindingOfAppThemeObjectFollowsTheAppTheme()
	{
		SetupBuilder();
		var label = new Label();
		label.SetBinding(Label.TextProperty, new AppThemeObject { Light = "Light", Dark = "Dark" }.GetBinding());

		await RunWithUserAppTheme(label, async () =>
		{
			await SetUserAppThemeAsync(AppTheme.Light);
			await AssertionExtensions.AssertEventually(() => label.Text == "Light", message: "The text did not follow the light theme.");

			await SetUserAppThemeAsync(AppTheme.Dark);
			await AssertionExtensions.AssertEventually(() => label.Text == "Dark", message: "The text did not follow the dark theme.");
		});
	}

	[Fact]
	public async Task AppThemeResourcesOfDifferentTypeArgumentsFollowOneThemeChange()
	{
		SetupBuilder();
		var label = new Label();
		label.SetAppThemeColor(Label.TextColorProperty, new AppThemeColor { Light = Colors.Green, Dark = Colors.Red });
		label.SetAppTheme(Label.TextProperty, new AppThemeObject { Light = "Light", Dark = "Dark" });
		label.SetBinding(VisualElement.BackgroundColorProperty, new AppThemeColor { Light = Colors.White, Dark = Colors.Black }.GetBinding());

		await RunWithUserAppTheme(label, async () =>
		{
			await SetUserAppThemeAsync(AppTheme.Light);
			await AssertionExtensions.AssertEventually(() => label.Text == "Light" && label.TextColor == Colors.Green && label.BackgroundColor == Colors.White, message: "The light theme did not reach all three bindings.");

			await SetUserAppThemeAsync(AppTheme.Dark);
			await AssertionExtensions.AssertEventually(() => label.Text == "Dark" && label.TextColor == Colors.Red && label.BackgroundColor == Colors.Black, message: "One flip to the dark theme did not reach all three bindings.");
		});
	}

	[Fact]
	public async Task AppThemeColorWithoutDarkUsesDefaultInTheDarkTheme()
	{
		SetupBuilder();
		var label = new Label { Text = nameof(AppThemeColorWithoutDarkUsesDefaultInTheDarkTheme) };
		label.SetAppThemeColor(Label.TextColorProperty, new AppThemeColor { Light = Colors.Green, Default = Colors.Blue });

		await RunWithUserAppTheme(label, async () =>
		{
			await SetUserAppThemeAsync(AppTheme.Light);
			await AssertTextColorEventually(label, Colors.Green);

			await SetUserAppThemeAsync(AppTheme.Dark);
			await AssertTextColorEventually(label, Colors.Blue);
		});
	}

	static Task SetUserAppThemeAsync(AppTheme theme) => InvokeOnMainThreadAsync(() => Application.Current!.UserAppTheme = theme);

	static Task AssertTextColorEventually(Label label, Color expected) =>
		AssertionExtensions.AssertEventually(() => label.TextColor == expected, message: $"The text color did not become {expected}.");

	async Task RunWithUserAppTheme(Label label, Func<Task> action)
	{
		await AttachAndRun(label, async _ =>
		{
			await label.OnLoadedAsync();

			try
			{
				await action();
			}
			finally
			{
				await SetUserAppThemeAsync(AppTheme.Unspecified);
			}
		});
	}
}