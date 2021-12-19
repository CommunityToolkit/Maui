using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Essentials;

namespace CommunityToolkit.Maui.UnitTests;

public abstract class BaseTest : IDisposable
{
	readonly CultureInfo? defaultCulture, defaultUICulture;

	bool isDisposed;

	protected BaseTest()
	{
		defaultCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
		defaultUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;

		Device.PlatformServices = new MockPlatformServices();

		DispatcherProvider.SetCurrent(new DispatcherProviderMock());
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
			return;

		Device.PlatformServices = null;

		System.Threading.Thread.CurrentThread.CurrentCulture = defaultCulture ?? throw new NullReferenceException();
		System.Threading.Thread.CurrentThread.CurrentUICulture = defaultUICulture ?? throw new NullReferenceException();

		DispatcherProvider.SetCurrent(null);
		DeviceDisplay.SetCurrent(null);

		isDisposed = true;
	}

	protected static Task<Stream> GetStreamFromImageSource(ImageSource imageSource, CancellationToken token)
	{
		var streamImageSource = (StreamImageSource)imageSource;
		return streamImageSource.Stream(token);
	}

	protected static bool StreamEquals(Stream a, Stream b)
	{
		if (a == b)
			return true;

		if (a.Length != b.Length)
			return false;

		for (var i = 0; i < a.Length; i++)
		{
			if (a.ReadByte() != b.ReadByte())
				return false;
		}

		return true;
	}
}