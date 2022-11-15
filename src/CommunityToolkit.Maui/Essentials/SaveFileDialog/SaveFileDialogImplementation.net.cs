using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Storage;

/// <inheritdoc />
public partial class SaveFileDialogImplementation : ISaveFileDialog
{
	/// <inheritdoc />
	public Task SaveAsync(string initialPath, string fileName, Stream stream, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public Task SaveAsync(string fileName, Stream stream, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}
}