using System;
using System.Globalization;
using System.IO;
using CommunityToolkit.Maui.Converters;
using Microsoft.Maui.Controls;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ByteArrayToImageSourceConverter_Tests : BaseTest
{
    [Fact]
    public void ByteArrayToImageSourceConverter()
    {
        var byteArray = new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };

        var memoryStream = new MemoryStream(byteArray);

        var expectedValue = ImageSource.FromStream(() => memoryStream);

        var byteArrayToImageSourceConverter = new ByteArrayToImageSourceConverter();

        var result = (ImageSource)byteArrayToImageSourceConverter.Convert(byteArray, typeof(ByteArrayToImageSourceConverter), null, CultureInfo.CurrentCulture);

        Assert.True(StreamEquals(GetStreamFromImageSource(result), memoryStream));
    }

    [Theory]
    [InlineData("Random String Value")]
    public void InvalidConverterValuesReturnsNull(object value)
    {
        var byteArrayToImageSourceConverter = new ByteArrayToImageSourceConverter();

        Assert.Throws<ArgumentException>(() => byteArrayToImageSourceConverter.Convert(value, typeof(ByteArrayToImageSourceConverter), null, CultureInfo.CurrentCulture));
    }
}