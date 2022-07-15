using System.Globalization;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Microsoft.Maui.Dispatching;

namespace CommunityToolkit.Maui.UnitTests;

public abstract class BaseTest : IDisposable
{
	readonly CultureInfo defaultCulture, defaultUICulture;

	bool isDisposed;

	protected BaseTest()
	{
		defaultCulture = Thread.CurrentThread.CurrentCulture;
		defaultUICulture = Thread.CurrentThread.CurrentUICulture;

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
		Thread.CurrentThread.CurrentUICulture = defaultUICulture;

		DispatcherProvider.SetCurrent(null);
		DeviceDisplay.SetCurrent(null);

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