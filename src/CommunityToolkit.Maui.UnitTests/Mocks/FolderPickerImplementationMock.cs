using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Storage;

namespace CommunityToolkit.Maui.UnitTests.Mocks;
class FolderPickerImplementationMock : IFolderPicker
{
	public Task<Folder> PickAsync(string initialPath, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(initialPath))
		{
			return Task.FromException<Folder>(new FolderPickerException("error"));
		}

		return Task.FromResult<Folder>(new Folder(initialPath, "name"));
	}

	public Task<Folder> PickAsync(CancellationToken cancellationToken)
	{
		return PickAsync(string.Empty, cancellationToken);
	}
}