// Vendored from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/TestUtils/src/DeviceTests/RepeatAttribute.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespace changed from Microsoft.Maui(.DeviceTests) to CommunityToolkit.Maui.DeviceTests.
// Toolkit change: #nullable enable added: the upstream project enables nullable reference types, these copies are compiled as generated code.
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit.Sdk;

namespace CommunityToolkit.Maui.DeviceTests
{
	/// <summary>	
	/// Usage Example
	/// [Theory]
	/// [Repeat(100)]
	/// public async Task TheSameImageSourceReturnsTheSameBitmap(int _)
	/// </summary>
	public sealed class RepeatAttribute : DataAttribute
	{
		readonly int _count;

		public RepeatAttribute(int count)
		{
			this._count = count;
		}

		public override IEnumerable<object[]> GetData(MethodInfo testMethod)
		{
			foreach (var iterationNumber in Enumerable.Range(start: 1, count: this._count))
			{
				yield return new object[] { iterationNumber };
			}
		}
	}
}
