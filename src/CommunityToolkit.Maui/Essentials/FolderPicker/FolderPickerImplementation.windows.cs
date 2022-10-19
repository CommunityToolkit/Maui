using System.Diagnostics;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;
using Windows.Storage.Pickers;

namespace CommunityToolkit.Maui.Essentials;

/// <inheritdoc />
public class FolderPickerImplementation : IFolderPicker
{
	/// <inheritdoc />
	public async Task<Folder?> PickAsync(string initialPath, CancellationToken cancellationToken)
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
			return null;
		}

		return new Folder
		{
			Path = folder.Path,
			Name = folder.Name
		};
	}

	/// <inheritdoc />
	public Task<Folder?> PickAsync(CancellationToken cancellationToken)
	{
		return PickAsync("", cancellationToken);
	}
}