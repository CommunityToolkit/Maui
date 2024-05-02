using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class MultiConverterTests : BaseTest
{
	public static IReadOnlyList<object[]> Data { get; } =
	[
		[new List<MultiConverterParameter> { new() { Value = "Param 1", }, new() { Value = "Param 2", } }],
	];

	[Theory]
	[MemberData(nameof(Data))]
	public void MultiConverter(object value)
	{
		var multiConverter = new MultiConverter();

		var result = multiConverter.Convert(value, typeof(object), null, CultureInfo.CurrentCulture);

		Assert.Equal(result, value);
	}

	[Fact]
	public void ChainingConvertersOfDifferentTargetTypes()
	{
		var multiConverter = new MultiConverter
		{
			new TextCaseConverter(),
			new IsEqualConverter()
		};

		var multiParams = new List<MultiConverterParameter>
		{
			new MultiConverterParameter { ConverterType = typeof(TextCaseConverter), Value = TextCaseType.Upper },
			new MultiConverterParameter { ConverterType = typeof(IsEqualConverter), Value = "MAUI" }
		};

		var falseResult = (bool?)multiConverter.Convert("JOHN", typeof(bool), multiParams, CultureInfo.CurrentCulture);
		var trueResult = (bool?)multiConverter.Convert("MAUI", typeof(bool), multiParams, CultureInfo.CurrentCulture);

		Assert.False(falseResult);
		Assert.True(trueResult);
	}

	[Fact]
	public void EnsureICommunityToolkitValueConverterThrowsExceptions()
	{
		ICommunityToolkitValueConverter communityToolkitValueConverter = new MultiConverter();

		// ICommunityToolkitValueConverter.DefaultConvertBackReturnValue GET
		Assert.Throws<NotSupportedException>(() =>
		{
			var value = communityToolkitValueConverter.DefaultConvertBackReturnValue;
		});

		// ICommunityToolkitValueConverter.DefaultConvertReturnValue GET
		Assert.Throws<NotSupportedException>(() =>
		{
			var value = communityToolkitValueConverter.DefaultConvertReturnValue;
		});

		// ICommunityToolkitValueConverter.FromType GET
		Assert.Throws<NotSupportedException>(() =>
		{
			var value = communityToolkitValueConverter.FromType;
		});

		// ICommunityToolkitValueConverter.ToType GET
		Assert.Throws<NotSupportedException>(() =>
		{
			var value = communityToolkitValueConverter.ToType;
		});
	}
}