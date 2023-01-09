namespace CommunityToolkit.Maui.Storage;

partial class SaveFileDialogImplementation
{
	static async Task WriteStream(Stream stream, string filePath, CancellationToken cancellationToken)
	{
		await using var fileStream = new FileStream(filePath, FileMode.OpenOrCreate);
		stream.Seek(0, SeekOrigin.Begin);
		await stream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
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