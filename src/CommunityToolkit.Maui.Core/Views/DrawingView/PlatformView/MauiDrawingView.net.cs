using System;
namespace CommunityToolkit.Maui.Core.Views;

public partial class MauiDrawingView : IDisposable
{
	bool isDisposed;

	/// <inheritdoc />
	~MauiDrawingView() => Dispose(false);

	/// <summary>
	/// Initialize resources
	/// </summary>
	public static void Initialize()
	{

	}

	/// <inheritdoc />
	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	/// <inheritdoc />
	protected virtual void Dispose(bool disposing)
	{
		if (!isDisposed)
		{
			if (disposing)
			{
				currentPath.Dispose();
			}

			isDisposed = true;
		}
	}

	static void Redraw()
	{

	}
}