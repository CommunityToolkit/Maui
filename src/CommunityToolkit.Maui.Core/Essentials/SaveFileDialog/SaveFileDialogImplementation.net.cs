namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc />
public partial class SaveFileDialogImplementation : ISaveFileDialog
{
	/// <inheritdoc />
	public Task<string> SaveAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public Task<string> SaveAsync(string fileName, Stream stream, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}
}