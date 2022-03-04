using System;
using System.Reflection;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public sealed class ByteArrayToImageSourceConverterViewModel : BaseViewModel, IDisposable
{
	public ByteArrayToImageSourceConverterViewModel()
	{
		const string dotnetBotSvgPath = "CommunityToolkit.Maui.UnitTests.Resources.images.dotnet-bot.svg";

		var applicationTypeInfo = Application.Current?.GetType().GetTypeInfo() ?? throw new InvalidOperationException("Application.Current cannot be null");
		var dotnetBotImageAsStream = applicationTypeInfo.Assembly.GetManifestResourceStream($"{applicationTypeInfo.Namespace}.{dotnetBotSvgPath}") ?? throw new InvalidOperationException("Could not find dotnetbotsvg");

		DotNetBotImageByteArray = ConvertStreamToByteArrary(dotnetBotImageAsStream);
	}

	public byte[]? DotNetBotImageByteArray { get; private set; }

	public void Dispose()
	{
		DotNetBotImageByteArray = null;
	}

	static byte[] ConvertStreamToByteArrary(Stream stream)
	{
		using var memoryStream = new MemoryStream();
		stream.CopyTo(memoryStream);
		return memoryStream.ToArray();
	}
}

