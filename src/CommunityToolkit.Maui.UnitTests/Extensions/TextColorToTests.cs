using System.Reflection;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Unique.Interface.Namespace.TextStyle;
using Unique.Namespace.TextStyle;
using Xunit;
using Font = Microsoft.Maui.Font;

namespace CommunityToolkit.Maui.UnitTests.Extensions
{
	public class TextColorToTests : BaseTest
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

#pragma warning disable CS8603 // Possible null reference return.
			await Assert.ThrowsAsync<NullReferenceException>(() => label?.TextColorTo(Colors.Red));
#pragma warning restore CS8603 // Possible null reference return.
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
		public async Task Extensions_For_Generic_Class()
		{
			Color originalTextColor = Colors.Blue, updatedTextColor = Colors.Red;

			var textStyleView = new GenericPicker<
				ClassConstraintWithInterface,
				ClassConstraint,
				StructConstraint,
				ClassConstraintWithInterface,
				string,
				int,
				bool,
				ClassConstraintWithInterface?,
				ClassConstraint[],
				ClassConstraintWithInterface,
				RecordClassContstraint,
				RecordClassContstraint[],
				RecordStructContstraint
			>
			{ TextColor = originalTextColor };
			textStyleView.EnableAnimations();

			Assert.Equal(originalTextColor, textStyleView.TextColor);

			var isSuccessful = await textStyleView.TextColorTo(updatedTextColor);

			Assert.True(isSuccessful);
			Assert.Equal(updatedTextColor, textStyleView.TextColor);
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

		[Fact]
		public void AccessModifierForMauiControlsShouldNotBePublic()
		{
			foreach (var (generatedType, control) in GetGeneratedColorAnimationExtensionTypes())
			{
				if (control.Assembly == typeof(Button).Assembly)
				{
					Assert.False(generatedType.IsPublic);
				}
			}
		}

		[Fact]
		public void AccessModifierForCustomControlsShouldMatchTheControl()
		{
			var executingAssembly = Assembly.GetExecutingAssembly();

			foreach (var (generatedType, control) in GetGeneratedColorAnimationExtensionTypes())
			{
				if (control.Assembly == executingAssembly)
				{
					Assert.Equal(control.IsPublic, generatedType.IsPublic);
				}
			}
		}

		static IEnumerable<(Type generatedType, Type control)> GetGeneratedColorAnimationExtensionTypes()
		{
			return from type in Assembly.GetExecutingAssembly().GetTypes()
				   where type.Name.StartsWith("ColorAnimationExtensions_")
				   let method = type.GetMethods().Single(m => m.Name.StartsWith("TextColorTo"))
				   let control = method.GetParameters()[0].ParameterType
				   select (type, control);
		}
	}
}

namespace Unique.Namespace.TextStyle
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

	public class ClassConstraintWithInterface : ISomeInterface
	{

	}

	public class ClassConstraint
	{

	}

	class MyGenericPicker<T> : Picker
	{

	}

	public record RecordClassContstraint
	{

	}


	public readonly record struct RecordStructContstraint
	{

	}

	class MoreGenericPicker<T> : MyGenericPicker<T>
	{

	}

	public struct StructConstraint
	{

	}

	public class GenericPicker<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM> : View, ITextStyle, IAnimatable
		where TA : notnull, ISomeInterface
		where TB : class
		where TC : struct
		where TD : class, ISomeInterface, new()
		//TE has no constraints 
		where TF : notnull
		where TG : unmanaged
		where TH : ISomeInterface?
		where TI : class?
		where TJ : ISomeInterface
		where TK : new()
		where TL : class
		where TM : struct
	{
		public double CharacterSpacing { get; } = 0;

		public Color TextColor { get; set; } = Colors.Transparent;

		public Font Font { get; set; }
	}

	class BrandNewControl : View, ITextStyle, IAnimatable
	{
		public double CharacterSpacing { get; } = 0;

		public Color TextColor { get; set; } = Colors.Transparent;

		public Font Font { get; set; }
	}
}

namespace Unique.Interface.Namespace.TextStyle
{
	public interface ISomeInterface
	{

	}
}