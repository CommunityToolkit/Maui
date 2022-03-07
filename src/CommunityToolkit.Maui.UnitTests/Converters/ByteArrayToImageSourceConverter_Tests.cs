using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ByteArrayToImageSourceConverter_Tests : BaseTest
{
	public static IReadOnlyList<object[]> NonImageStreamData { get; } = new[]
	{
		new object[] { 3 }, // primitive type
		new object[] { DateTime.UtcNow }, // Struct
		new object[] { new object() } // objects
	};

	[Fact]
	public async Task ByteArrayToImageSourceConverter()
	{
		var byteArray = new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };

		var memoryStream = new MemoryStream(byteArray);

		var expectedValue = ImageSource.FromStream(() => memoryStream);

		var byteArrayToImageSourceConverter = new ByteArrayToImageSourceConverter();

		var result = (ImageSource)byteArrayToImageSourceConverter.Convert(byteArray, typeof(ImageSource), null, CultureInfo.CurrentCulture);
		var streamResult = await GetStreamFromImageSource(result, CancellationToken.None);

		Assert.True(StreamEquals(streamResult, memoryStream));
	}

	[Theory]
	[InlineData("Random String Value")]
	public void InvalidConverterValuesReturnsNull(object value)
	{
		var byteArrayToImageSourceConverter = new ByteArrayToImageSourceConverter();

		Assert.Throws<ArgumentException>(() => byteArrayToImageSourceConverter.Convert(value, typeof(ImageSource), null, CultureInfo.CurrentCulture));
	}

	[Fact]
	public void ConvertImageSourceBackToByteArray()
	{
		var byteArray = new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };

		var memoryStream = new MemoryStream(byteArray);

		var expectedValue = (StreamImageSource)ImageSource.FromStream(() => memoryStream);

		var byteArrayToImageSourceConverter = new ByteArrayToImageSourceConverter();

		var result = (byte[]?)byteArrayToImageSourceConverter.ConvertBack(expectedValue, typeof(ImageSource), null, CultureInfo.CurrentCulture);

		Assert.NotNull(result);

		Assert.NotEmpty(result);
		Assert.Equal(result, byteArray);
	}

	[Fact]
	public void ConvertEmptyImageSourceBackToByteArray()
	{
		var byteArray = Array.Empty<byte>();

		var memoryStream = new MemoryStream(byteArray);

		var expectedValue = (StreamImageSource)ImageSource.FromStream(() => memoryStream);

		var byteArrayToImageSourceConverter = new ByteArrayToImageSourceConverter();

		var result = (byte[]?)byteArrayToImageSourceConverter.ConvertBack(expectedValue, typeof(ImageSource), null, CultureInfo.CurrentCulture);

		Assert.NotNull(result);

		Assert.Empty(result);
		Assert.Equal(result, byteArray);
	}

	[Fact]
	public void ConvertNullImageSourceBackToByteArray()
	{
		var expectedValue = (StreamImageSource)ImageSource.FromStream(() => null);

		var byteArrayToImageSourceConverter = new ByteArrayToImageSourceConverter();

		var result = byteArrayToImageSourceConverter.ConvertBack(expectedValue, typeof(ByteArrayToImageSourceConverter), null, CultureInfo.CurrentCulture);

		Assert.Null(result);
	}

	[Fact]
	public void ConvertingBackNullResultsInNull()
	{
		var byteArrayToImageSourceConverter = new ByteArrayToImageSourceConverter();

		var result = (byte[]?)byteArrayToImageSourceConverter.ConvertBack(null, typeof(ImageSource), null, CultureInfo.CurrentCulture);

		Assert.Null(result);
	}

	[Theory]
	[MemberData(nameof(NonImageStreamData))]
	public void ConvertingNonImageSourceThrows(object value)
	{
		var byteArrayToImageSourceConverter = new ByteArrayToImageSourceConverter();

		Assert.Throws<ArgumentException>(() => byteArrayToImageSourceConverter.ConvertBack(value, typeof(ImageSource), null, CultureInfo.CurrentCulture));
	}
}