using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// The resulting information from a Try* method in the .NET MAUI Community Toolkit
/// </summary>
public interface IResult
{
	/// <summary>
	/// Exception if operation failed
	/// </summary>
	Exception? Exception { get; }

	/// <summary>
	/// Check if operation was successful.
	/// </summary>
	[MemberNotNullWhen(false, nameof(Exception))]
	bool IsSuccessful => Exception is null;

	/// <summary>
	/// Check if the operation was cancelled.
	/// </summary>
	bool IsCancelled => Exception is OperationCanceledException;

	/// <summary>
	/// Check if operation was successful.
	/// </summary>
	/// <remarks>
	/// Throws <see cref="Exception"/> when not <see langword="null"/> 
	/// </remarks>
	void EnsureSuccess()
	{
		if (!IsSuccessful)
		{
			ExceptionDispatchInfo.Throw(Exception);
		}
	}
}