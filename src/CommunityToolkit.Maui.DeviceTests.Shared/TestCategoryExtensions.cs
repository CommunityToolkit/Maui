using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace CommunityToolkit.Maui.DeviceTests;

public static class TestCategoryExtensions
{
	/// <summary>
	/// The visual runner ignores this list, so the <paramref name="neverRunHeadless"/> categories can still be run by hand.
	/// </summary>
	public static List<string> GetHeadlessSkipCategories([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] this Type testCategoryType, params string[] neverRunHeadless)
	{
		var skipCategories = testCategoryType.GetExcludedTestCategories();

		foreach (var category in neverRunHeadless)
		{
			var entry = $"Category={category}";
			if (!skipCategories.Contains(entry))
			{
				skipCategories.Add(entry);
			}
		}

		Console.WriteLine($"Device tests skip categories: {string.Join(", ", skipCategories)}");

		return skipCategories;
	}

	/// <summary>
	/// "TestFilter=Category=X" skips every other constant, so a test without a category (or with a string that is not a constant)
	/// runs in every filtered run, and a test with two feature categories is skipped by both "Category=" runs. Windows runs a
	/// filtered app once per category and skips all other categories in each launch, so such a test never runs there.
	/// </summary>
	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Test classes and their methods are kept: the test runner discovers them by reflection.")]
	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Test classes and their methods are kept: the test runner discovers them by reflection.")]
	public static List<string> GetCategoryRuleViolations([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] this Type testCategoryType, Assembly testAssembly, params string[] additionalCategories)
	{
		var knownCategories = new HashSet<string>(testCategoryType.GetTestCategoryValues(), StringComparer.Ordinal);
		var violations = new List<string>();

		foreach (var type in testAssembly.GetTypes().OrderBy(static type => type.FullName, StringComparer.Ordinal))
		{
			if (!type.IsClass || type.IsAbstract || type.ContainsGenericParameters)
			{
				continue;
			}

			var classCategories = new List<string>();
			for (var current = type; current is not null; current = current.BaseType)
			{
				classCategories.AddRange(GetCategories(current));
			}

			const BindingFlags allMethods = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
			foreach (var method in type.GetMethods(allMethods).Where(static method => method.IsDefined(typeof(Xunit.FactAttribute), true)).OrderBy(static method => method.Name, StringComparer.Ordinal))
			{
				var test = $"{type.Name}.{method.Name}";
				var categories = classCategories.Concat(GetCategories(method)).Distinct(StringComparer.Ordinal).ToList();

				foreach (var unknown in categories.Where(category => !knownCategories.Contains(category)))
				{
					violations.Add($"{test}: category \"{unknown}\" is not a {testCategoryType.Name} constant.");
				}

				var featureCategories = categories.Where(category => !additionalCategories.Contains(category, StringComparer.Ordinal)).ToList();
				if (featureCategories.Count is 0)
				{
					violations.Add($"{test}: no [Category({testCategoryType.Name}.X)] on the method or its class.");
				}
				else if (featureCategories.Count > 1)
				{
					violations.Add($"{test}: {featureCategories.Count} feature categories ({string.Join(", ", featureCategories)}); a test has exactly one.");
				}
			}
		}

		return violations;
	}

	static IEnumerable<string> GetCategories(MemberInfo member) =>
		member.GetCustomAttributesData()
			.Where(static attribute => attribute.AttributeType == typeof(CategoryAttribute))
			.SelectMany(static attribute => attribute.ConstructorArguments)
			.SelectMany(static argument => ToStrings(argument.Value));

	// CoreCLR returns the params string[] argument as a ReadOnlyCollection<CustomAttributeTypedArgument>; Mono (Android, iOS,
	// Mac Catalyst) returns another collection type.
	static IEnumerable<string> ToStrings(object? value) => value switch
	{
		string category => [category],
		CustomAttributeTypedArgument argument => ToStrings(argument.Value),
		System.Collections.IEnumerable items => items.Cast<object?>().SelectMany(ToStrings),
		_ => [],
	};
}