using System;
using System.Linq;

namespace CommunityToolkit.Maui.Extensions;

/// <summary>
/// Linq extensions
/// </summary>
public static class LinqExtensions
{
	/// <summary>
	/// Get Max value of items
	/// </summary>
	public static T? Max<T>(params T[] items) => items.Max();
}

