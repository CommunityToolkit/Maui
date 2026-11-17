using System;
using System.Collections.Generic;
using System.Linq;
using Xunit.Sdk;

namespace CommunityToolkit.Maui.DeviceTests;

// xunit v2's Assert.Equal/Empty/All/Collection over value-type items build generic comparers by reflection (MakeGenericMethod), which
// NativeAOT cannot do. These compare element by element with EqualityComparer<T>.Default, so the same tests run under NativeAOT.
public static class AotSafeAssert
{
	// Long item lists (movie bytes, file contents) would flood the result log.
	const int maxItemsInMessage = 20;

	public static void Equal<T>(T expected, T actual)
	{
		if (!EqualityComparer<T>.Default.Equals(expected, actual))
		{
			throw new XunitException($"Values differ.{Environment.NewLine}Expected: {expected?.ToString() ?? "null"}{Environment.NewLine}Actual:   {actual?.ToString() ?? "null"}");
		}
	}

	public static void Empty<T>(IEnumerable<T> items)
	{
		ArgumentNullException.ThrowIfNull(items);

		var itemList = items.ToArray();
		if (itemList.Length > 0)
		{
			throw new XunitException($"The collection was expected to be empty but has {itemList.Length} items: {Describe(itemList)}");
		}
	}

	public static void NotEmpty<T>(IEnumerable<T> items)
	{
		ArgumentNullException.ThrowIfNull(items);

		if (!items.Any())
		{
			throw new XunitException("The collection was expected to contain items but is empty.");
		}
	}

	public static void SequenceEqual<T>(IEnumerable<T>? expected, IEnumerable<T>? actual)
	{
		if (expected is null || actual is null)
		{
			if (expected is not null || actual is not null)
			{
				throw new XunitException($"Sequences differ.{Environment.NewLine}Expected: {(expected is null ? "null" : Describe(expected.ToArray()))}{Environment.NewLine}Actual:   {(actual is null ? "null" : Describe(actual.ToArray()))}");
			}

			return;
		}

		var expectedItems = expected.ToArray();
		var actualItems = actual.ToArray();
		var firstDifference = FindFirstDifference(expectedItems, actualItems);

		if (firstDifference is not null)
		{
			throw new XunitException($"Sequences differ at index {firstDifference} (expected length {expectedItems.Length}, actual length {actualItems.Length}).{Environment.NewLine}Expected: {Describe(expectedItems, firstDifference.Value)}{Environment.NewLine}Actual:   {Describe(actualItems, firstDifference.Value)}");
		}
	}

	public static void Contains<T>(T expected, IEnumerable<T> items)
	{
		var itemList = items.ToArray();
		if (!itemList.Contains(expected))
		{
			throw new XunitException($"Item not found in the collection.{Environment.NewLine}Item:       {expected}{Environment.NewLine}Collection: {Describe(itemList)}");
		}
	}

	public static void DoesNotContain<T>(T unexpected, IEnumerable<T> items)
	{
		var itemList = items.ToArray();
		if (itemList.Contains(unexpected))
		{
			throw new XunitException($"Item found in the collection.{Environment.NewLine}Item:       {unexpected}{Environment.NewLine}Collection: {Describe(itemList)}");
		}
	}

	public static void All<T>(IEnumerable<T> items, Action<T> inspector)
	{
		var index = 0;
		foreach (var item in items)
		{
			try
			{
				inspector(item);
			}
			catch (XunitException exception)
			{
				throw new XunitException($"Item [{index}] ({item}) did not pass: {exception.Message}", exception);
			}

			index++;
		}
	}

	static int? FindFirstDifference<T>(T[] expectedItems, T[] actualItems)
	{
		var commonLength = Math.Min(expectedItems.Length, actualItems.Length);
		for (var index = 0; index < commonLength; index++)
		{
			if (!EqualityComparer<T>.Default.Equals(expectedItems[index], actualItems[index]))
			{
				return index;
			}
		}

		return expectedItems.Length == actualItems.Length ? null : commonLength;
	}

	static string Describe<T>(T[] items, int startIndex = 0)
	{
		var shown = items.Skip(startIndex).Take(maxItemsInMessage);
		var suffix = items.Length - startIndex > maxItemsInMessage ? ", ..." : string.Empty;
		return $"[{string.Join(", ", shown)}{suffix}]";
	}
}