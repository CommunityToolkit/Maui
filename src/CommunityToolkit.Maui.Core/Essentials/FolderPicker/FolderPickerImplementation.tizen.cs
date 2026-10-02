using CommunityToolkit.Maui.Core.Primitives;
using Microsoft.Maui.ApplicationModel;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc />
public sealed partial class FolderPickerImplementation : IFolderPicker
{
	async Task<Folder> InternalPickAsync(string initialPath, CancellationToken cancellationToken)
	{
		using var dialog = new FileFolderDialog(false, initialPath);
		var path = await dialog.Open().WaitAsync(cancellationToken).ConfigureAwait(false);

		return new Folder(path, Path.GetFileName(path));
	}

	Task<Folder> InternalPickAsync(CancellationToken cancellationToken)
	{
		return InternalPickAsync(FileFolderDialog.GetExternalDirectory(), cancellationToken);
	}
}