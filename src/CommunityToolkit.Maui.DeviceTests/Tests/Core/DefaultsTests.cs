using System.Reflection;
using CommunityToolkit.Maui.Core;
using Xunit;

namespace CommunityToolkit.Maui.DeviceTests.Tests.Core;

/// <summary>
/// All Defaults classes in CommunityToolkit.Maui.Core are internal static.
/// Per PR #3251, reflection is the sanctioned approach for testing toolkit-owned internals.
/// </summary>
static class DefaultsReflectionHelper
{
	public static readonly Assembly CoreAssembly = typeof(MathOperator).Assembly;

	public static Type GetDefaultsType(string typeName) =>
		CoreAssembly.GetType($"CommunityToolkit.Maui.Core.{typeName}")
		?? throw new InvalidOperationException($"Type CommunityToolkit.Maui.Core.{typeName} not found");

	public static object? GetConstValue(string typeName, string fieldName)
	{
		var type = GetDefaultsType(typeName);
		var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
		Assert.NotNull(field);
		return field.GetValue(null);
	}

	public static object? GetPropertyValue(string typeName, string propertyName)
	{
		var type = GetDefaultsType(typeName);
		var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
		Assert.NotNull(property);
		return property.GetValue(null);
	}
}

public class AlertDefaultsTests
{
	[Fact]
	public void AlertDefaults_FontSize_Is14()
	{
		Assert.Equal(14d, DefaultsReflectionHelper.GetConstValue("AlertDefaults", "FontSize"));
	}

	[Fact]
	public void AlertDefaults_CharacterSpacing_IsZero()
	{
		Assert.Equal(0.0d, DefaultsReflectionHelper.GetConstValue("AlertDefaults", "CharacterSpacing"));
	}

	[Fact]
	public void AlertDefaults_ActionButtonText_IsOK()
	{
		Assert.Equal("OK", DefaultsReflectionHelper.GetConstValue("AlertDefaults", "ActionButtonText"));
	}

	[Fact]
	public void AlertDefaults_TextColor_IsBlack()
	{
		Assert.Equal(Colors.Black, DefaultsReflectionHelper.GetPropertyValue("AlertDefaults", "TextColor"));
	}

	[Fact]
	public void AlertDefaults_BackgroundColor_IsLightGray()
	{
		Assert.Equal(Colors.LightGray, DefaultsReflectionHelper.GetPropertyValue("AlertDefaults", "BackgroundColor"));
	}
}

public class AvatarViewDefaultsTests
{
	[Fact]
	public void AvatarViewDefaults_BorderWidth_Is1()
	{
		Assert.Equal(1d, DefaultsReflectionHelper.GetConstValue("AvatarViewDefaults", "BorderWidth"));
	}

	[Fact]
	public void AvatarViewDefaults_HeightRequest_Is48()
	{
		Assert.Equal(48d, DefaultsReflectionHelper.GetConstValue("AvatarViewDefaults", "HeightRequest"));
	}

	[Fact]
	public void AvatarViewDefaults_WidthRequest_Is48()
	{
		Assert.Equal(48d, DefaultsReflectionHelper.GetConstValue("AvatarViewDefaults", "WidthRequest"));
	}

	[Fact]
	public void AvatarViewDefaults_Text_IsQuestionMark()
	{
		Assert.Equal("?", DefaultsReflectionHelper.GetConstValue("AvatarViewDefaults", "Text"));
	}

	[Fact]
	public void AvatarViewDefaults_BorderColor_IsWhite()
	{
		Assert.Equal(Colors.White, DefaultsReflectionHelper.GetPropertyValue("AvatarViewDefaults", "BorderColor"));
	}

	[Fact]
	public void AvatarViewDefaults_CornerRadius_Is24()
	{
		var value = DefaultsReflectionHelper.GetPropertyValue("AvatarViewDefaults", "CornerRadius");
		Assert.NotNull(value);
		Assert.Equal(new CornerRadius(24, 24, 24, 24), (CornerRadius)value);
	}

