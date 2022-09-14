using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class StateToBooleanConverterTests : BaseOneWayConverterTest<StateToBooleanConverter>
{
	[Theory]
	[InlineData((LayoutState)int.MinValue)]
	[InlineData((LayoutState)(-1))]
	[InlineData((LayoutState)7)]
	[InlineData((LayoutState)int.MaxValue)]
	public void InvalidLayoutStateEnumThrowsNotSupportedException(LayoutState layoutState)
	{
		var stateToBooleanConverter = new StateToBooleanConverter();

		Assert.Throws<InvalidEnumArgumentException>(() => new StateToBooleanConverter { StateToCompare = layoutState });
		Assert.Throws<InvalidEnumArgumentException>(() => stateToBooleanConverter.StateToCompare = layoutState);
		Assert.Throws<InvalidEnumArgumentException>(() => ((ICommunityToolkitValueConverter)stateToBooleanConverter).Convert(LayoutState.None, typeof(bool), layoutState, null));
		Assert.Throws<InvalidEnumArgumentException>(() => stateToBooleanConverter.ConvertFrom(LayoutState.None, layoutState, null));
	}

	[Theory]
	[InlineData(LayoutState.Custom, LayoutState.Custom, true)]
	[InlineData(LayoutState.Custom, LayoutState.None, false)]
	public void StateToBooleanConverterWithParameter(LayoutState value, LayoutState comparedValue, bool expectedResult)
	{
		var stateToBooleanConverter = new StateToBooleanConverter();

		var convertResult = ((ICommunityToolkitValueConverter)stateToBooleanConverter).Convert(value, typeof(bool), comparedValue, CultureInfo.CurrentCulture);
		var convertFromResult = stateToBooleanConverter.ConvertFrom(value, comparedValue);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Fact]
	public void StateToBooleanConverterDefaultValues()
	{
		var stateToBooleanConverter = new StateToBooleanConverter();

		Assert.Equal(LayoutState.None, stateToBooleanConverter.StateToCompare);
	}

	[Theory]
	[InlineData(LayoutState.Custom, false)]
	[InlineData(LayoutState.Empty, false)]
	[InlineData(LayoutState.Error, false)]
	[InlineData(LayoutState.Loading, true)]
	[InlineData(LayoutState.None, false)]
	[InlineData(LayoutState.Saving, false)]
	[InlineData(LayoutState.Success, false)]
	public void StateToBooleanConverterWithExplicitType(LayoutState value, bool expectedResult)
	{
		var stateToBooleanConverter = new StateToBooleanConverter
		{
			StateToCompare = LayoutState.Loading
		};

		var convertResult = ((ICommunityToolkitValueConverter)stateToBooleanConverter).Convert(value, typeof(bool), null, CultureInfo.CurrentCulture);
		var convertFromResult = stateToBooleanConverter.ConvertFrom(value);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Fact]
	public void StateToBooleanConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new StateToBooleanConverter()).Convert(LayoutState.None, null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new StateToBooleanConverter()).ConvertBack(LayoutState.None, null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}