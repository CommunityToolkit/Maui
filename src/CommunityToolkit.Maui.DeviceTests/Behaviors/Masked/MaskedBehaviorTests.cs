using CommunityToolkit.Maui.Behaviors;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;

namespace CommunityToolkit.Maui.DeviceTests;

// Device tests of MaskedBehavior with text that comes from the platform view, as typed text does. The behavior applies the mask while the
// Text change is handled on iOS and Mac Catalyst (see the .macios.cs partial) and right after it, from the dispatcher, on the other
// platforms (Android: https://github.com/CommunityToolkit/Maui/issues/460). The platform partials write and read the platform view's text.
[Category(TestCategory.MaskedBehavior)]
[Collection(TestCollections.DisableParallelization)]
public partial class MaskedBehaviorTests : CommunityToolkitHandlerTestBase
{
	const string mask = "XX-XX-XX";

	[Theory]
	[InlineData("abcdef", "ab-cd-ef")]
	[InlineData("abc", "ab-c")]
	// Longer than the mask (a paste): the behavior cuts the text, one character per pass, until it fits the mask
	[InlineData("abcdefghij", "ab-cd-ef")]
	public async Task TextFromThePlatformViewIsMasked(string text, string maskedText)
	{
		SetupBuilder();
		var entry = CreateEntryWithMask();

		await AttachAndRun(entry, async handler =>
		{
			await entry.OnLoadedAsync();
			var platformView = handler.PlatformView!;

			SetPlatformText(platformView, text);

			await Wait(() => entry.Text == maskedText && GetPlatformText(platformView) == maskedText, timeout: 2000);
			Assert.Equal(maskedText, entry.Text);
			Assert.Equal(maskedText, GetPlatformText(platformView));
		});
	}

	static Entry CreateEntryWithMask() => new()
	{
		WidthRequest = 200,
		Behaviors =
		{
			new MaskedBehavior
			{
				Mask = mask
			}
		}
	};
}