	[Fact]
	public void AvatarViewDefaults_Padding_Is1()
	{
		var value = DefaultsReflectionHelper.GetPropertyValue("AvatarViewDefaults", "Padding");
		Assert.NotNull(value);
		Assert.Equal(new Thickness(1), (Thickness)value);
	}
}

public class DrawingViewDefaultsTests
{
	[Fact]
	public void DrawingViewDefaults_MinimumGranularity_Is5()
	{
		Assert.Equal(5, DefaultsReflectionHelper.GetConstValue("DrawingViewDefaults", "MinimumGranularity"));
	}

	[Fact]
	public void DrawingViewDefaults_LineWidth_Is5()
	{
		Assert.Equal(5f, DefaultsReflectionHelper.GetConstValue("DrawingViewDefaults", "LineWidth"));
	}

	[Fact]
	public void DrawingViewDefaults_ShouldSmoothPathWhenDrawn_IsTrue()
	{
		Assert.Equal(true, DefaultsReflectionHelper.GetConstValue("DrawingViewDefaults", "ShouldSmoothPathWhenDrawn"));
	}

	[Fact]
	public void DrawingViewDefaults_IsMultiLineModeEnabled_IsFalse()
	{
		Assert.Equal(false, DefaultsReflectionHelper.GetConstValue("DrawingViewDefaults", "IsMultiLineModeEnabled"));
	}

	[Fact]
	public void DrawingViewDefaults_ShouldClearOnFinish_IsFalse()
	{
		Assert.Equal(false, DefaultsReflectionHelper.GetConstValue("DrawingViewDefaults", "ShouldClearOnFinish"));
	}

	[Fact]
	public void DrawingViewDefaults_LineColor_IsBlack()
	{
		Assert.Equal(Colors.Black, DefaultsReflectionHelper.GetPropertyValue("DrawingViewDefaults", "LineColor"));
	}

	[Fact]
	public void DrawingViewDefaults_BackgroundColor_IsLightGray()
	{
		Assert.Equal(Colors.LightGray, DefaultsReflectionHelper.GetPropertyValue("DrawingViewDefaults", "BackgroundColor"));
	}
}

public class DockLayoutDefaultsTests
{
	[Fact]
	public void DockLayoutDefaults_DockPosition_IsNone()
	{
		Assert.Equal(DockPosition.None, DefaultsReflectionHelper.GetConstValue("DockLayoutDefaults", "DockPosition"));
	}
}

public class ExpanderDefaultsTests
{
	[Fact]
	public void ExpanderDefaults_Direction_IsDown()
	{
		Assert.Equal(ExpandDirection.Down, DefaultsReflectionHelper.GetConstValue("ExpanderDefaults", "Direction"));
	}
}

public class FadeAnimationDefaultsTests
{
	[Fact]
	public void FadeAnimationDefaults_Length_Is300()
	{
		Assert.Equal(300u, DefaultsReflectionHelper.GetConstValue("FadeAnimationDefaults", "Length"));
	}

	[Fact]
	public void FadeAnimationDefaults_Opacity_Is0Point3()
	{
		Assert.Equal(0.3, DefaultsReflectionHelper.GetConstValue("FadeAnimationDefaults", "Opacity"));
	}
}

public class ImageTouchBehaviorDefaultsTests
{
	[Fact]
	public void ImageTouchBehaviorDefaults_DefaultBackgroundImageSource_IsNull()
	{
		Assert.Null(DefaultsReflectionHelper.GetConstValue("ImageTouchBehaviorDefaults", "DefaultBackgroundImageSource"));
	}

	[Fact]
	public void ImageTouchBehaviorDefaults_HoveredBackgroundImageSource_IsNull()
	{
		Assert.Null(DefaultsReflectionHelper.GetConstValue("ImageTouchBehaviorDefaults", "HoveredBackgroundImageSource"));
	}

	[Fact]
	public void ImageTouchBehaviorDefaults_PressedBackgroundImageSource_IsNull()
	{
		Assert.Null(DefaultsReflectionHelper.GetConstValue("ImageTouchBehaviorDefaults", "PressedBackgroundImageSource"));
	}

