using CommunityToolkit.Maui.UnitTests.Mocks;
using Microsoft.Maui.Controls;
using NUnit.Framework;
using System.Globalization;

namespace CommunityToolkit.Maui.UnitTests
{
    public class BaseTestFixture
    {
        CultureInfo? defaultCulture;
        CultureInfo? defaultUICulture;

        [SetUp]
        public virtual void Setup()
        {
            defaultCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
            defaultUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
            Device.PlatformServices = new MockPlatformServices();
        }

        [TearDown]
        public virtual void TearDown()
        {
            Device.PlatformServices = null;

            System.Threading.Thread.CurrentThread.CurrentCulture = defaultCulture ?? throw new NullReferenceException();
            System.Threading.Thread.CurrentThread.CurrentUICulture = defaultUICulture ?? throw new NullReferenceException();
        }
    }
}