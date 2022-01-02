using System.Globalization;
using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.UnitTests.Mocks;
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
	public async Task ImageResourceConverter()
	{
		const string resourceToLoad = "CommunityToolkit.Maui.UnitTests.Resources.dotnet-bot.png";

		var expectedResource = ImageSource.FromResource(resourceToLoad);
		var expectedMemoryStream = await GetStreamFromImageSource(expectedResource, CancellationToken.None);

		var imageResourceConverter = new ImageResourceConverter();
		var result = (ImageSource)imageResourceConverter.Convert(resourceToLoad, typeof(ImageResourceConverter), null, CultureInfo.CurrentCulture);

		var streamResult = await GetStreamFromImageSource(result, CancellationToken.None);

		Assert.True(StreamEquals(streamResult, expectedMemoryStream));
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