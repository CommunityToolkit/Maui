using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Essentials;

public partial class SaveFileDialogImplementation
{
	static async Task WriteStream(Stream stream, string filePath, CancellationToken cancellationToken)
	{
		await using var fileStream = new FileStream(filePath, FileMode.OpenOrCreate);
		stream.Seek(0, SeekOrigin.Begin);
		await stream.CopyToAsync(fileStream, cancellationToken);
	}

	static string GetFileName(string fileNameWithExtension)
	{
		return Path.GetFileNameWithoutExtension(fileNameWithExtension);
	}

	static string GetExtension(string fileNameWithExtension)
	{
		return Path.GetExtension(fileNameWithExtension);
	}
}