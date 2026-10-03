using System.Globalization;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.DeviceTests.Tests.Additional;

public class ColorToStringConverterTests
{
	[Fact]
	public void ColorToRgbStringConverter_ConvertFrom_Red()
	{
		var converter = new ColorToRgbStringConverter();

		var result = converter.ConvertFrom(Colors.Red);

		Assert.Equal("RGB(255,0,0)", result);
	}

	[Fact]
	public void ColorToRgbStringConverter_ConvertBackTo_ValidColor()
	{
		var converter = new ColorToRgbStringConverter();

		var result = converter.ConvertBackTo("#FF0000", null);

		Assert.Equal(Colors.Red, result);
	}

	[Fact]
	public void ColorToRgbStringConverter_ConvertBackTo_InvalidColor_ReturnsDefault()
	{
		var converter = new ColorToRgbStringConverter();

		var result = converter.ConvertBackTo("not-a-color", null);

		Assert.Equal(Colors.Transparent, result);
	}

	[Fact]
	public void ColorToRgbaStringConverter_ConvertFrom_Red()
	{
		var converter = new ColorToRgbaStringConverter();

		var result = converter.ConvertFrom(Colors.Red);

		Assert.StartsWith("RGBA(", result);
	}

	[Fact]
	public void ColorToHexRgbStringConverter_ConvertFrom_Red()
	{
		var converter = new ColorToHexRgbStringConverter();

		var result = converter.ConvertFrom(Colors.Red);

		Assert.Equal("#FF0000", result);
	}

	[Fact]
	public void ColorToHexRgbStringConverter_ConvertBackTo()
	{
		var converter = new ColorToHexRgbStringConverter();

		var result = converter.ConvertBackTo("#FF0000");

		Assert.Equal(1f, result.Red, 0.01f);
		Assert.Equal(0f, result.Green, 0.01f);
		Assert.Equal(0f, result.Blue, 0.01f);
	}

	[Fact]
	public void ColorToHexRgbaStringConverter_ConvertFrom_Red()
	{
		var converter = new ColorToHexRgbaStringConverter();

		var result = converter.ConvertFrom(Colors.Red);

		Assert.Equal("#FF0000FF", result);
	}

	[Fact]
	public void ColorToHexArgbStringConverter_ConvertFrom_Red()
	{
		var converter = new ColorToHexArgbStringConverter();

		var result = converter.ConvertFrom(Colors.Red);

		Assert.Equal("#FFFF0000", result);
	}

	[Fact]
	public void ColorToCmykStringConverter_ConvertFrom_Red()
	{
		var converter = new ColorToCmykStringConverter();

		var result = converter.ConvertFrom(Colors.Red);

		Assert.StartsWith("CMYK(", result);
	}

	[Fact]
	public void ColorToCmykaStringConverter_ConvertFrom_Red()
	{
		var converter = new ColorToCmykaStringConverter();

		var result = converter.ConvertFrom(Colors.Red);

		Assert.StartsWith("CMYKA(", result);
	}

	[Fact]
	public void ColorToHslStringConverter_ConvertFrom_Red()
	{
		var converter = new ColorToHslStringConverter();

		var result = converter.ConvertFrom(Colors.Red);

		Assert.StartsWith("HSL(", result);
	}

	[Fact]
	public void ColorToHslaStringConverter_ConvertFrom_Red()
	{
		var converter = new ColorToHslaStringConverter();

		var result = converter.ConvertFrom(Colors.Red);

		Assert.StartsWith("HSLA(", result);
	}

	[Fact]
	public void ColorToStringConverters_DefaultConvertReturnValue_IsEmpty()
	{
		Assert.Equal(string.Empty, new ColorToRgbStringConverter().DefaultConvertReturnValue);
		Assert.Equal(string.Empty, new ColorToRgbaStringConverter().DefaultConvertReturnValue);
		Assert.Equal(string.Empty, new ColorToHexRgbStringConverter().DefaultConvertReturnValue);
		Assert.Equal(string.Empty, new ColorToHexRgbaStringConverter().DefaultConvertReturnValue);
		Assert.Equal(string.Empty, new ColorToHexArgbStringConverter().DefaultConvertReturnValue);
		Assert.Equal(string.Empty, new ColorToCmykStringConverter().DefaultConvertReturnValue);
		Assert.Equal(string.Empty, new ColorToCmykaStringConverter().DefaultConvertReturnValue);
		Assert.Equal(string.Empty, new ColorToHslStringConverter().DefaultConvertReturnValue);
		Assert.Equal(string.Empty, new ColorToHslaStringConverter().DefaultConvertReturnValue);
	}
}

public class MultiConverterTests
{
	[Fact]
	public void MultiConverter_Convert_ChainsConverters()
	{
		var multiConverter = new MultiConverter
		{
			new InvertedBoolConverter()
		};

		var result = multiConverter.Convert(true, typeof(bool), null, CultureInfo.InvariantCulture);

		Assert.Equal(false, result);
	}

