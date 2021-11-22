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

	public static IReadOnlyList<object[]> NonStringData { get; } = new[]
	{
		new object[] { 3 }, // primitive type
		new object[] { DateTime.UtcNow }, // Struct
		new object[] { new object() } // objects
	};

	protected override void Dispose(bool isDisposing)
	{
		Application.Current = null;

		base.Dispose(isDisposing);
	}

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
	[MemberData(nameof(NonStringData))]
	public void ThrowsIfNotAString(object value)
	{
		var imageResourceConverter = new ImageResourceConverter();

		Assert.Throws<ArgumentException>(() => imageResourceConverter.Convert(value, typeof(ImageResourceConverter), null, CultureInfo.CurrentCulture));
	}
}