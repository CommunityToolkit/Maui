using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.DeviceTests;

// CameraView raises its events through a weak event manager, so keep the recorder referenced (for example with `using var`) while the test
// expects events. ErrorOccurred can be raised on any thread, hence the locks.
sealed class CameraErrorRecorder : IDisposable
{
	readonly CameraView view;
	readonly List<Exception> errors = [];

	public CameraErrorRecorder(CameraView view)
	{
		this.view = view;
		view.ErrorOccurred += HandleErrorOccurred;
	}

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

	/// <summary>
	/// Runs <paramref name="action"/>, which must not throw, and returns the one exception that <paramref name="view"/> reported through
	/// ErrorOccurred meanwhile, which must be exactly a <typeparamref name="TException"/>. StartCameraPreview reports a preview that cannot
	/// start this way instead of throwing.
	/// </summary>
	public static async Task<TException> AssertReportsAsync<TException>(CameraView view, Func<Task> action) where TException : Exception
	{
		using var recorder = new CameraErrorRecorder(view);

		await action();

		return Assert.IsType<TException>(Assert.Single(recorder.Errors));
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