	[Fact]
	public void MultiConverter_Convert_MultipleConverters()
	{
		var multiConverter = new MultiConverter
		{
			new InvertedBoolConverter(),
			new InvertedBoolConverter()
		};

		// true -> false -> true
		var result = multiConverter.Convert(true, typeof(bool), null, CultureInfo.InvariantCulture);

		Assert.Equal(true, result);
	}

	[Fact]
	public void MultiConverter_ConvertBack_ThrowsNotSupported()
	{
		var multiConverter = new MultiConverter();
		var thrown = false;

		try
		{
			multiConverter.ConvertBack(true, typeof(bool), null, CultureInfo.InvariantCulture);
		}
		catch (NotSupportedException)
		{
			thrown = true;
		}

		Assert.True(thrown);
	}

	[Fact]
	public void MultiConverter_IsList()
	{
		var multiConverter = new MultiConverter();

		Assert.IsAssignableFrom<List<ICommunityToolkitValueConverter>>(multiConverter);
	}
}

public class MultiConverterParameterTests
{
	[Fact]
	public void MultiConverterParameter_DefaultProperties()
	{
		var param = new MultiConverterParameter();

		Assert.Null(param.ConverterType);
		Assert.Null(param.Value);
	}

	[Fact]
	public void MultiConverterParameter_CanSetProperties()
	{
		var param = new MultiConverterParameter
		{
			ConverterType = typeof(InvertedBoolConverter),
			Value = 42
		};

		Assert.Equal(typeof(InvertedBoolConverter), param.ConverterType);
		Assert.Equal(42, param.Value);
	}
}

public class ByteArrayToImageSourceConverterTests
{
	[Fact]
	public void ByteArrayToImageSourceConverter_DefaultConvertReturnValue_IsNull()
	{
		var converter = new ByteArrayToImageSourceConverter();

		Assert.Null(converter.DefaultConvertReturnValue);
	}

	[Fact]
	public void ByteArrayToImageSourceConverter_DefaultConvertBackReturnValue_IsNull()
	{
		var converter = new ByteArrayToImageSourceConverter();

		// The converter's DefaultConvertBackReturnValue is initialized to null
		Assert.Null(converter.DefaultConvertBackReturnValue);
	}
}

public class MaskedBehaviorTests
{
	[Fact]
	public void MaskedBehavior_DefaultMask_IsNull()
	{
		var behavior = new MaskedBehavior();

		Assert.Null(behavior.Mask);
	}

	[Fact]
	public void MaskedBehavior_DefaultUnmaskedCharacter_IsX()
	{
		var behavior = new MaskedBehavior();

		Assert.Equal('X', behavior.UnmaskedCharacter);
	}

	[Fact]
	public void MaskedBehavior_CanSetMask()
	{
		var behavior = new MaskedBehavior
		{
			Mask = "XX-XX-XX"
		};

		Assert.Equal("XX-XX-XX", behavior.Mask);
	}

	[Fact]
	public void MaskedBehavior_CanSetUnmaskedCharacter()
	{
		var behavior = new MaskedBehavior
		{
			UnmaskedCharacter = '0'
		};

		Assert.Equal('0', behavior.UnmaskedCharacter);
	}
}

public class MaxLengthReachedBehaviorTests
{
	[Fact]
	public void MaxLengthReachedBehavior_DefaultCommand_IsNull()
	{
		var behavior = new MaxLengthReachedBehavior();

		Assert.Null(behavior.Command);
	}

	[Fact]
	public void MaxLengthReachedBehavior_DefaultShouldDismissKeyboard_IsFalse()
	{
		var behavior = new MaxLengthReachedBehavior();

		Assert.False(behavior.ShouldDismissKeyboardAutomatically);
	}

	[Fact]
	public void MaxLengthReachedBehavior_CanSetShouldDismissKeyboard()
	{
		var behavior = new MaxLengthReachedBehavior
		{
			ShouldDismissKeyboardAutomatically = true
		};

		Assert.True(behavior.ShouldDismissKeyboardAutomatically);
	}
}

public class MaxLengthReachedEventArgsTests
{
	[Fact]
	public void MaxLengthReachedEventArgs_CarriesText()
	{
		var args = new MaxLengthReachedEventArgs("hello");

		Assert.Equal("hello", args.Text);
	}

	[Fact]
	public void MaxLengthReachedEventArgs_EmptyText()
	{
		var args = new MaxLengthReachedEventArgs(string.Empty);

		Assert.Equal(string.Empty, args.Text);
	}
}

