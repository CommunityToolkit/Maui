using CommunityToolkit.Maui.UnitTests.Extensions.TextStyle;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Xunit;
using Font = Microsoft.Maui.Font;

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
		public async Task GenericPickerShouldUseThePickerExtension()
		{
			var genericPicker = new MyGenericPicker<string>();
			genericPicker.EnableAnimations();

			Color updatedTextColor = Colors.Yellow;
			var isSuccessful = await genericPicker.TextColorTo(updatedTextColor);

			Assert.True(isSuccessful);
		}

		[Fact]
		public async Task MoreGenericPickerShouldUseThePickerExtension()
		{
			var genericPicker = new MoreGenericPicker<string>();
			genericPicker.EnableAnimations();

			Color updatedTextColor = Colors.Yellow;
			var isSuccessful = await genericPicker.TextColorTo(updatedTextColor);

			Assert.True(isSuccessful);
		}
		
		[Fact]
		public async Task BrandNewControlShouldHaveHisOwnExtensionMethod()
		{
			var brandNewControl = new BrandNewControl();
			brandNewControl.EnableAnimations();

			Color updatedTextColor = Colors.Yellow;
			var isSuccessful = await brandNewControl.TextColorTo(updatedTextColor);

			Assert.True(isSuccessful);
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

	class MyGenericPicker<T> : Picker
	{

	}

	class MoreGenericPicker<T> : MyGenericPicker<T>
	{

	}

	class BrandNewControl : View, ITextStyle, IAnimatable
	{
		public double CharacterSpacing { get; } = 0;

		public Color TextColor { get; set; } = Colors.Transparent;

		public Font Font { get; set; }
	}
}