	[Fact]
	public void ImageTouchBehaviorDefaults_DefaultBackgroundImageAspect_IsAspectFit()
	{
		Assert.Equal(Aspect.AspectFit, DefaultsReflectionHelper.GetConstValue("ImageTouchBehaviorDefaults", "DefaultBackgroundImageAspect"));
	}

	[Fact]
	public void ImageTouchBehaviorDefaults_ShouldSetImageOnAnimationEnd_IsFalse()
	{
		Assert.Equal(false, DefaultsReflectionHelper.GetConstValue("ImageTouchBehaviorDefaults", "ShouldSetImageOnAnimationEnd"));
	}
}

public class MaxLengthReachedBehaviorDefaultsTests
{
	[Fact]
	public void MaxLengthReachedBehaviorDefaults_ShouldDismissKeyboardAutomatically_IsFalse()
	{
		Assert.Equal(false, DefaultsReflectionHelper.GetConstValue("MaxLengthReachedBehaviorDefaults", "ShouldDismissKeyboardAutomatically"));
	}

	[Fact]
	public void MaxLengthReachedBehaviorDefaults_Command_IsNull()
	{
		Assert.Null(DefaultsReflectionHelper.GetPropertyValue("MaxLengthReachedBehaviorDefaults", "Command"));
	}
}

public class MultiValidationBehaviorDefaultsTests
{
	[Fact]
	public void MultiValidationBehaviorDefaults_Errors_IsNull()
	{
		Assert.Null(DefaultsReflectionHelper.GetPropertyValue("MultiValidationBehaviorDefaults", "Errors"));
	}

	[Fact]
	public void MultiValidationBehaviorDefaults_Error_IsNull()
	{
		Assert.Null(DefaultsReflectionHelper.GetPropertyValue("MultiValidationBehaviorDefaults", "Error"));
	}
}

public class NumericValidationBehaviorDefaultsTests
{
	[Fact]
	public void NumericValidationBehaviorDefaults_MinimumValue_IsNegativeInfinity()
	{
		Assert.Equal(double.NegativeInfinity, DefaultsReflectionHelper.GetConstValue("NumericValidationBehaviorDefaults", "MinimumValue"));
	}

	[Fact]
	public void NumericValidationBehaviorDefaults_MaximumValue_IsPositiveInfinity()
	{
		Assert.Equal(double.PositiveInfinity, DefaultsReflectionHelper.GetConstValue("NumericValidationBehaviorDefaults", "MaximumValue"));
	}

	[Fact]
	public void NumericValidationBehaviorDefaults_MinimumDecimalPlaces_IsZero()
	{
		Assert.Equal(0, DefaultsReflectionHelper.GetConstValue("NumericValidationBehaviorDefaults", "MinimumDecimalPlaces"));
	}

	[Fact]
	public void NumericValidationBehaviorDefaults_MaximumDecimalPlaces_IsIntMaxValue()
	{
		Assert.Equal(int.MaxValue, DefaultsReflectionHelper.GetConstValue("NumericValidationBehaviorDefaults", "MaximumDecimalPlaces"));
	}
}

public class ProgressBarAnimationBehaviorDefaultsTests
{
	[Fact]
	public void ProgressBarAnimationBehaviorDefaults_Progress_IsZero()
	{
		Assert.Equal(0.0, DefaultsReflectionHelper.GetConstValue("ProgressBarAnimationBehaviorDefaults", "Progress"));
	}

	[Fact]
	public void ProgressBarAnimationBehaviorDefaults_Length_Is500()
	{
		Assert.Equal(500u, DefaultsReflectionHelper.GetConstValue("ProgressBarAnimationBehaviorDefaults", "Length"));
	}

	[Fact]
	public void ProgressBarAnimationBehaviorDefaults_Easing_IsLinear()
	{
		Assert.Equal(Easing.Linear, DefaultsReflectionHelper.GetPropertyValue("ProgressBarAnimationBehaviorDefaults", "Easing"));
	}
}

public class RatingViewDefaultsTests
{
	[Fact]
	public void RatingViewDefaults_Rating_IsZero()
	{
		Assert.Equal(0.0, DefaultsReflectionHelper.GetConstValue("RatingViewDefaults", "Rating"));
	}

