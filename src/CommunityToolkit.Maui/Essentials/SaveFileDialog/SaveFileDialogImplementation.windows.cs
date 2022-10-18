using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Essentials;

/// <summary>
/// 
/// </summary>
public partial class SaveFileDialogImplementation : ISaveFileDialog
{
	public Task<bool> SaveAsync(string initialPath, Stream stream, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	public Task<bool> SaveAsync(Stream stream, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}
}