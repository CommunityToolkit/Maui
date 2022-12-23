namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc />
public partial class SaveFileDialogImplementation : ISaveFileDialog
{
	/// <inheritdoc />
	public ValueTask<string> SaveAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public ValueTask<string> SaveAsync(string fileName, Stream stream, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}
}