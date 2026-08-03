using CommunityToolkit.Maui.Core;
using Xunit;

namespace CommunityToolkit.Maui.DeviceTests.Tests.Core;

public class AlertDefaultsTests
{
	[Fact]
	public void AlertDefaults_FontSize_Is14()
	{
		Assert.Equal(14d, AlertDefaults.FontSize);
	}

	[Fact]
	public void AlertDefaults_CharacterSpacing_IsZero()
	{
		Assert.Equal(0.0d, AlertDefaults.CharacterSpacing);
	}

	[Fact]
	public void AlertDefaults_ActionButtonText_IsOK()
	{
		Assert.Equal("OK", AlertDefaults.ActionButtonText);
	}

	[Fact]
	public void AlertDefaults_TextColor_IsBlack()
	{
		Assert.Equal(Colors.Black, AlertDefaults.TextColor);
	}

	[Fact]
	public void AlertDefaults_BackgroundColor_IsLightGray()
	{
		Assert.Equal(Colors.LightGray, AlertDefaults.BackgroundColor);
	}
}

public class AvatarViewDefaultsTests
{
	[Fact]
	public void AvatarViewDefaults_BorderWidth_Is1()
	{
		Assert.Equal(1d, AvatarViewDefaults.BorderWidth);
	}

	[Fact]
	public void AvatarViewDefaults_HeightRequest_Is48()
	{
		Assert.Equal(48d, AvatarViewDefaults.HeightRequest);
	}

	[Fact]
	public void AvatarViewDefaults_WidthRequest_Is48()
	{
		Assert.Equal(48d, AvatarViewDefaults.WidthRequest);
	}

	[Fact]
	public void AvatarViewDefaults_Text_IsQuestionMark()
	{
		Assert.Equal("?", AvatarViewDefaults.Text);
	}

	[Fact]
	public void AvatarViewDefaults_BorderColor_IsWhite()
	{
		Assert.Equal(Colors.White, AvatarViewDefaults.BorderColor);
	}

	[Fact]
	public void AvatarViewDefaults_CornerRadius_Is24()
	{
		Assert.Equal(new CornerRadius(24, 24, 24, 24), AvatarViewDefaults.CornerRadius);
	}

	[Fact]
	public void AvatarViewDefaults_Padding_Is1()
	{
		Assert.Equal(new Thickness(1), AvatarViewDefaults.Padding);
	}
}

public class DrawingViewDefaultsTests
{
	[Fact]
	public void DrawingViewDefaults_MinimumGranularity_Is5()
	{
		Assert.Equal(5, DrawingViewDefaults.MinimumGranularity);
	}

	[Fact]
	public void DrawingViewDefaults_LineWidth_Is5()
	{
		Assert.Equal(5f, DrawingViewDefaults.LineWidth);
	}

	[Fact]
	public void DrawingViewDefaults_ShouldSmoothPathWhenDrawn_IsTrue()
	{
		Assert.True(DrawingViewDefaults.ShouldSmoothPathWhenDrawn);
	}

	[Fact]
	public void DrawingViewDefaults_IsMultiLineModeEnabled_IsFalse()
	{
		Assert.False(DrawingViewDefaults.IsMultiLineModeEnabled);
	}

	[Fact]
	public void DrawingViewDefaults_ShouldClearOnFinish_IsFalse()
	{
		Assert.False(DrawingViewDefaults.ShouldClearOnFinish);
	}

	[Fact]
	public void DrawingViewDefaults_LineColor_IsBlack()
	{
		Assert.Equal(Colors.Black, DrawingViewDefaults.LineColor);
	}

	[Fact]
	public void DrawingViewDefaults_BackgroundColor_IsLightGray()
	{
		Assert.Equal(Colors.LightGray, DrawingViewDefaults.BackgroundColor);
	}
}

public class DockLayoutDefaultsTests
{
	[Fact]
	public void DockLayoutDefaults_DockPosition_IsNone()
	{
		Assert.Equal(DockPosition.None, DockLayoutDefaults.DockPosition);
	}
}

public class ExpanderDefaultsTests
{
	[Fact]
	public void ExpanderDefaults_Direction_IsDown()
	{
		Assert.Equal(ExpandDirection.Down, ExpanderDefaults.Direction);
	}
}

public class FadeAnimationDefaultsTests
{
	[Fact]
	public void FadeAnimationDefaults_Length_Is300()
	{
		Assert.Equal(300u, FadeAnimationDefaults.Length);
	}

	[Fact]
	public void FadeAnimationDefaults_Opacity_Is0Point3()
	{
		Assert.Equal(0.3, FadeAnimationDefaults.Opacity);
	}
}

