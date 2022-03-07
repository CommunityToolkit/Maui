using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class BoolToObjectConverter_Tests : BaseTest
{
	public const string TrueTestObject = nameof(TrueTestObject);
	public const string FalseTestObject = nameof(FalseTestObject);

	[Theory]
	[InlineData(true, TrueTestObject, FalseTestObject, TrueTestObject)]
	[InlineData(false, TrueTestObject, FalseTestObject, FalseTestObject)]
	public void BoolToObjectConvert(bool value, object trueObject, object falseObject, object expectedResult)
	{
		var boolObjectConverter = new BoolToObjectConverter();
		boolObjectConverter.TrueObject = trueObject;
		boolObjectConverter.FalseObject = falseObject;

		var result = boolObjectConverter.Convert(value, typeof(object), null, CultureInfo.CurrentCulture);

		Assert.NotNull(result);
		Assert.Equal(result, expectedResult);
	}

	[Theory]
	[InlineData(TrueTestObject, TrueTestObject, FalseTestObject, true)]
	[InlineData(FalseTestObject, TrueTestObject, FalseTestObject, false)]
	public void BoolToObjectConvertBack(object value, object trueObject, object falseObject, bool expectedResult)
	{
		var boolObjectConverter = new BoolToObjectConverter();
		boolObjectConverter.TrueObject = trueObject;
		boolObjectConverter.FalseObject = falseObject;

		var result = (bool)boolObjectConverter.ConvertBack(value, typeof(object), null, CultureInfo.CurrentCulture);

		Assert.Equal(result, expectedResult);
	}

	[Theory]
	[InlineData("")]
	public void BoolToObjectInValidValuesThrowArgumenException(object value)
	{
		var boolObjectConverter = new BoolToObjectConverter();
		Assert.Throws<ArgumentException>(() => boolObjectConverter.Convert(value, typeof(object), null, CultureInfo.CurrentCulture));
	}
}