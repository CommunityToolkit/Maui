// Vendored from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/TestUtils/src/DeviceTests.Runners/VisualRunner/XamlExtensions/EmbeddedResourceExtension.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespaces renamed from Microsoft.Maui.TestUtils.DeviceTests.Runners* to CommunityToolkit.Maui.DeviceTests.Runners*.
#nullable enable
using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace CommunityToolkit.Maui.DeviceTests.Runners.VisualRunner
{
	[ContentProperty(nameof(Name))]
	class EmbeddedResourceExtension : IMarkupExtension
	{
		public string? Name { get; set; }

		public virtual object? ProvideValue(IServiceProvider serviceProvider)
		{
			if (Name == null)
				return null;

			var resourceName = "." + Name.Trim().Replace('/', '.').Replace('\\', '.');

			var assembly = typeof(MauiVisualRunnerApp).Assembly;
			foreach (var name in assembly.GetManifestResourceNames())
			{
				if (name.EndsWith(resourceName, StringComparison.InvariantCultureIgnoreCase))
					return assembly.GetManifestResourceStream(name);
			}

			return null;
		}
	}
}