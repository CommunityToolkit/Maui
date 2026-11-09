// Vendored from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/TestUtils/src/TestShared/xUnitSharedAttributes.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespace changed from Microsoft.Maui to CommunityToolkit.Maui.DeviceTests and the discoverer assembly/type names updated to CommunityToolkit.Maui.DeviceTests.Shared, so test files in the CommunityToolkit.Maui.DeviceTests namespace bind [Fact], [Theory], [Category] and the data attributes to these types ahead of `using Xunit;`.
#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

// This file contains xUnit test attributes used in the TestUtils and TestUtils.DeviceTests projects.
// They are copies of the built-in xUnit attributes, but contain extra code to avoid first-chance
// exceptions that would otherwise occur when the tests begin. The exception they prevent is a Reflection
// exception due to the built-in xUnit attributes not having a constructor that takes an IMessageSink.

// Because all the tests in .NET MAUI are in sub-namespaces of Microsoft.Maui, these custom xUnit attributes
// are picked up instead of the built-in xUnit attributes that have the same name.

namespace CommunityToolkit.Maui.DeviceTests
{
	/// <inheritdoc/>
	public class InlineDataDiscoverer : Xunit.Sdk.InlineDataDiscoverer
	{
		public InlineDataDiscoverer(IMessageSink diagnosticMessageSink)
		{
		}
	}

	/// <summary>
	/// Provides a data source for a data theory, with the data coming from inline values.
	/// </summary>
	[DataDiscoverer("CommunityToolkit.Maui.DeviceTests.InlineDataDiscoverer", XUnitTypeData.XUnitAttributeAssembly)]
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	public sealed class InlineDataAttribute : DataAttribute
	{
		readonly object[] data;

		/// <summary>
		/// Initializes a new instance of the <see cref="InlineDataAttribute"/> class.
		/// </summary>
		/// <param name="data">The data values to pass to the theory.</param>
		public InlineDataAttribute(params object[] data)
		{
			this.data = data;
		}

		/// <inheritdoc/>
		public override IEnumerable<object[]> GetData(MethodInfo testMethod)
		{
			// This is called by the WPA81 version as it does not have access to attribute ctor params
			return new[] { data };
		}
	}

	public class ClassDataDiscoverer : DataDiscoverer
	{
		public ClassDataDiscoverer(IMessageSink diagnosticMessageSink)
		{
		}
	}

	/// <summary>
	/// Provides a data source for a data theory, with the data coming from a class
	/// which must implement IEnumerable&lt;object[]&gt;.
	/// Caution: the property is completely enumerated by .ToList() before any test is run. Hence it should return independent object sets.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
	[DataDiscoverer("CommunityToolkit.Maui.DeviceTests.ClassDataDiscoverer", XUnitTypeData.XUnitAttributeAssembly)]
	public class ClassDataAttribute : DataAttribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ClassDataAttribute"/> class.
		/// </summary>
		/// <param name="class">The class that provides the data.</param>
#if !NETSTANDARD

		public ClassDataAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type @class)
		{
			Class = @class;
		}

#else
		public ClassDataAttribute(Type @class)
		{
			Class = @class;
		}
#endif

		/// <summary>
		/// Gets the type of the class that provides the data.
		/// </summary>
#if !NETSTANDARD
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
#endif
		public Type Class { get; private set; }

		/// <inheritdoc/>
		public override IEnumerable<object[]> GetData(MethodInfo testMethod)
		{
			IEnumerable<object[]>? data = Activator.CreateInstance(Class) as IEnumerable<object[]>;
			if (data == null)
				throw new ArgumentException(
					string.Format(
						CultureInfo.CurrentCulture,
						"{0} must implement IEnumerable<object[]> to be used as ClassData for the test method named '{1}' on {2}",
						Class.FullName,
						testMethod.Name,
						testMethod.DeclaringType!.FullName
					)
				);

			return data;
		}
	}

	public class MemberDataDiscoverer : DataDiscoverer
	{
		public MemberDataDiscoverer(IMessageSink diagnosticMessageSink)
		{
		}
	}

	/// <summary>
	/// Provides a data source for a data theory, with the data coming from one of the following sources:
	/// 1. A static property
	/// 2. A static field
	/// 3. A static method (with parameters)
	/// The member must return something compatible with IEnumerable&lt;object[]&gt; with the test data.
	/// Caution: the property is completely enumerated by .ToList() before any test is run. Hence it should return independent object sets.
	/// </summary>
	[DataDiscoverer("CommunityToolkit.Maui.DeviceTests.MemberDataDiscoverer", XUnitTypeData.XUnitAttributeAssembly)]
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
	public sealed class MemberDataAttribute : MemberDataAttributeBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MemberDataAttribute"/> class.
		/// </summary>
		/// <param name="memberName">The name of the public static member on the test class that will provide the test data</param>
		/// <param name="parameters">The parameters for the member (only supported for methods; ignored for everything else)</param>
		public MemberDataAttribute(string memberName, params object[] parameters)
			: base(memberName, parameters) { }

		/// <inheritdoc/>
		protected override object[] ConvertDataItem(MethodInfo testMethod, object item)
		{
			if (item == null)
				return null!;

			var array = item as object[];
			if (array == null)
				throw new ArgumentException(
					string.Format(
						CultureInfo.CurrentCulture,
						"Property {0} on {1} yielded an item that is not an object[]",
						MemberName,
						MemberType ?? testMethod.DeclaringType
					)
				);

			return array;
		}
	}
}