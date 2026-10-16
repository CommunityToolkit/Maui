using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Xunit;
using Xunit.v3;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class ValidationBehaviorTests(ITestOutputHelper testOutputHelper) : BaseBehaviorTest<ValidationBehavior, VisualElement>(new MockValidationBehavior(), new MockView())
{
	public enum CancellationHandling
	{
		ThrowsTaskCanceledException,
		ThrowsOperationCanceledException,
		IgnoresToken
	}

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

		RunOnUIThread(() => entry.Behaviors.Add(behavior));

		// Act
		entry.Text = "321";

		// Assert
		Assert.True(behavior.IsValid);
	}

	[Fact]
	public void ValueChange_ShouldNotValidate_WhenFlagsDoesNotIncludeValidateOnValueChanged()
	{
		var behavior = new MockValidationBehavior
		{
			ExpectedValue = "321",
			Flags = ValidationFlags.ValidateOnUnfocused
		};

		var entry = new Entry();
		entry.Behaviors.Add(behavior);

		((IView)entry).IsFocused = true;
		entry.Text = "123";
		((IView)entry).IsFocused = false;

		Assert.False(behavior.IsValid);

		((IView)entry).IsFocused = true;
		entry.Text = "321";

		Assert.False(behavior.IsValid);

		((IView)entry).IsFocused = false;

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
		validStyle.Setters.Add(new Setter() { Property = VisualElement.BackgroundColorProperty, Value = Colors.Green });

		var invalidStyle = new Style(entry.GetType());
		invalidStyle.Setters.Add(new Setter() { Property = VisualElement.BackgroundColorProperty, Value = Colors.Red });

		var behavior = new MockValidationBehavior()
		{
			ExpectedValue = "321",
			ValidStyle = validStyle,
			InvalidStyle = invalidStyle
		};

		RunOnUIThread(() => entry.Behaviors.Add(behavior));

		// Act
		entry.Text = "321";
		await behavior.ForceValidate(TestContext.Current.CancellationToken);

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
		validStyle.Setters.Add(new Setter() { Property = VisualElement.BackgroundColorProperty, Value = Colors.Green });

		var invalidStyle = new Style(entry.GetType());
		invalidStyle.Setters.Add(new Setter() { Property = VisualElement.BackgroundColorProperty, Value = Colors.Red });

		var behavior = new MockValidationBehavior()
		{
			ExpectedValue = "21",
			ValidStyle = validStyle,
			InvalidStyle = invalidStyle
		};

		RunOnUIThread(() => entry.Behaviors.Add(behavior));

		// Act
		entry.Text = "321";
		await behavior.ForceValidate(TestContext.Current.CancellationToken);

		// Assert
		Assert.Equal(entry.Style, invalidStyle, (style1, style2) =>
		{
			if (style1 == null || style2 == null)
			{
				return style1 == style2;
			}

			testOutputHelper.WriteLine($"Style1: {style1.Setters.Count} - Style2: {style2.Setters.Count}");
			testOutputHelper.WriteLine($"Style1: {style1.TargetType.FullName} - Style2: {style2.TargetType.FullName}");

			return style1.Setters.Count == style2.Setters.Count
				   && style1.TargetType.FullName == style2.TargetType.FullName
				   && style1.Setters.All(x => style2.Setters.Contains(x, new StyleSetterComparer(testOutputHelper)));
		});
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public void IsRunning()
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

		RunOnUIThread(() => entry.Behaviors.Add(behavior));

		// Act
		entry.Text = "321";

		// Assert
		Assert.False(behavior.IsRunning);

		RunOnUIThread(async () =>
		{
			// Act
			var forceValidateTask = behavior.ForceValidate(TestContext.Current.CancellationToken);

			// Assert
			Assert.True(behavior.IsRunning);

			// Act
			await forceValidateTask;
		});
	}

	[Theory(Timeout = (int)TestDuration.Short)]
	[InlineData(CancellationHandling.ThrowsTaskCanceledException)]
	[InlineData(CancellationHandling.ThrowsOperationCanceledException)]
	[InlineData(CancellationHandling.IgnoresToken)]
	public void IsRunning_IsFalse_WhenForceValidateIsCancelledMidValidation(CancellationHandling cancellationHandling)
	{
		var entry = new Entry();
		var behavior = new GatedValidationBehavior(cancellationHandling);
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

		RunOnUIThread(async () =>
		{
			entry.Behaviors.Add(behavior);

			var forceValidateTask = behavior.ForceValidate(cts.Token);
			Assert.True(behavior.IsRunning);

			await cts.CancelAsync();
			behavior.CompleteAll(isValid: false);

			try
			{
				await forceValidateTask;
			}
			catch (OperationCanceledException)
			{
				// A validator that throws OperationCanceledException surfaces it to the caller, and this test is about IsRunning, not about that exception
			}

			Assert.False(behavior.IsRunning);
			Assert.True(behavior.IsValid);
		});
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public void IsRunning_IsFalse_WhenTheValidatorTimesOut()
	{
		var entry = new Entry();
		var behavior = new GatedValidationBehavior(CancellationHandling.IgnoresToken);

		RunOnUIThread(async () =>
		{
			entry.Behaviors.Add(behavior);

			var forceValidateTask = behavior.ForceValidate(TestContext.Current.CancellationToken);
			Assert.True(behavior.IsRunning);

			behavior.TimeOut(validationIndex: 0);
			await forceValidateTask;

			Assert.False(behavior.IsRunning);
			Assert.True(behavior.IsValid);
		});
	}

	[Fact]
	public void IsRunning_StaysTrueUntilTheNewerValidationFinishes_WhenAValidationIsSuperseded()
	{
		var entry = new Entry();
		var behavior = new GatedValidationBehavior(CancellationHandling.IgnoresToken)
		{
			Flags = ValidationFlags.ValidateOnValueChanged
		};

		RunOnUIThread(() =>
		{
			entry.Behaviors.Add(behavior);

			try
			{
				entry.Text = "first";
				entry.Text = "second";
				Assert.Equal(2, behavior.ValidationCount);

				behavior.Complete(validationIndex: 0, isValid: false);

				Assert.True(behavior.IsRunning);
				Assert.True(behavior.IsValid);

				behavior.Complete(validationIndex: 1, isValid: false);

				Assert.False(behavior.IsRunning);
				Assert.False(behavior.IsValid);
			}
			finally
			{
				behavior.CompleteAll(isValid: false);
			}
		});
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public void IsRunning_StaysTrue_WhenForceValidateIsCancelledWhileANewerValidationRuns()
	{
		var entry = new Entry();
		var behavior = new GatedValidationBehavior(CancellationHandling.IgnoresToken)
		{
			Flags = ValidationFlags.ValidateOnValueChanged
		};
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

		RunOnUIThread(async () =>
		{
			entry.Behaviors.Add(behavior);

			try
			{
				var forceValidateTask = behavior.ForceValidate(cts.Token);
				entry.Text = "newer";
				Assert.Equal(2, behavior.ValidationCount);

				await cts.CancelAsync();
				behavior.Complete(validationIndex: 0, isValid: false);
				await forceValidateTask;

				Assert.True(behavior.IsRunning);

				behavior.Complete(validationIndex: 1, isValid: true);

				Assert.False(behavior.IsRunning);
			}
			finally
			{
				behavior.CompleteAll(isValid: true);
			}
		});
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
			ForceValidateCommand = new Command<CancellationToken>(_ =>
			{
				entry.Text = "321";
			})
		};

		RunOnUIThread(() => entry.Behaviors.Add(behavior));

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
		RunOnUIThread(() => entry.Behaviors.Add(behavior));

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
		var behavior = new MockValidationBehavior();
		var cts = new CancellationTokenSource();

		var entry = new Entry
		{
			Text = "Hello"
		};
		RunOnUIThread(() => entry.Behaviors.Add(behavior));

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
	public void TestRemoveValidationBindingWithBindingContext()
	{
		var behavior = new MockValidationBehavior();
		var view = new MockView
		{
			BindingContext = new MockPageViewModel()
		};

		view.Behaviors.Add(behavior);

		Assert.IsType<MockValidationBehavior>(Assert.Single(view.Behaviors), exactMatch: false);

		view.Behaviors.Remove(behavior);

		Assert.Empty(view.Behaviors);
	}

	[Fact]
	public void TestRemoveValidationBindingWithoutBindingContext()
	{
		var behavior = new MockValidationBehavior();
		var view = new MockView();

		view.Behaviors.Add(behavior);

		Assert.IsType<MockValidationBehavior>(Assert.Single(view.Behaviors), exactMatch: false);

		view.Behaviors.Remove(behavior);

		Assert.Empty(view.Behaviors);
	}

	[Fact]
	public async Task ValidateAsync_ReceivesTheValueReturnedByDecorate()
	{
		var behavior = new UppercaseDecoratingValidationBehavior
		{
			ExpectedValue = "ABC"
		};

		var entry = new Entry();
		entry.Behaviors.Add(behavior);
		entry.Text = "abc";

		await behavior.ForceValidate(TestContext.Current.CancellationToken);

		Assert.Equal("ABC", behavior.ValidatedValue);
		Assert.True(behavior.IsValid);
	}

	[Fact]
	public void VerifyDefaults()
	{
		// Arrange
		var behavior = new MockValidationBehavior();

		// Act Assert
		Assert.Equal(ValidationBehaviorDefaults.IsNotValid, behavior.IsNotValid);
		Assert.Equal(ValidationBehaviorDefaults.IsValid, behavior.IsValid);
		Assert.Equal(ValidationBehaviorDefaults.IsRunning, behavior.IsRunning);
		Assert.Equal(ValidationBehaviorDefaults.ValidStyle, behavior.ValidStyle);
		Assert.Equal(ValidationBehaviorDefaults.InvalidStyle, behavior.InvalidStyle);
		Assert.Equal(ValidationBehaviorDefaults.Value, behavior.Value);
		Assert.Equal(ValidationBehaviorDefaults.ValuePropertyName, behavior.ValuePropertyName);
		Assert.Equal(ValidationBehaviorDefaults.Flags, behavior.Flags);

	}

	sealed class GatedValidationBehavior(CancellationHandling cancellationHandling) : ValidationBehavior<string>
	{
		readonly List<TaskCompletionSource<bool>> validations = [];

		public int ValidationCount => validations.Count;

		public void Complete(int validationIndex, bool isValid) => validations[validationIndex].TrySetResult(isValid);

		// HttpClient reports a timeout as a TaskCanceledException although no token was cancelled
		public void TimeOut(int validationIndex) => validations[validationIndex].TrySetCanceled();

		// A validation that never completes keeps AsyncContext.Run waiting, so a test completes them all in a finally block to fail on a broken assertion instead of hanging
		public void CompleteAll(bool isValid)
		{
			foreach (var validation in validations)
			{
				validation.TrySetResult(isValid);
			}
		}

		protected override async ValueTask<bool> ValidateAsync(string? value, CancellationToken token)
		{
			var validation = new TaskCompletionSource<bool>();
			validations.Add(validation);

			if (cancellationHandling is CancellationHandling.ThrowsTaskCanceledException)
			{
				return await validation.Task.WaitAsync(token);
			}

			var isValid = await validation.Task;

			if (cancellationHandling is CancellationHandling.ThrowsOperationCanceledException)
			{
				token.ThrowIfCancellationRequested();
			}

			return isValid;
		}
	}

	sealed class UppercaseDecoratingValidationBehavior : ValidationBehavior<string>
	{
		public string? ExpectedValue { get; init; }

		public string? ValidatedValue { get; private set; }

		protected override string? Decorate(string? value) => value?.ToUpperInvariant();

		protected override ValueTask<bool> ValidateAsync(string? value, CancellationToken token)
		{
			ValidatedValue = value;
			return ValueTask.FromResult(value == ExpectedValue);
		}
	}
}

public class StyleSetterComparer(ITestOutputHelper testOutputHelper) : IEqualityComparer<Setter>
{
	public bool Equals(Setter? x, Setter? y)
	{
		if (ReferenceEquals(x, y))
		{
			return true;
		}

		if (x is null)
		{
			return false;
		}

		if (y is null)
		{
			return false;
		}

		if (x.GetType() != y.GetType())
		{
			return false;
		}


		testOutputHelper.WriteLine($"Setter1: {x.TargetName},{x.Property.PropertyName} - Setter2: {y.TargetName},{y.Property.PropertyName}");
		return x.TargetName == y.TargetName && x.Property.PropertyName == y.Property.PropertyName;
	}

	public int GetHashCode(Setter obj)
	{
		return HashCode.Combine(obj.TargetName, obj.Property);
	}
}