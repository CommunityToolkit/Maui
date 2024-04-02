using CommunityToolkit.Maui.Behaviors;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class UriValidationBehaviorTests() : BaseBehaviorTest<UriValidationBehavior, VisualElement>(new UriValidationBehavior(), new View())
{
	[Theory]
	[InlineData(@"http://microsoft.com", UriKind.Absolute, true)]
	[InlineData(@"microsoft/xamarin/news", UriKind.Relative, true)]
	[InlineData(@"http://microsoft.com", UriKind.RelativeOrAbsolute, true)]
	[InlineData(@"microsoftcom", UriKind.Absolute, false)]
	[InlineData(@"microsoft\\\\\xamarin/news", UriKind.Relative, false)]
	[InlineData(@"ht\\\.com", UriKind.RelativeOrAbsolute, false)]
	public async Task IsValid(string value, UriKind uriKind, bool expectedValue)
	{
		// Arrange
		var behavior = new UriValidationBehavior
		{
			UriKind = uriKind,
			Value = value
		};

		// Act
		await behavior.ForceValidate(CancellationToken.None);

		// Assert
		Assert.Equal(expectedValue, behavior.IsValid);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task CancellationTokenExpired()
	{
		// Arrange
		var behavior = new UriValidationBehavior();
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
		var behavior = new UriValidationBehavior();
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