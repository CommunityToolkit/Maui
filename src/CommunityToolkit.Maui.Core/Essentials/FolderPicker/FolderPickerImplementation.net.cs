using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc />
public sealed partial class FolderPickerImplementation : IFolderPicker
{
	Task<Folder> InternalPickAsync(string initialPath, CancellationToken cancellationToken) => throw new NotImplementedException();

	Task<Folder> InternalPickAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
}