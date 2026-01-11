using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Xunit;
using Xunit.v3;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class ValidationBehaviorTests(ITestOutputHelper testOutputHelper) : BaseBehaviorTest<ValidationBehavior, VisualElement>(new MockValidationBehavior(), new View())
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
		validStyle.Setters.Add(new Setter() { Property = VisualElement.BackgroundColorProperty, Value = Colors.Green });

		var invalidStyle = new Style(entry.GetType());
		invalidStyle.Setters.Add(new Setter() { Property = VisualElement.BackgroundColorProperty, Value = Colors.Red });

		var behavior = new MockValidationBehavior()
		{
			ExpectedValue = "321",
			ValidStyle = validStyle,
			InvalidStyle = invalidStyle
		};

		entry.Behaviors.Add(behavior);

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

		entry.Behaviors.Add(behavior);

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
		var forceValidateTask = behavior.ForceValidate(TestContext.Current.CancellationToken);

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
			ForceValidateCommand = new Command<CancellationToken>(_ =>
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
	public void TestRemoveValidationBindingWithBindingContext()
	{
		var behavior = new MockValidationBehavior();
		var view = new View
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
		var view = new View();

		view.Behaviors.Add(behavior);

		Assert.IsType<MockValidationBehavior>(Assert.Single(view.Behaviors), exactMatch: false);

		view.Behaviors.Remove(behavior);

		Assert.Empty(view.Behaviors);
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