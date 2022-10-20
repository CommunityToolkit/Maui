using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.UnitTests.Mocks;
class FolderPickerImplementationMock : IFolderPicker
{
	public Task<Folder?> PickAsync(string initialPath, CancellationToken cancellationToken)
	{
		if (initialPath == string.Empty)
		{
			return Task.FromResult<Folder?>(null);
		}

		return Task.FromResult<Folder?>(new Folder()
		{
			Path = initialPath,
			Name = "name"
		});
	}

	public Task<Folder?> PickAsync(CancellationToken cancellationToken)
	{
		return PickAsync(string.Empty, cancellationToken);
	}
}