public class UserStoppedTypingBehaviorTests
{
	[Fact]
	public void UserStoppedTypingBehavior_DefaultCommand_IsNull()
	{
		var behavior = new UserStoppedTypingBehavior();

		Assert.Null(behavior.Command);
	}

	[Fact]
	public void UserStoppedTypingBehavior_DefaultStoppedTypingTimeThreshold()
	{
		var behavior = new UserStoppedTypingBehavior();

		Assert.Equal(1000, behavior.StoppedTypingTimeThreshold);
	}

	[Fact]
	public void UserStoppedTypingBehavior_DefaultMinimumLengthThreshold()
	{
		var behavior = new UserStoppedTypingBehavior();

		Assert.Equal(0, behavior.MinimumLengthThreshold);
	}

	[Fact]
	public void UserStoppedTypingBehavior_DefaultShouldDismissKeyboard_IsFalse()
	{
		var behavior = new UserStoppedTypingBehavior();

		Assert.False(behavior.ShouldDismissKeyboardAutomatically);
	}

	[Fact]
	public void UserStoppedTypingBehavior_CanSetProperties()
	{
		var behavior = new UserStoppedTypingBehavior
		{
			StoppedTypingTimeThreshold = 500,
			MinimumLengthThreshold = 3,
			ShouldDismissKeyboardAutomatically = true
		};

		Assert.Equal(500, behavior.StoppedTypingTimeThreshold);
		Assert.Equal(3, behavior.MinimumLengthThreshold);
		Assert.True(behavior.ShouldDismissKeyboardAutomatically);
	}
}

public class EventToCommandBehaviorTests
{
	[Fact]
	public void EventToCommandBehavior_DefaultEventName_IsNull()
	{
		var behavior = new EventToCommandBehavior();

		Assert.Null(behavior.EventName);
	}

	[Fact]
	public void EventToCommandBehavior_DefaultCommand_IsNull()
	{
		var behavior = new EventToCommandBehavior();

		Assert.Null(behavior.Command);
	}

	[Fact]
	public void EventToCommandBehavior_DefaultCommandParameter_IsNull()
	{
		var behavior = new EventToCommandBehavior();

		Assert.Null(behavior.CommandParameter);
	}

	[Fact]
	public void EventToCommandBehavior_DefaultEventArgsConverter_IsNull()
	{
		var behavior = new EventToCommandBehavior();

		Assert.Null(behavior.EventArgsConverter);
	}

	[Fact]
	public void EventToCommandBehavior_CanSetEventName()
	{
		var behavior = new EventToCommandBehavior
		{
			EventName = "Clicked"
		};

		Assert.Equal("Clicked", behavior.EventName);
	}
}

public class ImpliedOrderGridBehaviorTests
{
	[Fact]
	public void ImpliedOrderGridBehavior_DefaultThrowOnLayoutWarning_IsFalse()
	{
		var behavior = new ImpliedOrderGridBehavior();

		Assert.False(behavior.ThrowOnLayoutWarning);
	}

	[Fact]
	public void ImpliedOrderGridBehavior_CanSetThrowOnLayoutWarning()
	{
		var behavior = new ImpliedOrderGridBehavior
		{
			ThrowOnLayoutWarning = true
		};

		Assert.True(behavior.ThrowOnLayoutWarning);
	}
}

public class ProgressBarAnimationBehaviorTests
{
	[Fact]
	public void ProgressBarAnimationBehavior_DefaultLength()
	{
		var behavior = new ProgressBarAnimationBehavior();

		// ProgressBarAnimationBehaviorDefaults.Length is 500
		Assert.Equal(500u, behavior.Length);
	}

	[Fact]
	public void ProgressBarAnimationBehavior_DefaultEasing()
	{
		var behavior = new ProgressBarAnimationBehavior();

		Assert.Equal(Easing.Linear, behavior.Easing);
	}

	[Fact]
	public void ProgressBarAnimationBehavior_CanSetLength()
	{
		var behavior = new ProgressBarAnimationBehavior
		{
			Length = 500u
		};

		Assert.Equal(500u, behavior.Length);
	}

	[Fact]
	public void ProgressBarAnimationBehavior_CanSetEasing()
	{
		var behavior = new ProgressBarAnimationBehavior
		{
			Easing = Easing.CubicOut
		};

		Assert.Equal(Easing.CubicOut, behavior.Easing);
	}
}

public class AnimationBehaviorTests
{
	[Fact]
	public void AnimationBehavior_DefaultAnimationType_IsNull()
	{
		var behavior = new AnimationBehavior();

		Assert.Null(behavior.AnimationType);
	}

	[Fact]
	public void AnimationBehavior_CanSetAnimationType()
	{
		var animation = new CommunityToolkit.Maui.Animations.FadeAnimation();
		var behavior = new AnimationBehavior
		{
			AnimationType = animation
		};

		Assert.Same(animation, behavior.AnimationType);
	}
}