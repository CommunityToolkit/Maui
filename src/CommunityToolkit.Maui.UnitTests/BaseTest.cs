using System;
using System.Globalization;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.UnitTests;

public class BaseTest : IDisposable
{
	readonly CultureInfo? defaultCulture, defaultUICulture;

	bool _isDisposed;

	protected BaseTest()
	{
		defaultCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
		defaultUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
		Device.PlatformServices = new MockPlatformServices();
		Application.Current = new MockApplication();
	}

	~BaseTest() => Dispose(false);

	protected virtual void Dispose(bool isDisposing)
	{
		if (_isDisposed)
			return;

		Device.PlatformServices = null;
		Application.Current = null;

		System.Threading.Thread.CurrentThread.CurrentCulture = defaultCulture ?? throw new NullReferenceException();
		System.Threading.Thread.CurrentThread.CurrentUICulture = defaultUICulture ?? throw new NullReferenceException();

		_isDisposed = true;
	}

	void IDisposable.Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
}
