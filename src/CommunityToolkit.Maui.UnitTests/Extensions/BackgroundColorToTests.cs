using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions;

public class BackgroundColorToTests : BaseTest
{
	[Fact]
	public async Task BackgroundColorTo_VerifyColorChanged()
	{
		Color originalBackgroundColor = Colors.Blue, updatedBackgroundColor = Colors.Red;

		VisualElement element = new Label { BackgroundColor = originalBackgroundColor };
		element.EnableAnimations();

		Assert.Equal(originalBackgroundColor, element.BackgroundColor);

		var isSuccessful = await element.BackgroundColorTo(updatedBackgroundColor);

		Assert.True(isSuccessful);
		Assert.Equal(updatedBackgroundColor, element.BackgroundColor);
	}

	[Fact]
	public async Task BackgroundColorTo_VerifyColorChangedForDefaultBackgroundColor()
	{
		Color updatedBackgroundColor = Colors.Yellow;

		VisualElement element = new Label();
		element.EnableAnimations();

		var isSuccessful = await element.BackgroundColorTo(updatedBackgroundColor);

		Assert.True(isSuccessful);
		Assert.Equal(updatedBackgroundColor, element.BackgroundColor);
	}

	[Fact]
	public async Task BackgroundColorTo_DoesNotAllowNullVisualElement()
	{
		VisualElement? element = null;

#pragma warning disable CS8603 // Possible null reference return.
		await Assert.ThrowsAsync<NullReferenceException>(() => element?.BackgroundColorTo(Colors.Red));
#pragma warning restore CS8603 // Possible null reference return.
	}

	[Fact]
	public async Task BackgroundColorTo_DoesNotAllowNullColor()
	{
		VisualElement element = new Label();
		element.EnableAnimations();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		await Assert.ThrowsAsync<ArgumentNullException>(() => element.BackgroundColorTo(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}