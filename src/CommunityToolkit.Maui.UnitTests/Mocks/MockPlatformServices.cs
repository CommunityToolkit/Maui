﻿using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Graphics;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

public class MockPlatformServices : IPlatformServices
{
    public string GetHash(string input) => string.Empty;

    public string GetMD5Hash(string input) => string.Empty;

    public double GetNamedSize(NamedSize size, Type targetElement, bool useOldSizes) => 0;

    public Color GetNamedColor(string name) => Colors.Transparent;

    public void OpenUriAction(Uri uri)
    {
    }

    public bool IsInvokeRequired { get; } = false;

    public OSAppTheme RequestedTheme { get; } = OSAppTheme.Unspecified;

    public string RuntimePlatform { get; set; } = string.Empty;

    public void BeginInvokeOnMainThread(Action action) => action();

    public void StartTimer(TimeSpan interval, Func<bool> callback)
    {
    }

    public Task<Stream> GetStreamAsync(Uri uri, CancellationToken cancellationToken)
        => Task.FromResult<Stream>(new MemoryStream());

    public Assembly[] GetAssemblies() => Array.Empty<Assembly>();

    public IIsolatedStorageFile? GetUserStoreForApplication() => null;

    public void QuitApplication()
    {
    }

    public SizeRequest GetNativeSize(VisualElement view, double widthConstraint, double heightConstraint) => default;
}