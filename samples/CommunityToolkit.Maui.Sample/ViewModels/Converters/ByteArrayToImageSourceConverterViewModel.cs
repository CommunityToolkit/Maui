namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public sealed class ByteArrayToImageSourceConverterViewModel : BaseViewModel, IDisposable
{
	public ByteArrayToImageSourceConverterViewModel()
	{
		InitializeDotnetBotByteArray();
	}

	public byte[]? DotNetBotImageByteArray { get; private set; }

	public void Dispose()
	{
		DotNetBotImageByteArray = null;
	}

	async void InitializeDotnetBotByteArray()
	{
		var imageSource = new Image { Source = "dotnet-bot" }.Source;
		int totalPixelBytes = imageSource.LoadImage().BytesPerLine * e.Image.Height;

		byte[] byteArray = new byte[totalPixelBytes];
		e.Image.GetRow(0, byteArray, 0, totalPixelBytes);

		var dotnetBotImageAsStream = await streamImageSource.Stream(CancellationToken.None);

		DotNetBotImageByteArray = ConvertStreamToByteArrary(dotnetBotImageAsStream);
	}

	static byte[] ToByteArray(this Image imageIn)
	{
		var ms = new MemoryStream();
		imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
		return ms.ToArray();
	}

	static byte[] ConvertStreamToByteArrary(Stream stream)
	{
		using var memoryStream = new MemoryStream();
		stream.CopyTo(memoryStream);
		return memoryStream.ToArray();
	}
}