	[Fact]
	public void RatingViewDefaults_IsReadOnly_IsFalse()
	{
		Assert.Equal(false, DefaultsReflectionHelper.GetConstValue("RatingViewDefaults", "IsReadOnly"));
	}

	[Fact]
	public void RatingViewDefaults_ItemShapeSize_Is20()
	{
		Assert.Equal(20.0, DefaultsReflectionHelper.GetConstValue("RatingViewDefaults", "ItemShapeSize"));
	}

	[Fact]
	public void RatingViewDefaults_MaximumRating_Is5()
	{
		Assert.Equal(5, DefaultsReflectionHelper.GetConstValue("RatingViewDefaults", "MaximumRating"));
	}

	[Fact]
	public void RatingViewDefaults_MaximumRatingLimit_Is10()
	{
		Assert.Equal(10, DefaultsReflectionHelper.GetConstValue("RatingViewDefaults", "MaximumRatingLimit"));
	}

	[Fact]
	public void RatingViewDefaults_ShapeBorderThickness_Is1()
	{
		Assert.Equal(1.0, DefaultsReflectionHelper.GetConstValue("RatingViewDefaults", "ShapeBorderThickness"));
	}

	[Fact]
	public void RatingViewDefaults_Spacing_Is10()
	{
		Assert.Equal(10.0, DefaultsReflectionHelper.GetConstValue("RatingViewDefaults", "Spacing"));
	}

	[Fact]
	public void RatingViewDefaults_Shape_IsStar()
	{
		Assert.Equal(RatingViewShape.Star, DefaultsReflectionHelper.GetConstValue("RatingViewDefaults", "Shape"));
	}

	[Fact]
	public void RatingViewDefaults_FillOption_IsShape()
	{
		Assert.Equal(RatingViewFillOption.Shape, DefaultsReflectionHelper.GetConstValue("RatingViewDefaults", "FillOption"));
	}

	[Fact]
	public void RatingViewDefaults_EmptyShapeColor_IsTransparent()
	{
		Assert.Equal(Colors.Transparent, DefaultsReflectionHelper.GetPropertyValue("RatingViewDefaults", "EmptyShapeColor"));
	}

	[Fact]
	public void RatingViewDefaults_FillColor_IsYellow()
	{
		Assert.Equal(Colors.Yellow, DefaultsReflectionHelper.GetPropertyValue("RatingViewDefaults", "FillColor"));
	}

	[Fact]
	public void RatingViewDefaults_ShapeBorderColor_IsGrey()
	{
		Assert.Equal(Colors.Grey, DefaultsReflectionHelper.GetPropertyValue("RatingViewDefaults", "ShapeBorderColor"));
	}

	[Fact]
	public void RatingViewDefaults_ShapePadding_IsZero()
	{
		var value = DefaultsReflectionHelper.GetPropertyValue("RatingViewDefaults", "ShapePadding");
		Assert.NotNull(value);
		Assert.Equal(new Thickness(0), (Thickness)value);
	}
}

public class RequiredStringValidationBehaviorDefaultsTests
{
	[Fact]
	public void RequiredStringValidationBehaviorDefaults_RequiredString_IsNull()
	{
		Assert.Null(DefaultsReflectionHelper.GetConstValue("RequiredStringValidationBehaviorDefaults", "RequiredString"));
	}

	[Fact]
	public void RequiredStringValidationBehaviorDefaults_ExactMatch_IsTrue()
	{
		Assert.Equal(true, DefaultsReflectionHelper.GetConstValue("RequiredStringValidationBehaviorDefaults", "ExactMatch"));
	}
}

public class SpeechToTextOptionsDefaultsTests
{
	[Fact]
	public void SpeechToTextOptionsDefaults_ShouldReportPartialResults_IsTrue()
	{
		Assert.Equal(true, DefaultsReflectionHelper.GetConstValue("SpeechToTextOptionsDefaults", "ShouldReportPartialResults"));
	}

