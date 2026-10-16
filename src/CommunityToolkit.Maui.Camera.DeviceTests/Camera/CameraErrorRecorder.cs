using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// Records the exceptions that a <see cref="CameraView"/> reports through its ErrorOccurred event, on any thread, until it is disposed.
/// CameraView raises its events through a weak event manager: keep the recorder referenced (for example with <c>using var</c>) while
/// the test expects events.
/// </summary>
sealed class CameraErrorRecorder : IDisposable
{
	readonly CameraView view;
	readonly List<Exception> errors = [];

	public CameraErrorRecorder(CameraView view)
	{
		this.view = view;
		view.ErrorOccurred += HandleErrorOccurred;
	}

	/// <summary>The exceptions reported so far, in the order the view reported them.</summary>
	public IReadOnlyList<Exception> Errors
	{
		get
		{
			lock (errors)
			{
				return [.. errors];
			}
		}
	}

	public void Dispose() => view.ErrorOccurred -= HandleErrorOccurred;

	void HandleErrorOccurred(object? sender, ErrorOccurredEventArgs e)
	{
		lock (errors)
		{
			errors.Add(e.Exception);
		}
	}
}