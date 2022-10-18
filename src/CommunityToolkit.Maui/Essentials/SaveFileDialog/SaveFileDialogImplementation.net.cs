using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Essentials;

/// <inheritdoc />
public partial class SaveFileDialogImplementation : ISaveFileDialog
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="initialPath"></param>
	/// <param name="stream"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	/// <exception cref="NotImplementedException"></exception>
	public Task<bool> SaveAsync(string initialPath, Stream stream, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="stream"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	/// <exception cref="NotImplementedException"></exception>
	public Task<bool> SaveAsync(Stream stream, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}
}