	[Fact]
	public void SpeechToTextOptionsDefaults_AutoStopSilenceTimeout_IsTimeSpanMaxValue()
	{
		Assert.Equal(TimeSpan.MaxValue, DefaultsReflectionHelper.GetPropertyValue("SpeechToTextOptionsDefaults", "AutoStopSilenceTimeout"));
	}
}

public class StateViewDefaultsTests
{
	[Fact]
	public void StateViewDefaults_StateKey_IsEmpty()
	{
		Assert.Equal(string.Empty, DefaultsReflectionHelper.GetConstValue("StateViewDefaults", "StateKey"));
	}
}

public class TouchBehaviorDefaultsTests
{
	[Fact]
	public void TouchBehaviorDefaults_HoveredOpacity_Is1()
	{
		Assert.Equal(1d, DefaultsReflectionHelper.GetConstValue("TouchBehaviorDefaults", "HoveredOpacity"));
	}

	[Fact]
	public void TouchBehaviorDefaults_PressedOpacity_Is1()
	{
		Assert.Equal(1d, DefaultsReflectionHelper.GetConstValue("TouchBehaviorDefaults", "PressedOpacity"));
	}

	[Fact]
	public void TouchBehaviorDefaults_DefaultOpacity_Is1()
	{
		Assert.Equal(1d, DefaultsReflectionHelper.GetConstValue("TouchBehaviorDefaults", "DefaultOpacity"));
	}

	[Fact]
	public void TouchBehaviorDefaults_HoveredScale_Is1()
	{
		Assert.Equal(1d, DefaultsReflectionHelper.GetConstValue("TouchBehaviorDefaults", "HoveredScale"));
	}

	[Fact]
	public void TouchBehaviorDefaults_PressedScale_Is1()
	{
		Assert.Equal(1d, DefaultsReflectionHelper.GetConstValue("TouchBehaviorDefaults", "PressedScale"));
	}

	[Fact]
	public void TouchBehaviorDefaults_DefaultScale_Is1()
	{
		Assert.Equal(1d, DefaultsReflectionHelper.GetConstValue("TouchBehaviorDefaults", "DefaultScale"));
	}

	[Fact]
	public void TouchBehaviorDefaults_LongPressDuration_Is500()
	{
		Assert.Equal(500, DefaultsReflectionHelper.GetConstValue("TouchBehaviorDefaults", "LongPressDuration"));
	}

	[Fact]
	public void TouchBehaviorDefaults_IsEnabled_IsTrue()
	{
		Assert.Equal(true, DefaultsReflectionHelper.GetConstValue("TouchBehaviorDefaults", "IsEnabled"));
	}

	[Fact]
	public void TouchBehaviorDefaults_DisallowTouchThreshold_IsZero()
	{
		Assert.Equal(0, DefaultsReflectionHelper.GetConstValue("TouchBehaviorDefaults", "DisallowTouchThreshold"));
	}

	[Fact]
	public void TouchBehaviorDefaults_ShouldMakeChildrenInputTransparent_IsTrue()
	{
		Assert.Equal(true, DefaultsReflectionHelper.GetConstValue("TouchBehaviorDefaults", "ShouldMakeChildrenInputTransparent"));
	}

	[Fact]
	public void TouchBehaviorDefaults_CurrentTouchState_IsDefault()
	{
		Assert.Equal(TouchState.Default, DefaultsReflectionHelper.GetConstValue("TouchBehaviorDefaults", "CurrentTouchState"));
	}

	[Fact]
	public void TouchBehaviorDefaults_CurrentTouchStatus_IsCompleted()
	{
		Assert.Equal(TouchStatus.Completed, DefaultsReflectionHelper.GetConstValue("TouchBehaviorDefaults", "CurrentTouchStatus"));
	}

	[Fact]
	public void TouchBehaviorDefaults_CurrentHoverState_IsDefault()
	{
		Assert.Equal(HoverState.Default, DefaultsReflectionHelper.GetConstValue("TouchBehaviorDefaults", "CurrentHoverState"));
	}

	[Fact]
	public void TouchBehaviorDefaults_CurrentHoverStatus_IsExited()
	{
		Assert.Equal(HoverStatus.Exited, DefaultsReflectionHelper.GetConstValue("TouchBehaviorDefaults", "CurrentHoverStatus"));
	}

