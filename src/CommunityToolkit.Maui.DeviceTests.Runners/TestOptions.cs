// Vendored from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/TestUtils/src/DeviceTests.Runners/TestOptions.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespaces renamed from Microsoft.Maui.TestUtils.DeviceTests.Runners* to CommunityToolkit.Maui.DeviceTests.Runners*.
#nullable enable
using System.Collections.Generic;
using System.Reflection;

namespace CommunityToolkit.Maui.DeviceTests.Runners
{
	public class TestOptions
	{
		/// <summary>
		/// The list of assemblies that contain tests.
		/// </summary>
		public List<Assembly> Assemblies { get; set; } = new List<Assembly>();

		/// <summary>
		/// The list of categories to skip in the form:
		///   [category-name]=[skip-when-value]
		/// </summary>
		public List<string> SkipCategories { get; set; } = new List<string>();
	}
}