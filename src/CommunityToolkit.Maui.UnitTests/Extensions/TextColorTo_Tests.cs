using Font = Microsoft.Maui.Font;
using CommunityToolkit.Maui.UnitTests.Extensions.TextStyle;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions
{
	public class TextColorTo_Tests : BaseTest
	{
		[Fact]
		public async Task PublicTextColorTo_VerifyColorChanged()
		{
			Color originalTextColor = Colors.Blue, updatedTextColor = Colors.Red;

			var textStyleView = new PublicTextStyleView { TextColor = originalTextColor };
			textStyleView.EnableAnimations();

			Assert.Equal(originalTextColor, textStyleView.TextColor);

			var isSuccessful = await textStyleView.TextColorTo(updatedTextColor);

			Assert.True(isSuccessful);
			Assert.Equal(updatedTextColor, textStyleView.TextColor);
		}

		[Fact]
		public async Task InternalTextColorTo_VerifyColorChanged()
		{
			Color originalTextColor = Colors.Blue, updatedTextColor = Colors.Red;

			var textStyleView = new InternalTextStyleView { TextColor = originalTextColor };
			textStyleView.EnableAnimations();

			Assert.Equal(originalTextColor, textStyleView.TextColor);

			var isSuccessful = await textStyleView.TextColorTo(updatedTextColor);

			Assert.True(isSuccessful);
			Assert.Equal(updatedTextColor, textStyleView.TextColor);
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
	}
}

namespace CommunityToolkit.Maui.UnitTests.Extensions.TextStyle
{
	public class PublicTextStyleView : View, ICustomTextStyle
	{
		public Color TextColor { get; set; } = new();

		public Font Font { get; set; }

		public double CharacterSpacing { get; set; }
	}

	class InternalTextStyleView : View, ICustomTextStyle
	{
		public Color TextColor { get; set; } = new();

		public Font Font { get; set; }

		public double CharacterSpacing { get; set; }
	}

	// Ensures custom ITextStyle interfaces are supported
	interface ICustomTextStyle : ITextStyle
	{

	}
}