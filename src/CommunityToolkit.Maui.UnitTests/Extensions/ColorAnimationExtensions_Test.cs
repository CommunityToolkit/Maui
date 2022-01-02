using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions;

public class ColorAnimationExtensions_Test : BaseTest
{
	[Fact]
	public async Task CustomTextColorTo_VerifyColorChanged()
	{
		Color originalTextColor = Colors.Blue, updatedTextColor = Colors.Red;

		var customTextStyleView = new CustomTextStyleView { TextColor = originalTextColor };
		customTextStyleView.EnableAnimations();

		Assert.Equal(originalTextColor, customTextStyleView.TextColor);

		var isSuccessful = await customTextStyleView.TextColorTo(updatedTextColor);

		Assert.True(isSuccessful);
		Assert.Equal(updatedTextColor, customTextStyleView.TextColor);
	}

	[Fact]
	public async Task LabelTextColorTo_VerifyColorChanged()
	{
		Color originalTextColor = Colors.Blue, updatedTextColor = Colors.Red;

		var label = new Label { TextColor = originalTextColor };
		label.EnableAnimations();

		Assert.Equal(originalTextColor, label.TextColor);

		var isSuccessful = await label.TextColorTo(updatedTextColor);

		Assert.True(isSuccessful);
		Assert.Equal(updatedTextColor, label.TextColor);
	}

	[Fact]
	public async Task LabelTextColorTo_VerifyColorChangedForDefaultBackgroundColor()
	{
		Color updatedTextColor = Colors.Yellow;

		var label = new Label();
		label.EnableAnimations();

		var isSuccessful = await label.TextColorTo(updatedTextColor);

		Assert.True(isSuccessful);
		Assert.Equal(updatedTextColor, label.TextColor);
	}

	[Fact]
	public async Task LabelTextColorTo_VerifyFalseWhenAnimationContextNotSet()
	{
		var label = new Label();
		Assert.Null(label.TextColor);

		var isSuccessful = await label.TextColorTo(Colors.Red);

		Assert.False(isSuccessful);
		Assert.Equal(Colors.Transparent, label.TextColor);
	}

	[Fact]
	public async Task LabelTextColorTo_DoesNotAllowNullVisualElement()
	{
		Label? label = null;

		await Assert.ThrowsAsync<NullReferenceException>(() => label?.TextColorTo(Colors.Red));
	}

	[Fact]
	public async Task LabelTextColorTo_DoesNotAllowNullColor()
	{
		var label = new Label();
		label.EnableAnimations();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		await Assert.ThrowsAsync<ArgumentNullException>(() => label.TextColorTo(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

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
	public async Task BackgroundColorTo_VerifyFalseWhenAnimationContextNotSet()
	{
		VisualElement element = new Label();
		Assert.Null(element.BackgroundColor);

		var isSuccessful = await element.BackgroundColorTo(Colors.Red);

		Assert.False(isSuccessful);
		Assert.Equal(Colors.Transparent, element.BackgroundColor);
	}

	[Fact]
	public async Task BackgroundColorTo_DoesNotAllowNullVisualElement()
	{
		VisualElement? element = null;

		await Assert.ThrowsAsync<NullReferenceException>(() => element?.BackgroundColorTo(Colors.Red));
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

public class CustomTextStyleView : View, ITextStyle
{
	public Color TextColor { get; set; } = new();

	public Font Font { get; set; }

	public double CharacterSpacing { get; set; }
}