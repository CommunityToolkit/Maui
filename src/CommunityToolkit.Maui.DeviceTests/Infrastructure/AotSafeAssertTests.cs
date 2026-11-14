using Xunit.Sdk;

namespace CommunityToolkit.Maui.DeviceTests;

// Every NativeAOT-safe assert in the suite goes through AotSafeAssert, so a helper that never throws would turn every run green.
[Category(TestCategory.DeviceTestsInfrastructure)]
public class AotSafeAssertTests
{
	[Fact]
	public void SequenceEqualPassesForEqualSequencesAndTwoNulls()
	{
		AotSafeAssert.SequenceEqual(new[] { 1, 2, 3 }, new[] { 1, 2, 3 });
		AotSafeAssert.SequenceEqual(Array.Empty<int>(), Array.Empty<int>());
		AotSafeAssert.SequenceEqual<int>(null, null);
		AotSafeAssert.SequenceEqual(new[] { (1, "a"), (2, "b") }, new[] { (1, "a"), (2, "b") });
	}

	[Fact]
	public void SequenceEqualFailsWhenOnlyOneSideIsNull()
	{
		Assert.Throws<XunitException>(() => AotSafeAssert.SequenceEqual<int>(null, new[] { 1 }));
		Assert.Throws<XunitException>(() => AotSafeAssert.SequenceEqual<int>(new[] { 1 }, null));
	}

	[Fact]
	public void SequenceEqualFailsForALengthMismatchAndNamesTheFirstDifferingIndex()
	{
		var exception = Assert.Throws<XunitException>(() => AotSafeAssert.SequenceEqual(new[] { 1, 2 }, new[] { 1, 2, 3 }));

		Assert.Contains("index 2", exception.Message);
		Assert.Throws<XunitException>(() => AotSafeAssert.SequenceEqual(new[] { 1, 2, 3 }, new[] { 1, 2 }));
	}

	[Fact]
	public void SequenceEqualFailsForAValueTupleItemThatDiffers()
	{
		var exception = Assert.Throws<XunitException>(() => AotSafeAssert.SequenceEqual(new[] { (1, "a"), (2, "b") }, new[] { (1, "a"), (2, "c") }));

		Assert.Contains("index 1", exception.Message);
	}

	[Fact]
	public void SequenceEqualCapsTheItemsInItsFailureMessage()
	{
		var expected = Enumerable.Range(0, 10_000).ToArray();
		var actual = Enumerable.Range(0, 10_000).Select(i => i == 5 ? -1 : i).ToArray();

		var exception = Assert.Throws<XunitException>(() => AotSafeAssert.SequenceEqual(expected, actual));

		Assert.True(exception.Message.Length < 1_000, $"The message has {exception.Message.Length} characters.");
	}

	[Fact]
	public void ContainsAndDoesNotContainPassAndFailAsTheirNamesSay()
	{
		AotSafeAssert.Contains(2, new[] { 1, 2, 3 });
		AotSafeAssert.DoesNotContain(4, new[] { 1, 2, 3 });

		Assert.Throws<XunitException>(() => AotSafeAssert.Contains(4, new[] { 1, 2, 3 }));
		Assert.Throws<XunitException>(() => AotSafeAssert.DoesNotContain(2, new[] { 1, 2, 3 }));
	}

	[Fact]
	public void AllInspectsEveryItemAndReportsTheFirstFailure()
	{
		var inspected = new List<int>();

		AotSafeAssert.All(new[] { 1, 2, 3 }, inspected.Add);
		var exception = Assert.Throws<XunitException>(() => AotSafeAssert.All(new[] { 1, 2, 3 }, item => Assert.True(item < 3)));

		AotSafeAssert.SequenceEqual(new[] { 1, 2, 3 }, inspected);
		Assert.Contains("Item [2]", exception.Message);
	}

	[Fact]
	public void EmptyAndNotEmptyPassAndFailAsTheirNamesSay()
	{
		AotSafeAssert.Empty(Array.Empty<(int, int)>());
		AotSafeAssert.Empty(new List<double>());
		AotSafeAssert.NotEmpty(new List<double> { 1.5 });
		AotSafeAssert.NotEmpty(new[] { (1, 2) });

		Assert.Throws<XunitException>(() => AotSafeAssert.Empty(new List<double> { 1.5 }));
		Assert.Throws<XunitException>(() => AotSafeAssert.Empty(new[] { (1, 2) }));
		Assert.Throws<XunitException>(() => AotSafeAssert.NotEmpty(Array.Empty<byte>()));
	}

	[Fact]
	public void EqualComparesNullableValuesAndValueTuples()
	{
		AotSafeAssert.Equal<double?>(null, null);
		AotSafeAssert.Equal<double?>(1.5, 1.5);
		AotSafeAssert.Equal((1, 2), (1, 2));

		Assert.Throws<XunitException>(() => AotSafeAssert.Equal<double?>(null, 1.5));
		Assert.Throws<XunitException>(() => AotSafeAssert.Equal<int?>(1, 2));
		Assert.Throws<XunitException>(() => AotSafeAssert.Equal((1, 2), (1, 3)));
	}
}