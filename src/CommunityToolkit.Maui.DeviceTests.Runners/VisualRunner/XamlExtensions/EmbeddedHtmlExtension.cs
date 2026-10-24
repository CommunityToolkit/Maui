// Vendored from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/TestUtils/src/DeviceTests.Runners/VisualRunner/XamlExtensions/EmbeddedHtmlExtension.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespaces renamed from Microsoft.Maui.TestUtils.DeviceTests.Runners* to CommunityToolkit.Maui.DeviceTests.Runners*.
#nullable enable
using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace CommunityToolkit.Maui.DeviceTests.Runners.VisualRunner
{
	[AcceptEmptyServiceProvider]
	class EmbeddedHtmlExtension : EmbeddedResourceExtension
	{
		public override object? ProvideValue(IServiceProvider serviceProvider)
		{
			if (base.ProvideValue(serviceProvider) is Stream stream)
			{
				using var reader = new StreamReader(stream, leaveOpen: false);
				return new HtmlWebViewSource { Html = reader.ReadToEnd() };
			}

			return null;
		}
	}
}