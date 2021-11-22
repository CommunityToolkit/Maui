using System;
using System.Collections.Generic;
using System.Globalization;
using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Microsoft.Maui.Controls;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ImageResourceConverter_Tests : BaseTest
{
	public ImageResourceConverter_Tests() : base()
	{
		Application.Current = new MockApplication();
	}

	protected override void Dispose(bool isDisposing)
	{
		Application.Current = null;

		base.Dispose(isDisposing);
	}

	public static IReadOnlyList<object[]> Data { get; } = new[]
	{
		new object[] { 3 },
		new object[] { DateTime.UtcNow },
		new object[] { new object() }
	};

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

	[Theory]
	[MemberData(nameof(Data))]
	public void ThrowsIfNotAString(object value)
	{
		var imageResourceConverter = new ImageResourceConverter();

		Assert.Throws<ArgumentException>(() => imageResourceConverter.Convert(value, typeof(ImageResourceConverter), null, CultureInfo.CurrentCulture));
	}
}