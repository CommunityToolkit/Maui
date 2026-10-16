using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// Captures the static properties of option types (the toolkit's Options classes keep their values in static properties)
/// so a test that changes them can put the app-wide values back when it ends.
/// </summary>
public sealed class StaticPropertySnapshot
{
	const DynamicallyAccessedMemberTypes staticProperties = DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties;

	readonly List<(PropertyInfo Property, object? Value)> values = [];

	/// <summary>
	/// Records the current value of every settable static property declared by <paramref name="type"/>, whatever its accessibility.
	/// </summary>
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

	/// <summary>
	/// Number of properties captured.
	/// </summary>
	public int Count => values.Count;

	/// <summary>
	/// Writes the captured values back.
	/// </summary>
	public void Restore()
	{
		foreach (var (property, value) in values)
		{
			property.SetValue(null, value);
		}
	}
}