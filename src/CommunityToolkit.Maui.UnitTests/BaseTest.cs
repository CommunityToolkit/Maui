using System;
using System.Globalization;
using System.IO;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.UnitTests;

public abstract class BaseTest : IDisposable
{
	readonly CultureInfo? defaultCulture, defaultUICulture;

	bool _isDisposed;

	protected BaseTest()
	{
		defaultCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
		defaultUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
		Device.PlatformServices = new MockPlatformServices();
	}

	~BaseTest() => Dispose(false);

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool isDisposing)
	{
		if (_isDisposed)
			return;

		Device.PlatformServices = null;

		System.Threading.Thread.CurrentThread.CurrentCulture = defaultCulture ?? throw new NullReferenceException();
		System.Threading.Thread.CurrentThread.CurrentUICulture = defaultUICulture ?? throw new NullReferenceException();

		_isDisposed = true;
	}

	protected static Stream GetStreamFromImageSource(ImageSource imageSource)
	{
		var streamImageSource = (StreamImageSource)imageSource;

		var cancellationToken = System.Threading.CancellationToken.None;
		var task = streamImageSource.Stream(cancellationToken);
		return task.Result;
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