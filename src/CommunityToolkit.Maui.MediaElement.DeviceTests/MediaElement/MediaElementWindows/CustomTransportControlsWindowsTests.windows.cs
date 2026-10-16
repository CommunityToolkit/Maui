using System.Reflection;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Primitives;
using Windows.Media.Playback;
using MediaPlayerElement = Microsoft.UI.Xaml.Controls.MediaPlayerElement;
using WinStyle = Microsoft.UI.Xaml.Style;

namespace CommunityToolkit.Maui.DeviceTests;

// Templates apply during layout, so the tests poll for the effect.
[Category(TestCategory.CustomTransportControlsWindows)]
[Collection(TestCollections.DisableParallelization)]
public class CustomTransportControlsWindowsTests : WinMediaTestBase
{
	sealed record ControlsFixture(MediaPlayerElement Element, CustomTransportControls Controls, Microsoft.UI.Xaml.Controls.AppBarButton InitialButton);

	// Mirrors how MauiMediaElement loads it, so that the controls get exactly the template that ships with the toolkit.
	static WinStyle LoadToolkitStyle()
	{
		using var stream = typeof(MauiMediaElement).Assembly.GetManifestResourceStream("ResourceDictionary.windows.xaml")
			?? throw new InvalidOperationException("ResourceDictionary.windows.xaml is not embedded in the MediaElement assembly.");
		using var reader = new StreamReader(stream);
		var dictionary = (Microsoft.UI.Xaml.ResourceDictionary)Microsoft.UI.Xaml.Markup.XamlReader.Load(reader.ReadToEnd());

		return (WinStyle)dictionary["customTransportcontrols"];
	}

	static WinStyle StyleWithTemplate(string templateContent) =>
		(WinStyle)Microsoft.UI.Xaml.Markup.XamlReader.Load($$"""
			<Style xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="MediaTransportControls">
				<Setter Property="Template">
					<Setter.Value>
						<ControlTemplate TargetType="MediaTransportControls">
							<Grid x:Name="RootGrid">{{templateContent}}</Grid>
						</ControlTemplate>
					</Setter.Value>
				</Setter>
			</Style>
			""");

	Task<ControlsFixture> CreateControlsAsync(Func<WinStyle> createStyle) =>
		InvokeOnMainThreadAsync(() =>
		{
			var player = new MediaPlayer();
			var element = new MediaPlayerElement { AreTransportControlsEnabled = true, Width = 480, Height = 270 };
			element.SetMediaPlayer(player);
			RegisterCleanup(() => ReleasePlayer(player, element));

			var controls = new CustomTransportControls { Style = createStyle() };
			var initialButton = controls.FullScreenButton;
			element.TransportControls = controls;

			return new ControlsFixture(element, controls, initialButton);
		});

	static bool HasTemplate(CustomTransportControls controls) => Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(controls) > 0;

