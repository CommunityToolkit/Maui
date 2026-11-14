using System.Globalization;
using CommunityToolkit.Maui.Behaviors;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;

namespace CommunityToolkit.Maui.DeviceTests;

// ValuePropertyName used to be resolved with a string-path Binding, which full trimming and NativeAOT break. A BindableProperty name
// is now followed through a compiled binding, and anything else must be a compiled binding on Value with ValuePropertyName set to null.
[Category(TestCategory.ValidationBehavior)]
[Collection(TestCollections.DisableParallelization)]
public sealed class ValidationBehaviorValuePropertyNameTests : CommunityToolkitHandlerTestBase
{
	[Fact]
	public async Task ValueFollowsTheTextOfAnEntry()
	{
		SetupBuilder();
		var behavior = new CapturingValidationBehavior { ValuePropertyName = nameof(Entry.Text) };
		var entry = new Entry { WidthRequest = 200 };
		entry.Behaviors.Add(behavior);

		await AttachAndRun(entry, async _ =>
		{
			await entry.OnLoadedAsync();

			entry.Text = "one";
			await AssertValueEventually(behavior, "one");

			entry.Text = "two";
			await AssertValueEventually(behavior, "two");
		});
	}

	[Fact]
	public async Task ValueFollowsIsCheckedOfACheckBox()
	{
		SetupBuilder();
		var behavior = new CapturingValidationBehavior { ValuePropertyName = nameof(CheckBox.IsChecked) };
		var checkBox = new CheckBox();
		checkBox.Behaviors.Add(behavior);

		await AttachAndRun(checkBox, async _ =>
		{
			await checkBox.OnLoadedAsync();

			checkBox.IsChecked = true;
			await AssertValueEventually(behavior, true);

			checkBox.IsChecked = false;
			await AssertValueEventually(behavior, false);
		});
	}

	[Fact]
	public async Task ValueFollowsTheDateOfADatePicker()
	{
		SetupBuilder();
		var firstDate = new DateTime(2026, 11, 13);
		var secondDate = new DateTime(2027, 1, 2);
		var behavior = new CapturingValidationBehavior { ValuePropertyName = nameof(DatePicker.Date) };
		var datePicker = new DatePicker();
		datePicker.Behaviors.Add(behavior);

		await AttachAndRun(datePicker, async _ =>
		{
			await datePicker.OnLoadedAsync();

			datePicker.Date = firstDate;
			await AssertValueEventually(behavior, firstDate);

			datePicker.Date = secondDate;
			await AssertValueEventually(behavior, secondDate);
		});
	}

	[Fact]
	public async Task ValueFollowsAnInheritedBindableProperty()
	{
		SetupBuilder();
		var behavior = new CapturingValidationBehavior { ValuePropertyName = nameof(VisualElement.WidthRequest) };
		var entry = new Entry { WidthRequest = 200 };
		entry.Behaviors.Add(behavior);

		await AttachAndRun(entry, async _ =>
		{
			await entry.OnLoadedAsync();

			await AssertValueEventually(behavior, 200d);

			entry.WidthRequest = 250;
			await AssertValueEventually(behavior, 250d);
		});
	}

	[Fact]
	public async Task ValueFollowsACustomBindablePropertyOfACustomView()
	{
		SetupBuilder();
		var behavior = new CapturingValidationBehavior { ValuePropertyName = nameof(AmountView.Amount) };
		var view = new AmountView();
		view.Behaviors.Add(behavior);

		await AttachAndRun(view, async _ =>
		{
			await view.OnLoadedAsync();

			view.Amount = 5;
			await AssertValueEventually(behavior, 5);
		});
	}

