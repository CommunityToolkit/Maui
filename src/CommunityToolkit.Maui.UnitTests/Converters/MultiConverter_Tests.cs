using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class MultiConverter_Tests : BaseTest
{
	public static IReadOnlyList<object[]> Data { get; } = new[]
	{
		new object[] { new List<MultiConverterParameter>() { { new MultiConverterParameter() { Value = "Param 1", } }, { new MultiConverterParameter() { Value = "Param 2", } } } },
	};

	[Theory]
	[MemberData(nameof(Data))]
	public void MultiConverter(object value)
	{
		var multiConverter = new MultiConverter();

		var result = multiConverter.Convert(value, typeof(object), null, CultureInfo.CurrentCulture);

		Assert.Equal(result, value);
	}
}