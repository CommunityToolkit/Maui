namespace CommunityToolkit.Maui.Core;

/// <summary>
/// 
/// </summary>
public interface ISaveFileDialog
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="initialPath"></param>
	/// <param name="stream"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<bool> SaveAsync(string initialPath, Stream stream, CancellationToken cancellationToken);

	/// <summary>
	/// 
	/// </summary>
	/// <param name="stream"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<bool> SaveAsync(Stream stream, CancellationToken cancellationToken);
}