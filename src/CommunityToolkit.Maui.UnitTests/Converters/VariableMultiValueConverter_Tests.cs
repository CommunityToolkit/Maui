using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class VariableMultiValueConverter_Tests : BaseTest
{
	[Theory]
	[InlineData(new object[] { true, true, true }, MultiBindingCondition.None, false)]
	[InlineData(new object[] { true, false, true }, MultiBindingCondition.None, false)]
	[InlineData(new object[] { false, false, false }, MultiBindingCondition.None, true)]
	[InlineData(new object[] { true, true, true }, MultiBindingCondition.All, true)]
	[InlineData(new object[] { true, false, true }, MultiBindingCondition.All, false)]
	[InlineData(new object[] { false, false, false }, MultiBindingCondition.All, false)]
	[InlineData(new object[] { true, true, true }, MultiBindingCondition.Any, true)]
	[InlineData(new object[] { false, false, false }, MultiBindingCondition.Any, false)]
	[InlineData(new object[] { false, true, false }, MultiBindingCondition.Any, true)]
	[InlineData(new object[] { true, true, true }, MultiBindingCondition.Exact, true, 3)]
	[InlineData(new object[] { false, false, false }, MultiBindingCondition.Exact, false, 1)]
	[InlineData(new object[] { false, true, false }, MultiBindingCondition.Exact, true, 1)]
	[InlineData(new object[] { true, true, true }, MultiBindingCondition.GreaterThan, true, 2)]
	[InlineData(new object[] { false, false, false }, MultiBindingCondition.GreaterThan, false, 1)]
	[InlineData(new object[] { false, true, false }, MultiBindingCondition.GreaterThan, true, 0)]
	[InlineData(new object[] { true, true, true }, MultiBindingCondition.LessThan, false, 2)]
	[InlineData(new object[] { false, false, false }, MultiBindingCondition.LessThan, true, 1)]
	[InlineData(new object[] { false, true, false }, MultiBindingCondition.LessThan, true, 2)]
	[InlineData(null, MultiBindingCondition.All, false)]
	[InlineData(null, MultiBindingCondition.Any, false)]
	[InlineData(null, MultiBindingCondition.Exact, false)]
	[InlineData(null, MultiBindingCondition.GreaterThan, false)]
	[InlineData(null, MultiBindingCondition.LessThan, false)]
	[InlineData(null, MultiBindingCondition.None, false)]
	public void VariableMultiConverter(object[] value, MultiBindingCondition type, object expectedResult, int count = 0)
	{
		var variableMultiConverter = new VariableMultiValueConverter() { ConditionType = type, Count = count };
		var result = variableMultiConverter.Convert(value, typeof(bool), null, CultureInfo.CurrentCulture);
		Assert.Equal(result, expectedResult);
	}
}