using System.Globalization;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class NumericValidationBehaviorTests() : BaseBehaviorTest<NumericValidationBehavior, VisualElement>(new NumericValidationBehavior(), new MockView())
{
	[Theory]
	[InlineData("en-US", "15.2", 1.0, 16.0, 0, 16, null, true)]
	[InlineData("en-US", "15.", 1.0, 16.0, 0, 1, null, true)]
	[InlineData("en-US", "15.88", 1.0, 16.0, 2, 2, null, true)]
	[InlineData("en-US", "0.99", 0.9, 2.0, 0, 16, null, true)]
	[InlineData("en-US", ".99", 0.9, 2.0, 0, 16, null, true)]
	[InlineData("en-US", "1,115.2", 1.0, 2000.0, 0, 16, null, true)]
	[InlineData("de-DE", "15,2", 1.0, 16.0, 0, 16, null, true)]
	[InlineData("de-DE", "15,", 1.0, 16.0, 0, 1, null, true)]
	[InlineData("de-DE", "15,88", 1.0, 16.0, 2, 2, null, true)]
	[InlineData("de-DE", "0,99", 0.9, 2.0, 0, 16, null, true)]
	[InlineData("de-DE", ",99", 0.9, 2.0, 0, 16, null, true)]
	[InlineData("de-DE", "1.115,2", 1.0, 2000.0, 0, 16, null, true)]
	[InlineData("en-US", "15.3", 16.0, 20.0, 0, 16, null, false)]
	[InlineData("en-US", "15.3", 0.0, 15.0, 0, 16, null, false)]
	[InlineData("en-US", "15.", 1.0, 16.0, 0, 0, null, false)]
	[InlineData("en-US", ".7", 0.0, 16.0, 0, 0, null, false)]
	[InlineData("en-US", "15", 1.0, 16.0, 1, 16, null, false)]
	[InlineData("en-US", "", 0.0, 16.0, 0, 16, null, false)]
	[InlineData("en-US", " ", 0.0, 16.0, 0, 16, null, false)]
	[InlineData("en-US", "15,2", 1.0, 16.0, 0, 16, null, false)]
	[InlineData("en-US", "1.115,2", 1.0, 2000.0, 0, 16, null, false)]
	[InlineData("de-DE", "15,3", 16.0, 20.0, 0, 16, null, false)]
	[InlineData("de-DE", "15,3", 0.0, 15.0, 0, 16, null, false)]
	[InlineData("de-DE", "15,", 1.0, 16.0, 0, 0, null, false)]
	[InlineData("de-DE", ",7", 0.0, 16.0, 0, 0, null, false)]
	[InlineData("de-DE", "15", 1.0, 16.0, 1, 16, null, false)]
	[InlineData("de-DE", "", 0.0, 16.0, 0, 16, null, false)]
	[InlineData("de-DE", " ", 0.0, 16.0, 0, 16, null, false)]
	[InlineData("de-DE", "15.2", 1.0, 16.0, 0, 16, null, false)]
	[InlineData("de-DE", "1,115.2", 1.0, 2000.0, 0, 16, null, false)]
	[InlineData("en-US", "15", 0.0, 100.0, 0, 16, 5.0, true)]
	[InlineData("en-US", "0", 0.0, 100.0, 0, 16, 5.0, true)]
	[InlineData("en-US", "100", 0.0, 100.0, 0, 16, 25.0, true)]
	[InlineData("en-US", "16", 0.0, 100.0, 0, 16, 5.0, false)]
	[InlineData("en-US", "7", 0.0, 100.0, 0, 16, 5.0, false)]
	[InlineData("en-US", "1.5", 0.0, 10.0, 0, 16, 0.5, true)]
	[InlineData("en-US", "1.25", 0.0, 10.0, 0, 16, 0.5, false)]
	[InlineData("en-US", "0.3", 0.0, 10.0, 0, 16, 0.1, true)]
	[InlineData("en-US", "15", 0.0, 100.0, 0, 16, -5.0, true)]
	[InlineData("en-US", "16", 0.0, 100.0, 0, 16, -5.0, false)]
	[InlineData("en-US", "7", 0.0, 100.0, 0, 16, 0.0, false)]
	[InlineData("en-US", "0", -100.0, 100.0, 0, 16, 0.0, true)]
	[InlineData("en-US", "-0", -100.0, 100.0, 0, 16, 0.0, true)]
	[InlineData("en-US", "0.0", -100.0, 100.0, 0, 16, 0.0, true)]
	[InlineData("de-DE", "0,0", -100.0, 100.0, 0, 16, 0.0, true)]
	[InlineData("en-US", "0", 0.0, 100.0, 0, 16, -0.0, true)]
	[InlineData("en-US", "-7", -100.0, 100.0, 0, 16, 0.0, false)]
	[InlineData("en-US", "0.0000001", -100.0, 100.0, 0, 16, 0.0, false)]
	[InlineData("en-US", "1e-300", -100.0, 100.0, 0, 16, 0.0, false)]
	[InlineData("de-DE", "0,5", -100.0, 100.0, 0, 16, 0.0, false)]
	[InlineData("en-US", "5", -100.0, 100.0, 0, 16, -0.0, false)]
	[InlineData("en-US", "0", 1.0, 100.0, 0, 16, 0.0, false)]
	[InlineData("en-US", "0.0", -100.0, 100.0, 0, 0, 0.0, false)]
	[InlineData("en-US", "-15", -100.0, 100.0, 0, 16, 5.0, true)]
	[InlineData("en-US", "-16", -100.0, 100.0, 0, 16, 5.0, false)]
	[InlineData("en-US", "-0.3", -100.0, 100.0, 0, 16, 0.1, true)]
	[InlineData("en-US", "-0.7", -100.0, 100.0, 0, 16, 0.1, true)]
	[InlineData("en-US", "-0.35", -100.0, 100.0, 0, 16, 0.1, false)]
	[InlineData("en-US", "-15", -100.0, 100.0, 0, 16, -5.0, true)]
	[InlineData("en-US", "-16", -100.0, 100.0, 0, 16, -5.0, false)]
	[InlineData("en-US", "0.3", -100.0, 100.0, 0, 16, -0.1, true)]
	[InlineData("de-DE", "0,3", -100.0, 100.0, 0, 16, 0.1, true)]
	[InlineData("de-DE", "0,35", -100.0, 100.0, 0, 16, 0.1, false)]
	[InlineData("de-DE", "1.000,5", 0.0, 2000.0, 0, 16, 0.5, true)]
	[InlineData("de-DE", "12.345,67", 0.0, 20000.0, 0, 16, 0.01, true)]
	[InlineData("de-DE", "12.345,675", 0.0, 20000.0, 0, 16, 0.01, false)]
	[InlineData("en-US", "1,000", 0.0, 2000.0, 0, 16, 5.0, true)]
	[InlineData("en-US", "1,001", 0.0, 2000.0, 0, 16, 5.0, false)]
	[InlineData("en-US", "12,345.67", 0.0, 20000.0, 0, 16, 0.01, true)]
	[InlineData("en-US", "1e3", -1000.0, 2000.0, 0, 16, 5.0, true)]
	[InlineData("en-US", "2.5e-1", -1000.0, 2000.0, 0, 16, 0.25, true)]
	[InlineData("en-US", "1.6e1", -1000.0, 2000.0, 0, 16, 5.0, false)]
	[InlineData("en-US", "1000000000", double.NegativeInfinity, double.PositiveInfinity, 0, 16, 0.1, true)]
	[InlineData("en-US", "-1000000000", double.NegativeInfinity, double.PositiveInfinity, 0, 16, 0.1, true)]
	[InlineData("en-US", "20000000", double.NegativeInfinity, double.PositiveInfinity, 0, 16, 0.1, true)]
	[InlineData("en-US", "99999999.99", double.NegativeInfinity, double.PositiveInfinity, 0, 16, 0.01, true)]
	[InlineData("en-US", "123456789.12", double.NegativeInfinity, double.PositiveInfinity, 0, 16, 0.01, true)]
	[InlineData("en-US", "999999999.9", double.NegativeInfinity, double.PositiveInfinity, 0, 16, 0.3, true)]
	[InlineData("de-DE", "1.000.000.000,1", double.NegativeInfinity, double.PositiveInfinity, 0, 16, 0.1, true)]
	[InlineData("en-US", "123456789012345", double.NegativeInfinity, double.PositiveInfinity, 0, 16, 5.0, true)]
	[InlineData("en-US", "99999999999999.9", double.NegativeInfinity, double.PositiveInfinity, 0, 16, 0.3, true)]
	[InlineData("en-US", "1000000000.05", double.NegativeInfinity, double.PositiveInfinity, 0, 16, 0.1, false)]
	[InlineData("en-US", "123456789.125", double.NegativeInfinity, double.PositiveInfinity, 0, 16, 0.01, false)]
	[InlineData("en-US", "1000000000", double.NegativeInfinity, double.PositiveInfinity, 0, 16, 0.3, false)]
	[InlineData("en-US", "1000000000000000", double.NegativeInfinity, double.PositiveInfinity, 0, 16, 3.0, false)]
	[InlineData("en-US", "0.00000000015", double.NegativeInfinity, double.PositiveInfinity, 0, int.MaxValue, 1e-10, false)]
	[InlineData("en-US", "1.00000000005", double.NegativeInfinity, double.PositiveInfinity, 0, int.MaxValue, 1e-10, false)]
	[InlineData("en-US", "0.0000000015", double.NegativeInfinity, double.PositiveInfinity, 0, int.MaxValue, 1e-9, false)]
	[InlineData("en-US", "1.5e-300", double.NegativeInfinity, double.PositiveInfinity, 0, int.MaxValue, 1e-300, false)]
	[InlineData("en-US", "0.0000000003", double.NegativeInfinity, double.PositiveInfinity, 0, int.MaxValue, 1e-10, true)]
	[InlineData("en-US", "1", double.NegativeInfinity, double.PositiveInfinity, 0, int.MaxValue, 1e-10, true)]
	[InlineData("en-US", "3e-300", double.NegativeInfinity, double.PositiveInfinity, 0, int.MaxValue, 1e-300, true)]
	[InlineData("en-US", "0.000000001", 0.0, 100.0, 0, 16, 5.0, false)]
	[InlineData("en-US", "15.0000000001", double.NegativeInfinity, double.PositiveInfinity, 0, 16, 5.0, false)]
	[InlineData("en-US", "14.9999999999", double.NegativeInfinity, double.PositiveInfinity, 0, 16, 5.0, false)]
	[InlineData("en-US", "0.3000000001", double.NegativeInfinity, double.PositiveInfinity, 0, 16, 0.1, false)]
	[InlineData("en-US", "15", double.NegativeInfinity, double.PositiveInfinity, 0, 16, 5.0000000001, false)]
	[InlineData("en-US", "9990.00000000001", double.NegativeInfinity, double.PositiveInfinity, 0, 16, 0.1, false)]
	[InlineData("en-US", "9989.99999999999", double.NegativeInfinity, double.PositiveInfinity, 0, 16, 0.3, false)]
	[InlineData("en-US", "1e400", double.NegativeInfinity, double.PositiveInfinity, 0, 16, 5.0, false)]
	[InlineData("en-US", "NaN", double.NegativeInfinity, double.PositiveInfinity, 0, 16, 5.0, false)]
	[InlineData("en-US", "1.5", 0.0, 100.0, 0, 0, 0.5, false)]
	[InlineData("en-US", "1.50", 0.0, 100.0, 2, 2, 0.5, true)]
	[InlineData("en-US", "1.3", 0.0, 100.0, 0, 1, 0.25, false)]
	[InlineData("en-US", "5", 1.0, 100.0, 0, 16, 5.0, true)]
	[InlineData("en-US", "6", 1.0, 100.0, 0, 16, 5.0, false)]
	[InlineData("en-US", "103", 3.0, 200.0, 0, 16, 10.0, false)]
	[InlineData("en-US", "105", 0.0, 100.0, 0, 16, 5.0, false)]
	[InlineData("en-US", "0", double.NegativeInfinity, double.PositiveInfinity, 0, 16, double.MaxValue, true)]
	[InlineData("en-US", "1", double.NegativeInfinity, double.PositiveInfinity, 0, 16, double.MaxValue, false)]
	[InlineData("en-US", "0", double.NegativeInfinity, double.PositiveInfinity, 0, 16, double.Epsilon, true)]
	[InlineData("en-US", "0.999", double.NegativeInfinity, double.PositiveInfinity, 0, 16, 0.333, true)]
	[InlineData("en-US", "1", double.NegativeInfinity, double.PositiveInfinity, 0, 16, 0.333, false)]
	public async Task IsValid(string culture, string? value, double minValue, double maxValue, int minDecimalPlaces, int maxDecimalPlaces, double? interval, bool expectedValue)
	{
		// Arrange
		var origCulture = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);

		var behavior = new NumericValidationBehavior
		{
			MinimumValue = minValue,
			MaximumValue = maxValue,
			MinimumDecimalPlaces = minDecimalPlaces,
			MaximumDecimalPlaces = maxDecimalPlaces,
			Interval = interval
		};

		var entry = new Entry
		{
			Text = value
		};
		entry.Behaviors.Add(behavior);

		try
		{
			// Act
			await behavior.ForceValidate(TestContext.Current.CancellationToken);

			// Assert
			Assert.Equal(expectedValue, behavior.IsValid);
		}
		finally
		{
			CultureInfo.CurrentCulture = origCulture;
		}
	}

	[Theory(Timeout = (int)TestDuration.Short)]
	[InlineData("en-US", double.PositiveInfinity, 5.0)]
	[InlineData("en-US", double.NegativeInfinity, 5.0)]
	[InlineData("en-US", double.PositiveInfinity, 0.0)]
	[InlineData("de-DE", double.PositiveInfinity, 0.5)]
	public async Task IsValid_InfiniteValueWithInterval_ShouldBeInvalid(string culture, double infiniteValue, double interval)
	{
		// Arrange
		var origCulture = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);

		// MinimumValue and MaximumValue keep their infinite defaults, so only the Interval check can reject an infinite value
		var behavior = new NumericValidationBehavior
		{
			Interval = interval
		};

		var entry = new Entry
		{
			// Use the culture's own symbol, e.g. "∞", so the text parses back to infiniteValue
			Text = infiniteValue.ToString(CultureInfo.CurrentCulture)
		};
		entry.Behaviors.Add(behavior);

		try
		{
			// Ensure Text parses back to infiniteValue; otherwise double.TryParse, not the Interval check, would make the behavior invalid
			Assert.True(double.TryParse(entry.Text, out var parsedValue));
			Assert.Equal(infiniteValue, parsedValue);

			// Act
			await behavior.ForceValidate(TestContext.Current.CancellationToken);

			// Assert
			Assert.False(behavior.IsValid);
		}
		finally
		{
			CultureInfo.CurrentCulture = origCulture;
		}
	}

	[Theory(Timeout = (int)TestDuration.Medium)]
	[InlineData("en-US", 0.01, "0", "0.01")]
	[InlineData("de-DE", 0.01, "0", "0.01")]
	[InlineData("en-US", 0.01, "-100", "0.01")]
	[InlineData("en-US", 0.1, "0", "0.1")]
	[InlineData("en-US", 0.01, "100000000", "0.01")]
	[InlineData("en-US", 0.1, "1000000000", "0.1")]
	[InlineData("en-US", 1e-10, "0", "0.0000000001")]
	public async Task IsValid_EveryMultipleOfIntervalIsValid_AndEveryHalfStepIsInvalid(string culture, double interval, string start, string step)
	{
		// Arrange
		const int multiplesToValidate = 10_000;

		var origCulture = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);

		var startValue = decimal.Parse(start, CultureInfo.InvariantCulture);
		var stepValue = decimal.Parse(step, CultureInfo.InvariantCulture);

		var behavior = new NumericValidationBehavior
		{
			Interval = interval
		};

		var entry = new Entry();
		entry.Behaviors.Add(behavior);

		try
		{
			for (var i = 0; i <= multiplesToValidate; i++)
			{
				// Use decimal arithmetic so that each Text is exactly what a user would type, e.g. "0.3" instead of the double result of 3 * 0.1 (0.30000000000000004)
				var multiple = (startValue + (i * stepValue)).ToString(CultureInfo.CurrentCulture);
				var halfStep = (startValue + (i * stepValue) + (stepValue / 2)).ToString(CultureInfo.CurrentCulture);

				// Act
				entry.Text = multiple;
				await behavior.ForceValidate(TestContext.Current.CancellationToken);

				// Assert
				Assert.True(behavior.IsValid, $"\"{multiple}\" is a multiple of {interval} and should be valid");

				// Act
				entry.Text = halfStep;
				await behavior.ForceValidate(TestContext.Current.CancellationToken);

				// Assert
				Assert.False(behavior.IsValid, $"\"{halfStep}\" is not a multiple of {interval} and should be invalid");
			}
		}
		finally
		{
			CultureInfo.CurrentCulture = origCulture;
		}
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task IsValid_ShouldUseLatestInterval_WhenIntervalChanges()
	{
		// Arrange
		var behavior = new NumericValidationBehavior
		{
			Interval = 5.0
		};

		var entry = new Entry
		{
			Text = "15"
		};
		entry.Behaviors.Add(behavior);

		// Act
		await behavior.ForceValidate(TestContext.Current.CancellationToken);

		// Assert
		Assert.True(behavior.IsValid);

		// Act
		behavior.Interval = 4.0;
		await behavior.ForceValidate(TestContext.Current.CancellationToken);

		// Assert
		Assert.False(behavior.IsValid);

		// Act
		behavior.Interval = -3.0;
		await behavior.ForceValidate(TestContext.Current.CancellationToken);

		// Assert
		Assert.True(behavior.IsValid);

		// Act
		behavior.Interval = 0.0;
		await behavior.ForceValidate(TestContext.Current.CancellationToken);

		// Assert
		Assert.False(behavior.IsValid);

		// Act
		behavior.Interval = null;
		await behavior.ForceValidate(TestContext.Current.CancellationToken);

		// Assert
		Assert.True(behavior.IsValid);
	}

	[Fact]
	public void IsValid_ShouldRevalidate_WhenIntervalChangesAndValidateOnValueChangedIsSet()
	{
		// Arrange
		var behavior = new NumericValidationBehavior
		{
			Flags = ValidationFlags.ValidateOnValueChanged,
			Interval = 5.0
		};

		// Set Text after attaching the behavior: when Text is already set, ValidationBehavior.OnAttachedTo holds its semaphore while the Value binding updates,
		// so the ValidateOnValueChanged validation would resume later on a thread-pool thread instead of completing before the asserts below
		var entry = new Entry();
		entry.Behaviors.Add(behavior);

		// Act
		entry.Text = "7";

		// Assert
		Assert.False(behavior.IsValid);

		// Act
		behavior.Interval = 7.0;

		// Assert
		Assert.True(behavior.IsValid);

		// Act
		behavior.Interval = 0.0;

		// Assert
		Assert.False(behavior.IsValid);

		// Act
		behavior.Interval = null;

		// Assert
		Assert.True(behavior.IsValid);

		// Act
		behavior.Interval = 5.0;

		// Assert
		Assert.False(behavior.IsValid);
	}

	[Theory]
	[InlineData(0.0)]
	[InlineData(5.0)]
	[InlineData(-5.0)]
	[InlineData(1e-300)]
	[InlineData(double.Epsilon)]
	[InlineData(-double.Epsilon)]
	[InlineData(double.MaxValue)]
	[InlineData(double.MinValue)]
	public void SetFiniteIntervalValue_ShouldBeAccepted(double interval)
	{
		// Arrange
		var behavior = new NumericValidationBehavior
		{
			Interval = 1.0
		};

		// Act
		behavior.Interval = interval;

		// Assert
		Assert.Equal(interval, behavior.Interval);
	}

	[Theory]
	[InlineData(5.0, double.NaN)]
	[InlineData(5.0, double.PositiveInfinity)]
	[InlineData(5.0, double.NegativeInfinity)]
	[InlineData(0.0, double.NaN)]
	[InlineData(0.0, double.PositiveInfinity)]
	[InlineData(0.0, double.NegativeInfinity)]
	[InlineData(-0.5, double.NaN)]
	[InlineData(null, double.NaN)]
	[InlineData(null, double.PositiveInfinity)]
	public void SetInvalidIntervalValue_ShouldKeepPreviousValue(double? previousInterval, double invalidInterval)
	{
		// Arrange
		var behavior = new NumericValidationBehavior
		{
			Interval = previousInterval
		};

		// Act
		behavior.Interval = invalidInterval;

		// Assert - .NET MAUI ignores a value rejected by ValidateInterval (logging a warning) instead of throwing, so the previous value is kept
		Assert.Equal(previousInterval, behavior.Interval);
	}

	[Theory(Timeout = (int)TestDuration.Short)]
	[InlineData("15", 5.0, true)]
	[InlineData("7", 5.0, false)]
	[InlineData("0", 0.0, true)]
	[InlineData("7", 0.0, false)]
	public async Task SetInvalidIntervalValue_ShouldKeepEnforcingPreviousInterval(string value, double previousInterval, bool expectedValue)
	{
		// Arrange
		var behavior = new NumericValidationBehavior
		{
			Interval = previousInterval
		};

		var entry = new Entry
		{
			Text = value
		};
		entry.Behaviors.Add(behavior);

		// Act
		behavior.Interval = double.NaN;
		await behavior.ForceValidate(TestContext.Current.CancellationToken);

		// Assert
		Assert.Equal(previousInterval, behavior.Interval);
		Assert.Equal(expectedValue, behavior.IsValid);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task IsNull()
	{
		// Arrange
		string? text = null;

		var behavior = new NumericValidationBehavior();

		var entry = new Entry
		{
			Text = text
		};
		entry.Behaviors.Add(behavior);

		//Act
		await behavior.ForceValidate(TestContext.Current.CancellationToken);

		// Assert
		Assert.False(behavior.IsValid);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShouldNotThrowIsNull()
	{
		var options = new Options();
		options.SetShouldSuppressExceptionsInBehaviors(true);

		// Arrange
		string? text = null;

		var behavior = new NumericValidationBehavior();

		var entry = new Entry
		{
			Text = text
		};
		entry.Behaviors.Add(behavior);

		var action = (async () => await behavior.ForceValidate(TestContext.Current.CancellationToken));
		await action.Should().NotThrowAsync<ArgumentNullException>();

		options.SetShouldSuppressExceptionsInBehaviors(false);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task CancellationTokenExpired()
	{
		// Arrange
		var behavior = new NumericValidationBehavior();
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		var entry = new Entry
		{
			Text = "Hello"
		};
		entry.Behaviors.Add(behavior);

		// Act

		// Ensure CancellationToken expires
		await Task.Delay(100, TestContext.Current.CancellationToken);

		// Assert
		await Assert.ThrowsAsync<OperationCanceledException>(async () => await behavior.ForceValidate(cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task CancellationTokenCanceled()
	{
		// Arrange
		var behavior = new NumericValidationBehavior();
		var cts = new CancellationTokenSource();

		var entry = new Entry
		{
			Text = "Hello"
		};
		entry.Behaviors.Add(behavior);

		// Act

		// Ensure CancellationToken expires
		await Task.Delay(100, TestContext.Current.CancellationToken);

		// Assert
		await Assert.ThrowsAsync<OperationCanceledException>(async () =>
		{
			await cts.CancelAsync();
			await behavior.ForceValidate(cts.Token);
		});
	}

	[Fact]
	public void VerifyDefaults()
	{
		// Arrange
		var numericValidationBehavior = new NumericValidationBehavior();

		// Act Assert
		Assert.Equal(NumericValidationBehaviorDefaults.MaximumDecimalPlaces, numericValidationBehavior.MaximumDecimalPlaces);
		Assert.Equal(NumericValidationBehaviorDefaults.MaximumValue, numericValidationBehavior.MaximumValue);
		Assert.Equal(NumericValidationBehaviorDefaults.MinimumDecimalPlaces, numericValidationBehavior.MinimumDecimalPlaces);
		Assert.Equal(NumericValidationBehaviorDefaults.MinimumValue, numericValidationBehavior.MinimumValue);
		Assert.Null(numericValidationBehavior.Interval);
	}
}