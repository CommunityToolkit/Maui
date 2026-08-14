using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class RangeSliderTests : BaseViewTest
{
	[Fact]
	public void Defaults_ShouldBeCorrect()
	{
		RangeSlider rangeSlider = new();
		rangeSlider.MinimumValue.Should().Be(RangeSliderDefaults.MinimumValue);
		rangeSlider.MaximumValue.Should().Be(RangeSliderDefaults.MaximumValue);
		rangeSlider.LowerValue.Should().Be(RangeSliderDefaults.LowerValue);
		rangeSlider.UpperValue.Should().Be(RangeSliderDefaults.UpperValue);
		rangeSlider.StepSize.Should().Be(RangeSliderDefaults.StepSize);
		rangeSlider.LowerThumbColor.Should().Be(RangeSliderDefaults.LowerThumbColor);
		rangeSlider.UpperThumbColor.Should().Be(RangeSliderDefaults.UpperThumbColor);
		rangeSlider.InnerTrackColor.Should().Be(RangeSliderDefaults.InnerTrackColor);
		rangeSlider.InnerTrackSize.Should().Be(RangeSliderDefaults.InnerTrackSize);
		rangeSlider.InnerTrackCornerRadius.Should().Be(RangeSliderDefaults.InnerTrackCornerRadius);
		rangeSlider.OuterTrackColor.Should().Be(RangeSliderDefaults.OuterTrackColor);
		rangeSlider.OuterTrackSize.Should().Be(RangeSliderDefaults.OuterTrackSize);
		rangeSlider.OuterTrackCornerRadius.Should().Be(RangeSliderDefaults.OuterTrackCornerRadius);
	}

	[Theory]
	[InlineData(-50, 50)]
	[InlineData(50, -50)]
	[InlineData(0, 100)]
	[InlineData(100, 0)]
	[InlineData(100, 200)]
	[InlineData(200, 100)]
	public void MinimumMaximumValues_ShouldStickToContract(double min, double max)
	{
		RangeSlider rangeSlider = new(true)
		{
			MinimumValue = min,
			MaximumValue = max,
		};
		rangeSlider.IsClampingEnabled.Should().BeTrue();
		rangeSlider.MinimumValue.Should().Be(min);
		rangeSlider.MaximumValue.Should().Be(max);
	}

	[Theory]
	[InlineData(0, 100, 70, 30, 30)]
	[InlineData(0, 100, 70, 80, 70)]
	[InlineData(0, 100, 170, 80, 80)]
	[InlineData(0, 100, 170, 180, 100)]
	public void LowerValue_ShouldBeClampedToBounds(double min, double max, double upper, double lower, double expectedLower)
	{
		RangeSlider rangeSlider = new(true)
		{
			MinimumValue = min,
			MaximumValue = max,
			UpperValue = max,
			LowerValue = min,
		};
		rangeSlider.IsClampingEnabled.Should().BeTrue();
		rangeSlider.UpperValue = upper;
		rangeSlider.LowerValue = lower;
		rangeSlider.MinimumValue.Should().Be(min);
		rangeSlider.MaximumValue.Should().Be(max);
		rangeSlider.UpperValue.Should().Be(Math.Min(upper, max));
		rangeSlider.LowerValue.Should().Be(expectedLower);
	}

	[Theory]
	[InlineData(0, 100, 30, 70, 70)]
	[InlineData(0, 100, 30, 0, 30)]
	[InlineData(0, 100, 30, 170, 100)]
	public void UpperValue_ShouldBeClampedToBounds(double min, double max, double lower, double upper, double expectedUpper)
	{
		RangeSlider rangeSlider = new(true)
		{
			MinimumValue = min,
			MaximumValue = max,
			LowerValue = min,
			UpperValue = max,
		};
		rangeSlider.IsClampingEnabled.Should().BeTrue();
		rangeSlider.LowerValue = lower;
		rangeSlider.UpperValue = upper;
		rangeSlider.MinimumValue.Should().Be(min);
		rangeSlider.MaximumValue.Should().Be(max);
		rangeSlider.LowerValue.Should().Be(Math.Max(lower, min));
		rangeSlider.UpperValue.Should().Be(expectedUpper);
	}
}