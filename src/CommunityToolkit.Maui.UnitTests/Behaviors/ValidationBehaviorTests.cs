using CommunityToolkit.Maui.Behaviors;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class ValidationBehaviorTests : BaseTest
{
	[Fact]
	public void ValidateOnValueChanged()
	{
		// Arrange
		var entry = new Entry
		{
			Text = "123"
		};
		var behavior = new MockValidationBehavior()
		{
			ExpectedValue = "321",
			Flags = ValidationFlags.ValidateOnValueChanged
		};

		entry.Behaviors.Add(behavior);

		// Act
		entry.Text = "321";

		// Assert
		Assert.True(behavior.IsValid);
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ValidValue_ValidStyle()
	{
		// Arrange
		var entry = new Entry
		{
			Text = "123"
		};

		var validStyle = new Style(entry.GetType());
		validStyle.Setters.Add(new Setter() { Property = Entry.BackgroundColorProperty, Value = Colors.Green });

		var invalidStyle = new Style(entry.GetType());
		invalidStyle.Setters.Add(new Setter() { Property = Entry.BackgroundColorProperty, Value = Colors.Red });

		var behavior = new MockValidationBehavior()
		{
			ExpectedValue = "321",
			ValidStyle = validStyle,
			InvalidStyle = invalidStyle
		};

		entry.Behaviors.Add(behavior);

		// Act
		entry.Text = "321";
		await behavior.ForceValidate(CancellationToken.None);

		// Assert
		Assert.Equal(entry.Style, validStyle);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task InvalidValue_InvalidStyle()
	{
		// Arrange
		var entry = new Entry
		{
			Text = "123"
		};

		var validStyle = new Style(entry.GetType());
		validStyle.Setters.Add(new Setter() { Property = Entry.BackgroundColorProperty, Value = Colors.Green });

		var invalidStyle = new Style(entry.GetType());
		invalidStyle.Setters.Add(new Setter() { Property = Entry.BackgroundColorProperty, Value = Colors.Red });

		var behavior = new MockValidationBehavior()
		{
			ExpectedValue = "21",
			ValidStyle = validStyle,
			InvalidStyle = invalidStyle
		};

		entry.Behaviors.Add(behavior);

		// Act
		entry.Text = "321";
		await behavior.ForceValidate(CancellationToken.None);

		// Assert
		Assert.Equal(entry.Style, invalidStyle);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task IsRunning()
	{
		// Arrange
		var entry = new Entry
		{
			Text = "123"
		};
		var behavior = new MockValidationBehavior()
		{
			ExpectedValue = "321",
			SimulateValidationDelay = true
		};

		entry.Behaviors.Add(behavior);

		// Act
		entry.Text = "321";

		// Assert
		Assert.False(behavior.IsRunning);

		// Act
		var forceValidateTask = behavior.ForceValidate(CancellationToken.None);

		// Assert
		Assert.True(behavior.IsRunning);

		// Act
		await forceValidateTask;
	}

	[Fact]
	public void ForceValidateCommand()
	{
		// Arrange
		var entry = new Entry
		{
			Text = "123"
		};
		var behavior = new MockValidationBehavior()
		{
			ExpectedValue = "321",
			ForceValidateCommand = new Command<CancellationToken>(token =>
			{
				entry.Text = "321";
			})
		};

		entry.Behaviors.Add(behavior);

		// Act
		behavior.ForceValidateCommand.Execute(null);

		// Assert
		Assert.True(behavior.IsValid);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task CancellationTokenExpired()
	{
		// Arrange
		var behavior = new MockValidationBehavior();
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
		var behavior = new MockValidationBehavior();
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

	class MockValidationBehavior : ValidationBehavior<string>
	{
		public string? ExpectedValue { get; init; }
		public bool SimulateValidationDelay { get; init; } = false;

		protected override async ValueTask<bool> ValidateAsync(string? value, CancellationToken token)
		{
			if (SimulateValidationDelay)
			{
				await Task.Delay(500, token);
			}

			return value == ExpectedValue;
		}
	}
}