using System.Diagnostics;
using CommunityToolkit.Maui.Core.Primitives;
using Windows.Storage.Pickers;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc />
public class FolderPickerImplementation : IFolderPicker
{
	/// <inheritdoc />
	public async ValueTask<Folder> PickAsync(string initialPath, CancellationToken cancellationToken)
	{
		var folderPicker = new Windows.Storage.Pickers.FolderPicker()
		{
			SuggestedStartLocation = PickerLocationId.DocumentsLibrary
		};
		WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, Process.GetCurrentProcess().MainWindowHandle);
		folderPicker.FileTypeFilter.Add("*");
		var folder = await folderPicker.PickSingleFolderAsync();
		if (folder is null)
		{
			throw new FolderPickerException("Folder doesn't exist.");
		}

		return new Folder(folder.Path, folder.Name);
	}

	/// <inheritdoc />
	public ValueTask<Folder> PickAsync(CancellationToken cancellationToken)
	{
		return PickAsync(string.Empty, cancellationToken);
	}
}