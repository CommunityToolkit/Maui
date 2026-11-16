using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// Static properties, such as those of the toolkit's Options classes, hold app-wide values that outlive the test that changes them,
/// so that test has to put them back when it ends.
/// </summary>
public sealed class StaticPropertySnapshot
{
	const DynamicallyAccessedMemberTypes staticProperties = DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties;

	readonly List<(PropertyInfo Property, object? Value)> values = [];

	public StaticPropertySnapshot Capture([DynamicallyAccessedMembers(staticProperties)] Type type)
	{
		foreach (var property in type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
		{
			if (property.GetMethod is not null && property.SetMethod is not null && property.GetIndexParameters().Length is 0)
			{
				values.Add((property, property.GetValue(null)));
			}
		}

		return this;
	}

	public int Count => values.Count;

	public void Restore()
	{
		foreach (var (property, value) in values)
		{
			property.SetValue(null, value);
		}
	}
}