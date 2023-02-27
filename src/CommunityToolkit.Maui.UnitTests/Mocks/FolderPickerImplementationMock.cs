using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Storage;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

class FolderPickerImplementationMock : IFolderPicker
{
	public Task<FolderPickerResult> PickAsync(string initialPath, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(initialPath))
		{
			return Task.FromResult(new FolderPickerResult(null, new FolderPickerException("error")));
		}

		return Task.FromResult(new FolderPickerResult(new Folder(initialPath, "name"), null));
	}

	public Task<FolderPickerResult> PickAsync(CancellationToken cancellationToken)
	{
		return PickAsync(string.Empty, cancellationToken);
	}
}