using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Xaml;

namespace CommunityToolkit.Maui.DeviceTests;

// https://github.com/CommunityToolkit/Maui/issues/3114: inflates MediaElement from run-time XAML under trimming.
// Other tests here create MediaElement in code, so this test alone cannot catch a trimmed constructor.
[Category(TestCategory.MediaElementAndroid)]
public class MediaElementRuntimeXamlAndroidTests : MediaElementAndroidTestBase
{
	const string pageXaml =
		"""
		<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
		             xmlns:toolkit="clr-namespace:CommunityToolkit.Maui.Views;assembly=CommunityToolkit.Maui.MediaElement">
		    <toolkit:MediaElement x:Name="player" xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml" WidthRequest="200" HeightRequest="100" ShouldAutoPlay="False" />
		</ContentPage>
		""";

	[Fact]
	public async Task MediaElementLoadedFromRuntimeXamlCreatesAWorkingPlatformView()
	{
		SetupProbeBuilder();
		var mediaElement = await InvokeOnMainThreadAsync(() =>
		{
			var page = new ContentPage();
#pragma warning disable IL2026, IL3050 // Run-time XAML is the scenario under test.
			page.LoadFromXaml(pageXaml);
#pragma warning restore IL2026, IL3050
			return (MediaElement)page.FindByName("player");
		});

		await HostAsync(mediaElement, handler =>
		{
			Assert.NotNull(GetMauiMediaElement(mediaElement));
			Assert.NotNull(GetPlayer(mediaElement));
			Assert.Equal(200, mediaElement.WidthRequest);
			Assert.False(mediaElement.ShouldAutoPlay);
		});
	}
}