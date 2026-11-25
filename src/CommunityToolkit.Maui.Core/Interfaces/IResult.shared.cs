using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// The result of an operation that reports a failure through <see cref="Exception"/> instead of throwing it, for example the result of a Try* method.
/// </summary>
public interface IResult
{
	/// <summary>
	/// Gets the exception if the operation failed; <see langword="null"/> if the operation was successful.
	/// </summary>
	Exception? Exception { get; }

	/// <summary>
	/// Gets whether the operation was successful.
	/// </summary>
	[MemberNotNullWhen(false, nameof(Exception))]
	bool IsSuccessful { get; }

	/// <summary>
	/// Gets whether the operation was canceled.
	/// </summary>
	bool IsCancelled { get; }

	/// <summary>
	/// Throws <see cref="Exception"/> if the operation was not successful.
	/// </summary>
	void EnsureSuccess();
}