	[Fact]
	public void TouchBehaviorDefaults_CurrentInteractionStatus_IsCompleted()
	{
		Assert.Equal(TouchInteractionStatus.Completed, DefaultsReflectionHelper.GetConstValue("TouchBehaviorDefaults", "CurrentInteractionStatus"));
	}

	[Fact]
	public void TouchBehaviorDefaults_DefaultBackgroundColor_IsTransparent()
	{
		Assert.Equal(Colors.Transparent, DefaultsReflectionHelper.GetPropertyValue("TouchBehaviorDefaults", "DefaultBackgroundColor"));
	}

	[Fact]
	public void TouchBehaviorDefaults_HoveredBackgroundColor_IsTransparent()
	{
		Assert.Equal(Colors.Transparent, DefaultsReflectionHelper.GetPropertyValue("TouchBehaviorDefaults", "HoveredBackgroundColor"));
	}

	[Fact]
	public void TouchBehaviorDefaults_PressedBackgroundColor_IsTransparent()
	{
		Assert.Equal(Colors.Transparent, DefaultsReflectionHelper.GetPropertyValue("TouchBehaviorDefaults", "PressedBackgroundColor"));
	}

	[Fact]
	public void TouchBehaviorDefaults_DefaultAnimationDuration_IsZero()
	{
		Assert.Equal(0, DefaultsReflectionHelper.GetConstValue("TouchBehaviorDefaults", "DefaultAnimationDuration"));
	}

	[Fact]
	public void TouchBehaviorDefaults_HoveredAnimationDuration_IsZero()
	{
		Assert.Equal(0, DefaultsReflectionHelper.GetConstValue("TouchBehaviorDefaults", "HoveredAnimationDuration"));
	}

	[Fact]
	public void TouchBehaviorDefaults_PressedAnimationDuration_IsZero()
	{
		Assert.Equal(0, DefaultsReflectionHelper.GetConstValue("TouchBehaviorDefaults", "PressedAnimationDuration"));
	}
}

public class UniformItemLayoutDefaultsTests
{
	[Fact]
	public void UniformItemLayoutDefaults_MaxRows_IsIntMaxValue()
	{
		Assert.Equal(int.MaxValue, DefaultsReflectionHelper.GetConstValue("UniformItemLayoutDefaults", "MaxRows"));
	}

	[Fact]
	public void UniformItemLayoutDefaults_MaxColumns_IsIntMaxValue()
	{
		Assert.Equal(int.MaxValue, DefaultsReflectionHelper.GetConstValue("UniformItemLayoutDefaults", "MaxColumns"));
	}
}

public class UriValidationBehaviorDefaultsTests
{
	[Fact]
	public void UriValidationBehaviorDefaults_UriKind_IsRelativeOrAbsolute()
	{
		Assert.Equal(UriKind.RelativeOrAbsolute, DefaultsReflectionHelper.GetPropertyValue("UriValidationBehaviorDefaults", "UriKind"));
	}
}

public class UserStoppedTypingBehaviorDefaultsTests
{
	[Fact]
	public void UserStoppedTypingBehaviorDefaults_StoppedTypingTimeThreshold_Is1000()
	{
		Assert.Equal(1000, DefaultsReflectionHelper.GetConstValue("UserStoppedTypingBehaviorDefaults", "StoppedTypingTimeThreshold"));
	}

	[Fact]
	public void UserStoppedTypingBehaviorDefaults_MinimumLengthThreshold_IsZero()
	{
		Assert.Equal(0, DefaultsReflectionHelper.GetConstValue("UserStoppedTypingBehaviorDefaults", "MinimumLengthThreshold"));
	}

	[Fact]
	public void UserStoppedTypingBehaviorDefaults_ShouldDismissKeyboardAutomatically_IsFalse()
	{
		Assert.Equal(false, DefaultsReflectionHelper.GetConstValue("UserStoppedTypingBehaviorDefaults", "ShouldDismissKeyboardAutomatically"));
	}
}