public class ImageTouchBehaviorDefaultsTests
{
	[Fact]
	public void ImageTouchBehaviorDefaults_DefaultBackgroundImageSource_IsNull()
	{
		Assert.Null(ImageTouchBehaviorDefaults.DefaultBackgroundImageSource);
	}

	[Fact]
	public void ImageTouchBehaviorDefaults_HoveredBackgroundImageSource_IsNull()
	{
		Assert.Null(ImageTouchBehaviorDefaults.HoveredBackgroundImageSource);
	}

	[Fact]
	public void ImageTouchBehaviorDefaults_PressedBackgroundImageSource_IsNull()
	{
		Assert.Null(ImageTouchBehaviorDefaults.PressedBackgroundImageSource);
	}

	[Fact]
	public void ImageTouchBehaviorDefaults_DefaultBackgroundImageAspect_IsAspectFit()
	{
		Assert.Equal(Aspect.AspectFit, ImageTouchBehaviorDefaults.DefaultBackgroundImageAspect);
	}

	[Fact]
	public void ImageTouchBehaviorDefaults_ShouldSetImageOnAnimationEnd_IsFalse()
	{
		Assert.False(ImageTouchBehaviorDefaults.ShouldSetImageOnAnimationEnd);
	}
}

public class MaxLengthReachedBehaviorDefaultsTests
{
	[Fact]
	public void MaxLengthReachedBehaviorDefaults_ShouldDismissKeyboardAutomatically_IsFalse()
	{
		Assert.False(MaxLengthReachedBehaviorDefaults.ShouldDismissKeyboardAutomatically);
	}

	[Fact]
	public void MaxLengthReachedBehaviorDefaults_Command_IsNull()
	{
		Assert.Null(MaxLengthReachedBehaviorDefaults.Command);
	}
}

public class MultiValidationBehaviorDefaultsTests
{
	[Fact]
	public void MultiValidationBehaviorDefaults_Errors_IsNull()
	{
		Assert.Null(MultiValidationBehaviorDefaults.Errors);
	}

	[Fact]
	public void MultiValidationBehaviorDefaults_Error_IsNull()
	{
		Assert.Null(MultiValidationBehaviorDefaults.Error);
	}
}

public class NumericValidationBehaviorDefaultsTests
{
	[Fact]
	public void NumericValidationBehaviorDefaults_MinimumValue_IsNegativeInfinity()
	{
		Assert.Equal(double.NegativeInfinity, NumericValidationBehaviorDefaults.MinimumValue);
	}

	[Fact]
	public void NumericValidationBehaviorDefaults_MaximumValue_IsPositiveInfinity()
	{
		Assert.Equal(double.PositiveInfinity, NumericValidationBehaviorDefaults.MaximumValue);
	}

	[Fact]
	public void NumericValidationBehaviorDefaults_MinimumDecimalPlaces_IsZero()
	{
		Assert.Equal(0, NumericValidationBehaviorDefaults.MinimumDecimalPlaces);
	}

	[Fact]
	public void NumericValidationBehaviorDefaults_MaximumDecimalPlaces_IsIntMaxValue()
	{
		Assert.Equal(int.MaxValue, NumericValidationBehaviorDefaults.MaximumDecimalPlaces);
	}
}

public class ProgressBarAnimationBehaviorDefaultsTests
{
	[Fact]
	public void ProgressBarAnimationBehaviorDefaults_Progress_IsZero()
	{
		Assert.Equal(0.0, ProgressBarAnimationBehaviorDefaults.Progress);
	}

	[Fact]
	public void ProgressBarAnimationBehaviorDefaults_Length_Is500()
	{
		Assert.Equal(500u, ProgressBarAnimationBehaviorDefaults.Length);
	}

	[Fact]
	public void ProgressBarAnimationBehaviorDefaults_Easing_IsLinear()
	{
		Assert.Equal(Easing.Linear, ProgressBarAnimationBehaviorDefaults.Easing);
	}
}

public class RatingViewDefaultsTests
{
	[Fact]
	public void RatingViewDefaults_Rating_IsZero()
	{
		Assert.Equal(0.0, RatingViewDefaults.Rating);
	}

	[Fact]
	public void RatingViewDefaults_IsReadOnly_IsFalse()
	{
		Assert.False(RatingViewDefaults.IsReadOnly);
	}

	[Fact]
	public void RatingViewDefaults_ItemShapeSize_Is20()
	{
		Assert.Equal(20.0, RatingViewDefaults.ItemShapeSize);
	}

