namespace CommunityToolkit.Maui.Storage;

public sealed partial class FolderPickerImplementation
{
	/// <inheritdoc/>
	public async Task<FolderPickerResult> PickAsync(string initialPath, CancellationToken cancellationToken)
	{
		try
		{
			cancellationToken.ThrowIfCancellationRequested();
			var folder = await InternalPickAsync(initialPath, cancellationToken);
			return new FolderPickerResult(folder, null);
		}
		catch (Exception e)
		{
			return new FolderPickerResult(null, e);
		}
	}

	/// <inheritdoc/>
	public async Task<FolderPickerResult> PickAsync(CancellationToken cancellationToken)
	{
		try
		{
			cancellationToken.ThrowIfCancellationRequested();
			var folder = await InternalPickAsync(cancellationToken);
			return new FolderPickerResult(folder, null);
		}
		catch (Exception e)
		{
			return new FolderPickerResult(null, e);
		}
	}
}