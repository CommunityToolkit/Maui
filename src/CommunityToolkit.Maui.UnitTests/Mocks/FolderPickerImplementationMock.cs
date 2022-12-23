using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Storage;

namespace CommunityToolkit.Maui.UnitTests.Mocks;
class FolderPickerImplementationMock : IFolderPicker
{
	public ValueTask<Folder> PickAsync(string initialPath, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(initialPath))
		{
			return ValueTask.FromException<Folder>(new FolderPickerException("error"));
		}

		return ValueTask.FromResult<Folder>(new Folder(initialPath, "name"));
	}

	public ValueTask<Folder> PickAsync(CancellationToken cancellationToken)
	{
		return PickAsync(string.Empty, cancellationToken);
	}
}