	[Fact]
	public void RatingViewDefaults_MaximumRating_Is5()
	{
		Assert.Equal(5, RatingViewDefaults.MaximumRating);
	}

	[Fact]
	public void RatingViewDefaults_MaximumRatingLimit_Is10()
	{
		Assert.Equal(10, RatingViewDefaults.MaximumRatingLimit);
	}

	[Fact]
	public void RatingViewDefaults_ShapeBorderThickness_Is1()
	{
		Assert.Equal(1.0, RatingViewDefaults.ShapeBorderThickness);
	}

	[Fact]
	public void RatingViewDefaults_Spacing_Is10()
	{
		Assert.Equal(10.0, RatingViewDefaults.Spacing);
	}

	[Fact]
	public void RatingViewDefaults_Shape_IsStar()
	{
		Assert.Equal(RatingViewShape.Star, RatingViewDefaults.Shape);
	}

	[Fact]
	public void RatingViewDefaults_FillOption_IsShape()
	{
		Assert.Equal(RatingViewFillOption.Shape, RatingViewDefaults.FillOption);
	}

	[Fact]
	public void RatingViewDefaults_EmptyShapeColor_IsTransparent()
	{
		Assert.Equal(Colors.Transparent, RatingViewDefaults.EmptyShapeColor);
	}

	[Fact]
	public void RatingViewDefaults_FillColor_IsYellow()
	{
		Assert.Equal(Colors.Yellow, RatingViewDefaults.FillColor);
	}

	[Fact]
	public void RatingViewDefaults_ShapeBorderColor_IsGrey()
	{
		Assert.Equal(Colors.Grey, RatingViewDefaults.ShapeBorderColor);
	}

	[Fact]
	public void RatingViewDefaults_ShapePadding_IsZero()
	{
		Assert.Equal(new Thickness(0), RatingViewDefaults.ShapePadding);
	}
}

public class RequiredStringValidationBehaviorDefaultsTests
{
	[Fact]
	public void RequiredStringValidationBehaviorDefaults_RequiredString_IsNull()
	{
		Assert.Null(RequiredStringValidationBehaviorDefaults.RequiredString);
	}

	[Fact]
	public void RequiredStringValidationBehaviorDefaults_ExactMatch_IsTrue()
	{
		Assert.True(RequiredStringValidationBehaviorDefaults.ExactMatch);
	}
}

public class SpeechToTextOptionsDefaultsTests
{
	[Fact]
	public void SpeechToTextOptionsDefaults_ShouldReportPartialResults_IsTrue()
	{
		Assert.True(SpeechToTextOptionsDefaults.ShouldReportPartialResults);
	}

	[Fact]
	public void SpeechToTextOptionsDefaults_AutoStopSilenceTimeout_IsTimeSpanMaxValue()
	{
		Assert.Equal(TimeSpan.MaxValue, SpeechToTextOptionsDefaults.AutoStopSilenceTimeout);
	}
}

public class StateViewDefaultsTests
{
	[Fact]
	public void StateViewDefaults_StateKey_IsEmpty()
	{
		Assert.Equal(StateViewDefaults.StateKey, string.Empty);
	}
}

public class TouchBehaviorDefaultsTests
{
	[Fact]
	public void TouchBehaviorDefaults_HoveredOpacity_Is1()
	{
		Assert.Equal(1d, TouchBehaviorDefaults.HoveredOpacity);
	}

	[Fact]
	public void TouchBehaviorDefaults_PressedOpacity_Is1()
	{
		Assert.Equal(1d, TouchBehaviorDefaults.PressedOpacity);
	}

	[Fact]
	public void TouchBehaviorDefaults_DefaultOpacity_Is1()
	{
		Assert.Equal(1d, TouchBehaviorDefaults.DefaultOpacity);
	}

	[Fact]
	public void TouchBehaviorDefaults_HoveredScale_Is1()
	{
		Assert.Equal(1d, TouchBehaviorDefaults.HoveredScale);
	}

	[Fact]
	public void TouchBehaviorDefaults_PressedScale_Is1()
	{
		Assert.Equal(1d, TouchBehaviorDefaults.PressedScale);
	}

	[Fact]
	public void TouchBehaviorDefaults_DefaultScale_Is1()
	{
		Assert.Equal(1d, TouchBehaviorDefaults.DefaultScale);
	}

	[Fact]
	public void TouchBehaviorDefaults_LongPressDuration_Is500()
	{
		Assert.Equal(500, TouchBehaviorDefaults.LongPressDuration);
	}

	[Fact]
	public void TouchBehaviorDefaults_IsEnabled_IsTrue()
	{
		Assert.True(TouchBehaviorDefaults.IsEnabled);
	}