	[Theory]
	[InlineData("en-US")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public async Task ValueFollowsTheExactPropertyNameUnderEachCurrentCulture(string cultureName)
	{
		SetupBuilder();
		var behavior = new CapturingValidationBehavior { ValuePropertyName = nameof(CheckBox.IsChecked) };
		var checkBox = new CheckBox();

		await AttachAndRun(checkBox, async _ =>
		{
			await RunWithCurrentCulture(cultureName, async () =>
			{
				await checkBox.OnLoadedAsync();
				checkBox.Behaviors.Add(behavior);

				checkBox.IsChecked = true;
				await AssertValueEventually(behavior, true);
			});
		});
	}

	[Fact]
	public async Task CompiledBindingOnValueFollowsAClrPropertyWhenValuePropertyNameIsNull()
	{
		SetupBuilder();
		var view = new ClrPropertyView();
		var behavior = new CapturingValidationBehavior { ValuePropertyName = null };
		behavior.SetBinding(ValidationBehavior.ValueProperty, static (ClrPropertyView source) => source.Amount, source: view);
		view.Behaviors.Add(behavior);

		await AttachAndRun(view, async _ =>
		{
			await view.OnLoadedAsync();

			view.Amount = "first";
			await AssertValueEventually(behavior, "first");

			view.Amount = "second";
			await AssertValueEventually(behavior, "second");
		});
	}

	[Fact]
	public async Task BindingCreatedInCodeOnValueFollowsAClrPropertyWhenValuePropertyNameIsNull()
	{
		SetupBuilder();
		var view = new ClrPropertyView();
		var behavior = new CapturingValidationBehavior { ValuePropertyName = null };
		behavior.SetBinding(ValidationBehavior.ValueProperty, BindingBase.Create(static (ClrPropertyView source) => source.Amount, source: view));
		view.Behaviors.Add(behavior);

		await AttachAndRun(view, async _ =>
		{
			await view.OnLoadedAsync();

			view.Amount = "bound";
			await AssertValueEventually(behavior, "bound");
		});
	}

	[Fact]
	public async Task ClrPropertyNameThrowsAnArgumentExceptionSynchronouslyWhenTheBehaviorIsAttached()
	{
		SetupBuilder();
		var behavior = new CapturingValidationBehavior { ValuePropertyName = nameof(ClrPropertyView.Amount) };
		var view = new ClrPropertyView();

		await AttachAndRun(view, async _ =>
		{
			await view.OnLoadedAsync();

			var exception = Assert.Throws<ArgumentException>(() => view.Behaviors.Add(behavior));

			Assert.Equal(nameof(ValidationBehavior.ValuePropertyName), exception.ParamName);
			Assert.Contains(nameof(ClrPropertyView.Amount), exception.Message, StringComparison.Ordinal);
			Assert.Contains("x:Null", exception.Message, StringComparison.Ordinal);
		});
	}

	[Fact]
	public async Task DottedPathNameThrowsAnArgumentExceptionSynchronouslyWhenTheBehaviorIsAttached()
	{
		SetupBuilder();
		var behavior = new CapturingValidationBehavior { ValuePropertyName = "Text.Length" };
		var entry = new Entry { WidthRequest = 200 };

		await AttachAndRun(entry, async _ =>
		{
			await entry.OnLoadedAsync();

			Assert.Throws<ArgumentException>(() => entry.Behaviors.Add(behavior));
		});
	}

	[Theory]
	[InlineData("en-US", "ISCHECKED")]
	[InlineData("en-US", "isChecked")]
	[InlineData("tr-TR", "ISCHECKED")]
	[InlineData("tr-TR", "isChecked")]
	[InlineData("az-Latn-AZ", "ISCHECKED")]
	[InlineData("az-Latn-AZ", "isChecked")]
	public async Task NameThatDiffersOnlyByCasingThrowsAnArgumentExceptionOnAttachUnderEachCurrentCulture(string cultureName, string valuePropertyName)
	{
		SetupBuilder();
		var behavior = new CapturingValidationBehavior { ValuePropertyName = valuePropertyName };
		var checkBox = new CheckBox();

		await AttachAndRun(checkBox, async _ =>
		{
			await RunWithCurrentCulture(cultureName, async () =>
			{
				await checkBox.OnLoadedAsync();

				Assert.Throws<ArgumentException>(() => checkBox.Behaviors.Add(behavior));
			});
		});
	}

	[Fact]
	public async Task ClrPropertyNameDoesNotThrowAndLeavesValueUnboundWhenExceptionsInBehaviorsAreSuppressed()
	{
		SetupBuilder(configureToolkitOptions: static options => options.SetShouldSuppressExceptionsInBehaviors(true));
		var behavior = new CapturingValidationBehavior { ValuePropertyName = nameof(ClrPropertyView.Amount) };
		var view = new ClrPropertyView { Amount = "ignored" };

		await AttachAndRun(view, async _ =>
		{
			await view.OnLoadedAsync();

			var listener = CapturingTraceListener.Capture(() => view.Behaviors.Add(behavior));

			Assert.Null(behavior.Value);
			Assert.Contains(listener.Messages, static message => message.Contains(nameof(ClrPropertyView.Amount), StringComparison.Ordinal));
		});
	}

	[Fact]
	public async Task EmptyValuePropertyNameDoesNotThrowAndLeavesValueUnbound()
	{
		SetupBuilder();
		var behavior = new CapturingValidationBehavior { ValuePropertyName = string.Empty };
		var entry = new Entry { WidthRequest = 200, Text = "text" };

		await AttachAndRun(entry, async _ =>
		{
			await entry.OnLoadedAsync();

			entry.Behaviors.Add(behavior);
			entry.Text = "changed";

			Assert.Null(behavior.Value);
		});
	}

	[Fact]
	public async Task SettingAnInvalidValuePropertyNameWhileAttachedThrowsAnArgumentException()
	{
		SetupBuilder();
		var behavior = new CapturingValidationBehavior { ValuePropertyName = nameof(CheckBox.IsChecked) };
		var checkBox = new CheckBox();
		checkBox.Behaviors.Add(behavior);

		await AttachAndRun(checkBox, async _ =>
		{
			await checkBox.OnLoadedAsync();

			var exception = Assert.Throws<ArgumentException>(() => behavior.ValuePropertyName = "Amount");

			Assert.Equal(nameof(ValidationBehavior.ValuePropertyName), exception.ParamName);
		});
	}

	[Fact]
	public async Task SubclassDefaultValuePropertyNameIsFollowed()
	{
		SetupBuilder();
		var behavior = new IsCheckedValidationBehavior();
		var checkBox = new CheckBox();
		checkBox.Behaviors.Add(behavior);

		await AttachAndRun(checkBox, async _ =>
		{
			await checkBox.OnLoadedAsync();

			checkBox.IsChecked = true;
			await AssertValueEventually(behavior, true);
		});
	}

	[Fact]
	public async Task UserBindingOnValueSurvivesDetachingAndReattachingTheBehavior()
	{
		SetupBuilder();
		var view = new ClrPropertyView();
		var behavior = new CapturingValidationBehavior { ValuePropertyName = null };
		behavior.SetBinding(ValidationBehavior.ValueProperty, static (ClrPropertyView source) => source.Amount, source: view);
		view.Behaviors.Add(behavior);

		await AttachAndRun(view, async _ =>
		{
			await view.OnLoadedAsync();

			view.Amount = "before";
			await AssertValueEventually(behavior, "before");

			view.Behaviors.Remove(behavior);
			view.Behaviors.Add(behavior);

			view.Amount = "after";
			await AssertValueEventually(behavior, "after");
		});
	}

	static async Task RunWithCurrentCulture(string cultureName, Func<Task> action)
	{
		var originalCulture = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);

		try
		{
			await action();
		}
		finally
		{
			CultureInfo.CurrentCulture = originalCulture;
		}
	}

	async Task AssertValueEventually(ValidationBehavior behavior, object? expected)
	{
		await Wait(() => Equals(behavior.Value, expected), timeout: 3000);
		Assert.Equal(expected, behavior.Value);
	}

	sealed class CapturingValidationBehavior : ValidationBehavior
	{
		protected override ValueTask<bool> ValidateAsync(object? value, CancellationToken token) => ValueTask.FromResult(value is not null);
	}

	sealed class IsCheckedValidationBehavior : ValidationBehavior
	{
		protected override string DefaultValuePropertyName => nameof(CheckBox.IsChecked);

		protected override ValueTask<bool> ValidateAsync(object? value, CancellationToken token) => ValueTask.FromResult(value is not null);
	}

	sealed class AmountView : ContentView
	{
		public static readonly BindableProperty AmountProperty = BindableProperty.Create(nameof(Amount), typeof(int), typeof(AmountView), 0);

		public int Amount
		{
			get => (int)GetValue(AmountProperty);
			set => SetValue(AmountProperty, value);
		}
	}

	internal sealed class ClrPropertyView : ContentView
	{
		string? amount;

		public string? Amount
		{
			get => amount;
			set
			{
				amount = value;
				OnPropertyChanged();
			}
		}
	}
}