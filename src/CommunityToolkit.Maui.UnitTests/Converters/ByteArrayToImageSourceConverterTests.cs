using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ByteArrayToImageSourceConverterTests : BaseConverterTest<ByteArrayToImageSourceConverter>
{
	public static IReadOnlyList<object[]> NonImageStreamData { get; } =
	[
		[3], // primitive type
		[DateTime.UtcNow], // Struct
		[new object()] // objects
	];

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ByteArrayToImageSourceConverter()
	{
		var byteArray = new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };

		var byteArrayToImageSourceConverter = new ByteArrayToImageSourceConverter();

		var convertFromResult = (StreamImageSource)byteArrayToImageSourceConverter.ConvertFrom(byteArray);
		var convertResult = (StreamImageSource)(((ICommunityToolkitValueConverter)byteArrayToImageSourceConverter).Convert(byteArray, typeof(StreamImageSource), null, CultureInfo.CurrentCulture) ?? throw new InvalidOperationException());

		var convertFromResultStream = await GetStreamFromImageSource(convertFromResult, CancellationToken.None);
		var convertResultStream = await GetStreamFromImageSource(convertResult, CancellationToken.None);

		Assert.True(StreamEquals(convertFromResultStream, new MemoryStream(byteArray)));
		Assert.True(StreamEquals(convertResultStream, new MemoryStream(byteArray)));
	}

	[Theory]
	[InlineData("Random String Value")]
	[InlineData(3)]
	[InlineData(5.5)]
	[InlineData('c')]
	[InlineData(true)]
	public void InvalidConverterValuesReturnsNull(object value)
	{
		var byteArrayToImageSourceConverter = new ByteArrayToImageSourceConverter();

		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)byteArrayToImageSourceConverter).Convert(value, typeof(ImageSource), null, CultureInfo.CurrentCulture));
	}

	[Fact]
	public void ConvertImageSourceBackToByteArray()
	{
		var byteArray = new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };

		var byteArrayToImageSourceConverter = new ByteArrayToImageSourceConverter();

		var convertBackResult = (byte[]?)((ICommunityToolkitValueConverter)byteArrayToImageSourceConverter).ConvertBack((StreamImageSource)ImageSource.FromStream(() => new MemoryStream(byteArray)), typeof(byte[]), null, CultureInfo.CurrentCulture);
		var convertBackToResult = byteArrayToImageSourceConverter.ConvertBackTo((StreamImageSource)ImageSource.FromStream(() => new MemoryStream(byteArray)));

		Assert.NotNull(convertBackResult);
		Assert.NotNull(convertBackToResult);

		Assert.NotEmpty(convertBackResult);
		Assert.NotEmpty(convertBackToResult);

		Assert.Equal(byteArray, convertBackResult);
		Assert.Equal(byteArray, convertBackToResult);
	}

	[Fact]
	public void ConvertEmptyImageSourceBackToByteArray()
	{
		var byteArray = Array.Empty<byte>();

		var memoryStream = new MemoryStream(byteArray);

		var expectedValue = (StreamImageSource)ImageSource.FromStream(() => memoryStream);

		var byteArrayToImageSourceConverter = new ByteArrayToImageSourceConverter();

		var convertBackResult = (byte[]?)((ICommunityToolkitValueConverter)byteArrayToImageSourceConverter).ConvertBack(expectedValue, typeof(byte[]), null, CultureInfo.CurrentCulture);
		var convertBackToResult = byteArrayToImageSourceConverter.ConvertBackTo(expectedValue);

		Assert.NotNull(convertBackResult);
		Assert.NotNull(convertBackToResult);

		Assert.Empty(convertBackResult);
		Assert.Empty(convertBackToResult);

		Assert.Equal(byteArray, convertBackResult);
		Assert.Equal(byteArray, convertBackToResult);
	}

	[Fact]
	public void ConvertNullImageSourceBackToByteArray()
	{
		var nullStream = (StreamImageSource)ImageSource.FromStream(() => null);

		var byteArrayToImageSourceConverter = new ByteArrayToImageSourceConverter();

		var convertBackToResult = byteArrayToImageSourceConverter.ConvertBackTo(nullStream);
		var convertBackResult = ((ICommunityToolkitValueConverter)byteArrayToImageSourceConverter).ConvertBack(nullStream, typeof(byte[]), null, CultureInfo.CurrentCulture);

		Assert.Null(convertBackToResult);
		Assert.Null(convertBackResult);
	}

	[Fact]
	public void ConvertingBackNullResultsInNull()
	{
		var byteArrayToImageSourceConverter = new ByteArrayToImageSourceConverter();

		var convertBackResult = (byte[]?)((ICommunityToolkitValueConverter)byteArrayToImageSourceConverter).ConvertBack(null, typeof(byte[]), null, CultureInfo.CurrentCulture);
		var convertBackToResult = byteArrayToImageSourceConverter.ConvertBackTo(null);

		Assert.Null(convertBackResult);
		Assert.Null(convertBackToResult);
	}

	[Theory]
	[MemberData(nameof(NonImageStreamData))]
	public void ConvertingNonImageSourceThrows(object value)
	{
		var byteArrayToImageSourceConverter = new ByteArrayToImageSourceConverter();

		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)byteArrayToImageSourceConverter).ConvertBack(value, typeof(StreamImageSource), null, CultureInfo.CurrentCulture));
	}

	[Fact]
	public void ByteArrayToImageSourceConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ByteArrayToImageSourceConverter()).Convert(new byte(), null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ByteArrayToImageSourceConverter()).ConvertBack(ImageSource.FromStream(() => Stream.Null), null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}