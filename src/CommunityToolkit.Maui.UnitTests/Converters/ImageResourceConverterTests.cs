using System.Globalization;
using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ImageResourceConverterTests : BaseOneWayConverterTest<ImageResourceConverter>
{
	public ImageResourceConverterTests()
	{
		Application.Current = new MockApplication();
	}

	public static IReadOnlyList<object[]> NonStringData { get; } =
	[
		[3], // primitive type
		[DateTime.UtcNow], // Struct
		[new()] // objects
	];

	protected override void Dispose(bool isDisposing)
	{
		Application.Current = null;

		base.Dispose(isDisposing);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ImageResourceConverter()
	{
		const string resourceToLoad = "CommunityToolkit.Maui.UnitTests.Resources.dotnet-bot.png";

		var expectedResource = (StreamImageSource)ImageSource.FromResource(resourceToLoad);
		var expectedMemoryStream = await GetStreamFromImageSource(expectedResource, CancellationToken.None);

		var imageResourceConverter = new ImageResourceConverter();
		var convertResult = (StreamImageSource)(((ICommunityToolkitValueConverter)imageResourceConverter).Convert(resourceToLoad, typeof(ImageSource), null, CultureInfo.CurrentCulture) ?? throw new NullReferenceException());
		var streamResult = await GetStreamFromImageSource(convertResult, CancellationToken.None);

		var convertFromResult = (StreamImageSource)imageResourceConverter.ConvertFrom(resourceToLoad, CultureInfo.CurrentCulture);
		var streamFromResult = await GetStreamFromImageSource(convertFromResult, CancellationToken.None);

		Assert.True(StreamEquals(expectedMemoryStream, streamResult));
		Assert.True(StreamEquals(expectedMemoryStream, streamFromResult));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ImageResourceConverter_CancellationTokenExpired()
	{
		const string resourceToLoad = "CommunityToolkit.Maui.UnitTests.Resources.dotnet-bot.png";
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		var imageResourceConverter = new ImageResourceConverter();

		var convertFromResult = (StreamImageSource)imageResourceConverter.ConvertFrom(resourceToLoad, CultureInfo.CurrentCulture);

		// Ensure CancellationToken has expired
		await Task.Delay(100, CancellationToken.None);

		await Assert.ThrowsAsync<TaskCanceledException>(() => GetStreamFromImageSource(convertFromResult, cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ImageResourceConverter_CancellationTokenCancelled()
	{
		const string resourceToLoad = "CommunityToolkit.Maui.UnitTests.Resources.dotnet-bot.png";
		var cts = new CancellationTokenSource();

		var imageResourceConverter = new ImageResourceConverter();

		var convertFromResult = (StreamImageSource)imageResourceConverter.ConvertFrom(resourceToLoad, CultureInfo.CurrentCulture);

		// Ensure CancellationToken has expired
		await cts.CancelAsync();

		await Assert.ThrowsAsync<TaskCanceledException>(() => GetStreamFromImageSource(convertFromResult, cts.Token));
	}

	[Fact]
	public void NullValueReturnsNull()
	{
		var imageResourceConverter = new ImageResourceConverter();

		Assert.Null(((ICommunityToolkitValueConverter)imageResourceConverter).Convert(null, typeof(ImageSource), null, CultureInfo.CurrentCulture));
		Assert.Null(imageResourceConverter.ConvertFrom(null));
	}

	[Theory]
	[MemberData(nameof(NonStringData))]
	public void ThrowsIfNotAString(object value)
	{
		var imageResourceConverter = new ImageResourceConverter();

		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)imageResourceConverter).Convert(value, typeof(ImageSource), null, CultureInfo.CurrentCulture));
	}

	[Fact]
	public void ImageResourceConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ImageResourceConverter()).Convert(string.Empty, null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ImageResourceConverter()).ConvertBack(ImageSource.FromStream(() => Stream.Null), null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}