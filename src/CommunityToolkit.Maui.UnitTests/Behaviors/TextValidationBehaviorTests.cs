using System.Text.RegularExpressions;
using CommunityToolkit.Maui.Behaviors;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class TextValidationBehaviorTests() : BaseBehaviorTest<TextValidationBehavior, VisualElement>(new CharactersValidationBehavior(), new View())
{
	[Theory]
	[InlineData("mi.....ft", RegexOptions.IgnoreCase, 5, 25, TextDecorationFlags.None, "Microsoft", true)]
	[InlineData("mi.....ft", RegexOptions.None, 5, 25, TextDecorationFlags.Trim, "minecraft    ", true)]
	[InlineData("mi.....ft", RegexOptions.IgnoreCase, 5, 25, TextDecorationFlags.None, "microservice", false)]
	[InlineData("mi.....ft", RegexOptions.IgnoreCase, 5, 6, TextDecorationFlags.None, "Microsoft", false)]
	[InlineData("mi.....ft", RegexOptions.IgnoreCase, 10, 11, TextDecorationFlags.None, "Microsoft", false)]
	public async Task IsValid(string pattern, RegexOptions options, int minLength, int maxLength, TextDecorationFlags flags, string value, bool expectedValue)
	{
		// Arrange
		var behavior = new TextValidationBehavior
		{
			RegexPattern = pattern,
			RegexOptions = options,
			MinimumLength = minLength,
			MaximumLength = maxLength,
			DecorationFlags = flags
		};

		var entry = new Entry
		{
			Text = value
		};
		entry.Behaviors.Add(behavior);

		// Act
		await behavior.ForceValidate(CancellationToken.None);

		// Assert
		Assert.Equal(expectedValue, behavior.IsValid);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task CancellationTokenExpired()
	{
		// Arrange
		var behavior = new TextValidationBehavior();
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		var entry = new Entry
		{
			Text = "Hello"
		};
		entry.Behaviors.Add(behavior);

		// Act

		// Ensure CancellationToken expires
		await Task.Delay(100, CancellationToken.None);

		// Assert
		await Assert.ThrowsAsync<OperationCanceledException>(async () => await behavior.ForceValidate(cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task CancellationTokenCanceled()
	{
		// Arrange
		var behavior = new TextValidationBehavior();
		var cts = new CancellationTokenSource();

		var entry = new Entry
		{
			Text = "Hello"
		};
		entry.Behaviors.Add(behavior);

		// Act

		// Ensure CancellationToken expires
		await Task.Delay(100, CancellationToken.None);

		// Assert
		await Assert.ThrowsAsync<OperationCanceledException>(async () =>
		{
			await cts.CancelAsync();
			await behavior.ForceValidate(cts.Token);
		});
	}
}