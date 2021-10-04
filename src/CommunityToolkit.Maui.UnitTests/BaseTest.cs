using System;
using System.Globalization;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.UnitTests
{
     public class  BaseTest : IDisposable
    {
        readonly CultureInfo? defaultCulture, defaultUICulture;

        protected BaseTest()
        {
            defaultCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
            defaultUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
            Device.PlatformServices = new MockPlatformServices();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            Device.PlatformServices = null;

            System.Threading.Thread.CurrentThread.CurrentCulture = defaultCulture ?? throw new NullReferenceException();
            System.Threading.Thread.CurrentThread.CurrentUICulture = defaultUICulture ?? throw new NullReferenceException();

        }
    }
}