	[Fact]
	public async Task ConstructorStartsWithAnUnattachedFullScreenButtonAndTheOwnDefaultStyleKey()
	{
		SetupWindowsBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var controls = new CustomTransportControls();

			Assert.NotNull(controls.FullScreenButton);
			Assert.Null(controls.FullScreenButton.Parent);

			var defaultStyleKey = typeof(Microsoft.UI.Xaml.Controls.Control)
				.GetProperty("DefaultStyleKey", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!
				.GetValue(controls);
			Assert.Contains(nameof(CustomTransportControls), defaultStyleKey is Type type ? type.FullName : defaultStyleKey?.ToString(), StringComparison.Ordinal);
		});
	}

	[Fact]
	public async Task ToolkitTemplateProvidesTheFullWindowButtonAndRaisesOnTemplateLoaded()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var fixture = await CreateControlsAsync(LoadToolkitStyle);
		var loadedWith = new List<(object? Sender, string ButtonName)>();
		fixture.Controls.OnTemplateLoaded += (sender, _) => loadedWith.Add((sender, fixture.Controls.FullScreenButton.Name));

		await HostAsync(fixture.Element, async _ =>
		{
			await AssertionExtensions.AssertEventually(() => fixture.Controls.FullScreenButton.Name == "FullWindowButton", 5000, 50, "The FullWindowButton of the template was never picked up.");

			// Visibility is not asserted here: inside the toolkit's template, MediaTransportControls collapses and expands its buttons by width while it
			// lays out, so the value would race with that. TemplateButtonThatStartsCollapsedIsShown asserts it with a template without a CommandBar.
			Assert.NotSame(fixture.InitialButton, fixture.Controls.FullScreenButton);
			Assert.NotEmpty(loadedWith);
			Assert.All(loadedWith, entry =>
			{
				Assert.Same(fixture.Controls, entry.Sender);
				Assert.Equal("FullWindowButton", entry.ButtonName);
			});
		});
	}

	[Fact]
	public async Task TemplateWithoutTheFullWindowButtonLeavesThePlaceholderAndRaisesNothing()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var fixture = await CreateControlsAsync(() => StyleWithTemplate(string.Empty));
		var templateLoaded = 0;
		fixture.Controls.OnTemplateLoaded += (_, _) => Interlocked.Increment(ref templateLoaded);

		await HostAsync(fixture.Element, async _ =>
		{
			await AssertionExtensions.AssertEventually(() => HasTemplate(fixture.Controls), 5000, 50, "The template without FullWindowButton was never applied.");

			Assert.Same(fixture.InitialButton, fixture.Controls.FullScreenButton);
			Assert.Equal(0, templateLoaded);
		});
	}

	[Fact]
	public async Task TemplatePartThatIsNotAnAppBarButtonIsIgnored()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var fixture = await CreateControlsAsync(() => StyleWithTemplate("""<Button x:Name="FullWindowButton" />"""));
		var templateLoaded = 0;
		fixture.Controls.OnTemplateLoaded += (_, _) => Interlocked.Increment(ref templateLoaded);

		await HostAsync(fixture.Element, async _ =>
		{
			await AssertionExtensions.AssertEventually(() => HasTemplate(fixture.Controls), 5000, 50, "The template with a plain Button was never applied.");

			Assert.Same(fixture.InitialButton, fixture.Controls.FullScreenButton);
			Assert.Equal(0, templateLoaded);
		});
	}

	// The base MediaTransportControls collapses its FullWindowButton (full-window mode is disabled in WinUI 3); OnApplyTemplate shows it again.
	[Fact]
	public async Task TemplateButtonThatStartsCollapsedIsShown()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var fixture = await CreateControlsAsync(() => StyleWithTemplate("""<AppBarButton x:Name="FullWindowButton" Visibility="Collapsed" />"""));
		var templateLoaded = 0;
		fixture.Controls.OnTemplateLoaded += (_, _) => Interlocked.Increment(ref templateLoaded);

		await HostAsync(fixture.Element, async _ =>
		{
			await AssertionExtensions.AssertEventually(() => fixture.Controls.FullScreenButton.Name == "FullWindowButton", 5000, 50, "The FullWindowButton of the template was never picked up.");

			Assert.NotSame(fixture.InitialButton, fixture.Controls.FullScreenButton);
			Assert.Equal(Microsoft.UI.Xaml.Visibility.Visible, fixture.Controls.FullScreenButton.Visibility);
			Assert.NotEqual(0, templateLoaded);
		});
	}

	[Fact]
	public async Task FullScreenButtonClickTogglesTheIconBetweenTheTwoGlyphs()
	{
		SetupWindowsBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var controls = new CustomTransportControls();

			WinMediaReflection.Invoke(controls, "FullScreenButton_Click", null, null);
			var backToWindow = Assert.IsType<Microsoft.UI.Xaml.Controls.SymbolIcon>(controls.FullScreenButton.Icon);
			Assert.Equal(Microsoft.UI.Xaml.Controls.Symbol.BackToWindow, backToWindow.Symbol);

			WinMediaReflection.Invoke(controls, "FullScreenButton_Click", null, null);
			var fullWindow = Assert.IsType<Microsoft.UI.Xaml.Controls.FontIcon>(controls.FullScreenButton.Icon);
			Assert.Equal("\uE740", fullWindow.Glyph);

			WinMediaReflection.Invoke(controls, "FullScreenButton_Click", null, null);
			Assert.IsType<Microsoft.UI.Xaml.Controls.SymbolIcon>(controls.FullScreenButton.Icon);
		});
	}

	// Nothing subscribes to OnTemplateLoaded here: the null-conditional invocation in OnApplyTemplate must let the method go on to subscribe
	// the click handler, which the icon toggle proves.
	[Fact]
	public async Task ClickingTheTemplateButtonTogglesTheIcon()
	{
		SetupWindowsBuilder();
		await RequireMediaPlayerAsync();
		var fixture = await CreateControlsAsync(LoadToolkitStyle);

		await HostAsync(fixture.Element, async _ =>
		{
			await AssertionExtensions.AssertEventually(() => fixture.Controls.FullScreenButton.Name == "FullWindowButton", 5000, 50, "The FullWindowButton of the template was never picked up.");

			WinMediaAutomation.Invoke(fixture.Controls.FullScreenButton);
			await AssertionExtensions.AssertEventually(() => fixture.Controls.FullScreenButton.Icon is Microsoft.UI.Xaml.Controls.SymbolIcon { Symbol: Microsoft.UI.Xaml.Controls.Symbol.BackToWindow }, 5000, 50,
				"Clicking the FullWindowButton did not switch its icon to BackToWindow.");

			WinMediaAutomation.Invoke(fixture.Controls.FullScreenButton);
			await AssertionExtensions.AssertEventually(() => fixture.Controls.FullScreenButton.Icon is Microsoft.UI.Xaml.Controls.FontIcon { Glyph: "\uE740" }, 5000, 50,
				"The second click did not switch the icon back to the full-window glyph.");
		});
	}
}