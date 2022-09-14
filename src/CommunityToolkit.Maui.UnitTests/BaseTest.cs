using System.Globalization;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Microsoft.Maui.Dispatching;

namespace CommunityToolkit.Maui.UnitTests;

public abstract class BaseTest : IDisposable
{
	readonly CultureInfo defaultCulture, defaultUiCulture;

	bool isDisposed;

	protected BaseTest()
	{
		defaultCulture = Thread.CurrentThread.CurrentCulture;
		defaultUiCulture = Thread.CurrentThread.CurrentUICulture;

		DispatcherProvider.SetCurrent(new MockDispatcherProvider());
		DeviceDisplay.SetCurrent(null);
	}

	~BaseTest() => Dispose(false);

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool isDisposing)
	{
		if (isDisposed)
		{
			return;
		}

		Thread.CurrentThread.CurrentCulture = defaultCulture;
		Thread.CurrentThread.CurrentUICulture = defaultUiCulture;

		DeviceDisplay.SetCurrent(null);
		DispatcherProvider.SetCurrent(null);

		var options = new Options();
		options.SetShouldSuppressExceptionsInAnimations(false);
		options.SetShouldSuppressExceptionsInBehaviors(false);
		options.SetShouldSuppressExceptionsInConverters(false);

		isDisposed = true;
	}

	protected static Task<Stream> GetStreamFromImageSource(StreamImageSource imageSource, CancellationToken token)
		=> imageSource.Stream(token);

	protected static bool StreamEquals(Stream a, Stream b)
	{
		if (a == b)
		{
			return true;
		}

		if (a.Length != b.Length)
		{
			return false;
		}

		for (var i = 0; i < a.Length; i++)
		{
			if (a.ReadByte() != b.ReadByte())
			{
				return false;
			}
		}

		return true;
	}
}