	[Fact]
	public void TouchBehaviorDefaults_DisallowTouchThreshold_IsZero()
	{
		Assert.Equal(0, TouchBehaviorDefaults.DisallowTouchThreshold);
	}

	[Fact]
	public void TouchBehaviorDefaults_ShouldMakeChildrenInputTransparent_IsTrue()
	{
		Assert.True(TouchBehaviorDefaults.ShouldMakeChildrenInputTransparent);
	}

	[Fact]
	public void TouchBehaviorDefaults_CurrentTouchState_IsDefault()
	{
		Assert.Equal(TouchState.Default, TouchBehaviorDefaults.CurrentTouchState);
	}

	[Fact]
	public void TouchBehaviorDefaults_CurrentTouchStatus_IsCompleted()
	{
		Assert.Equal(TouchStatus.Completed, TouchBehaviorDefaults.CurrentTouchStatus);
	}

	[Fact]
	public void TouchBehaviorDefaults_CurrentHoverState_IsDefault()
	{
		Assert.Equal(HoverState.Default, TouchBehaviorDefaults.CurrentHoverState);
	}

	[Fact]
	public void TouchBehaviorDefaults_CurrentHoverStatus_IsExited()
	{
		Assert.Equal(HoverStatus.Exited, TouchBehaviorDefaults.CurrentHoverStatus);
	}

	[Fact]
	public void TouchBehaviorDefaults_CurrentInteractionStatus_IsCompleted()
	{
		Assert.Equal(TouchInteractionStatus.Completed, TouchBehaviorDefaults.CurrentInteractionStatus);
	}

	[Fact]
	public void TouchBehaviorDefaults_DefaultBackgroundColor_IsTransparent()
	{
		Assert.Equal(Colors.Transparent, TouchBehaviorDefaults.DefaultBackgroundColor);
	}

	[Fact]
	public void TouchBehaviorDefaults_HoveredBackgroundColor_IsTransparent()
	{
		Assert.Equal(Colors.Transparent, TouchBehaviorDefaults.HoveredBackgroundColor);
	}

	[Fact]
	public void TouchBehaviorDefaults_PressedBackgroundColor_IsTransparent()
	{
		Assert.Equal(Colors.Transparent, TouchBehaviorDefaults.PressedBackgroundColor);
	}

	[Fact]
	public void TouchBehaviorDefaults_DefaultAnimationDuration_IsZero()
	{
		Assert.Equal(0, TouchBehaviorDefaults.DefaultAnimationDuration);
	}

	[Fact]
	public void TouchBehaviorDefaults_HoveredAnimationDuration_IsZero()
	{
		Assert.Equal(0, TouchBehaviorDefaults.HoveredAnimationDuration);
	}

	[Fact]
	public void TouchBehaviorDefaults_PressedAnimationDuration_IsZero()
	{
		Assert.Equal(0, TouchBehaviorDefaults.PressedAnimationDuration);
	}
}

public class UniformItemLayoutDefaultsTests
{
	[Fact]
	public void UniformItemLayoutDefaults_MaxRows_IsIntMaxValue()
	{
		Assert.Equal(int.MaxValue, UniformItemLayoutDefaults.MaxRows);
	}

	[Fact]
	public void UniformItemLayoutDefaults_MaxColumns_IsIntMaxValue()
	{
		Assert.Equal(int.MaxValue, UniformItemLayoutDefaults.MaxColumns);
	}
}

public class UriValidationBehaviorDefaultsTests
{
	[Fact]
	public void UriValidationBehaviorDefaults_UriKind_IsRelativeOrAbsolute()
	{
		Assert.Equal(UriKind.RelativeOrAbsolute, UriValidationBehaviorDefaults.UriKind);
	}
}

public class UserStoppedTypingBehaviorDefaultsTests
{
	[Fact]
	public void UserStoppedTypingBehaviorDefaults_StoppedTypingTimeThreshold_Is1000()
	{
		Assert.Equal(1000, UserStoppedTypingBehaviorDefaults.StoppedTypingTimeThreshold);
	}

	[Fact]
	public void UserStoppedTypingBehaviorDefaults_MinimumLengthThreshold_IsZero()
	{
		Assert.Equal(0, UserStoppedTypingBehaviorDefaults.MinimumLengthThreshold);
	}

	[Fact]
	public void UserStoppedTypingBehaviorDefaults_ShouldDismissKeyboardAutomatically_IsFalse()
	{
		Assert.False(UserStoppedTypingBehaviorDefaults.ShouldDismissKeyboardAutomatically);
	}
}
