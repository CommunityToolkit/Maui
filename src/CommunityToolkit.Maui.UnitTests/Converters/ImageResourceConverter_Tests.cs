using System;
using System.Globalization;
using System.IO;
using CommunityToolkit.Maui.Converters;
using Microsoft.Maui.Controls;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ImageResourceConverter_Tests : BaseTest
{
	[Fact]
	public void ImageResourceConverter()
	{
		const string resourceToLoad = "CommunityToolkit.Maui.UnitTests.Resources.dotnet-bot.png";

		var expectedResource = ImageSource.FromResource(resourceToLoad);
		var expectedMemoryStream = GetStreamFromImageSource(expectedResource);

		var imageResourceConverter = new ImageResourceConverter();
		var result = (ImageSource)imageResourceConverter.Convert(resourceToLoad, typeof(ImageResourceConverter), null, CultureInfo.CurrentCulture);

		Assert.True(StreamEquals(GetStreamFromImageSource(result), expectedMemoryStream));
	}

	[Fact]
	public void NullValueReturnsNull()
	{
		var imageResourceConverter = new ImageResourceConverter();
		
		Assert.Null(imageResourceConverter.Convert(null, typeof(ImageResourceConverter), null, CultureInfo.CurrentCulture));
	}

	[Fact]
	public void ThrowsIfNotAString()
	{
		var imageResourceConverter = new ImageResourceConverter();

		Assert.Throws<ArgumentException>(() => imageResourceConverter.Convert(3, typeof(ImageResourceConverter), null, CultureInfo.CurrentCulture));
		Assert.Throws<ArgumentException>(() => imageResourceConverter.Convert(DateTime.UtcNow, typeof(ImageResourceConverter), null, CultureInfo.CurrentCulture));
		Assert.Throws<ArgumentException>(() => imageResourceConverter.Convert(new object(), typeof(ImageResourceConverter), null, CultureInfo.CurrentCulture));
	}

	static Stream GetStreamFromImageSource(ImageSource imageSource)
	{
		var streamImageSource = (StreamImageSource)imageSource;

		var cancellationToken = System.Threading.CancellationToken.None;
		var task = streamImageSource.Stream(cancellationToken);
		return task.Result;
	}

	static bool StreamEquals(Stream a, Stream b)
	{
		if (a == b)
			return true;

		if (a == null
			|| b == null
			|| a.Length != b.Length)
		{
			return false;
		}

		for (var i = 0; i < a.Length; i++)
		{
			if (a.ReadByte() != b.ReadByte())
				return false;
		}

		return true;
	}

}