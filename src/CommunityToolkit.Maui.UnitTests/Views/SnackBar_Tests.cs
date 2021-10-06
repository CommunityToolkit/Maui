using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.UI.Views.Options;
using Microsoft.Maui.Controls;
using NUnit.Framework;

namespace CommunityToolkit.Maui.UnitTests.Views
{
    public class SnackBar_Tests
    {
        [Test]
        public void PageExtension_DisplaySnackBarAsync_PlatformNotSupportedException()
        {
            var page = new ContentPage();
            Assert.ThrowsAsync<PlatformNotSupportedException>(() => page.DisplaySnackBarAsync(string.Empty, string.Empty, () => Task.CompletedTask));
        }

        [Test]
        public void PageExtension_DisplaySnackBarAsyncWithOptions_PlatformNotSupportedException()
        {
            var page = new ContentPage();
            Assert.ThrowsAsync<PlatformNotSupportedException>(() => page.DisplaySnackBarAsync(new SnackBarOptions()));
        }

        [Test]
        public void PageExtension_DisplayToastAsync_PlatformNotSupportedException()
        {
            var page = new ContentPage();
            Assert.ThrowsAsync<PlatformNotSupportedException>(() => page.DisplayToastAsync("message"));
        }

        [Test]
        public void PageExtension_DisplayToastAsyncWithOptions_PlatformNotSupportedException()
        {
            var page = new ContentPage();
            Assert.ThrowsAsync<PlatformNotSupportedException>(() => page.DisplayToastAsync(new ToastOptions()));
        }
    }
}