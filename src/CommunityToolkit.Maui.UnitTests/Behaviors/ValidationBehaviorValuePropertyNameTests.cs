using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using CommunityToolkit.Maui.Behaviors;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class ValidationBehaviorValuePropertyNameTests : BaseTest
{
	[Fact]
	public void DefaultValuePropertyName_FollowsEntryText()
	{
		var entry = new Entry { Text = "first" };
		var behavior = new ValueCapturingBehavior();

		entry.Behaviors.Add(behavior);
		Assert.Equal("first", behavior.Value);

		entry.Text = "second";
		Assert.Equal("second", behavior.Value);
	}

	[Fact]
	public void DefaultValuePropertyName_FollowsEditorText()
	{
		var editor = new Editor { Text = "first" };
		var behavior = new ValueCapturingBehavior();

		editor.Behaviors.Add(behavior);
		editor.Text = "second";

		Assert.Equal("second", behavior.Value);
	}

	[Fact]
	public void DefaultValuePropertyName_FollowsSearchBarText()
	{
		var searchBar = new SearchBar { Text = "first" };
		var behavior = new ValueCapturingBehavior();

		searchBar.Behaviors.Add(behavior);
		searchBar.Text = "second";

		Assert.Equal("second", behavior.Value);
	}

	[Fact]
	public void ValuePropertyName_FollowsBindablePropertyOfCustomView()
	{
		var view = new CodeView { Code = "A1" };
		var behavior = new ValueCapturingBehavior { ValuePropertyName = nameof(CodeView.Code) };

		view.Behaviors.Add(behavior);
		Assert.Equal("A1", behavior.Value);

		view.Code = "B2";
		Assert.Equal("B2", behavior.Value);
	}

	[Fact]
	public void ValuePropertyName_FollowsBindablePropertyDeclaredOnBaseClassOfCustomView()
	{
		var view = new DerivedCodeView { Code = "A1" };
		var behavior = new ValueCapturingBehavior { ValuePropertyName = nameof(CodeView.Code) };

		view.Behaviors.Add(behavior);
		view.Code = "B2";

		Assert.Equal("B2", behavior.Value);
	}

	[Fact]
	public void ValuePropertyName_Switched_FollowsNewPropertyOnly()
	{
		var entry = new Entry { Text = "text", Placeholder = "placeholder" };
		var behavior = new ValueCapturingBehavior();
		entry.Behaviors.Add(behavior);

		behavior.ValuePropertyName = nameof(Entry.Placeholder);
		Assert.Equal("placeholder", behavior.Value);

		entry.Text = "changed text";
		Assert.Equal("placeholder", behavior.Value);

		entry.Placeholder = "changed placeholder";
		Assert.Equal("changed placeholder", behavior.Value);

		behavior.ValuePropertyName = nameof(Entry.Text);
		Assert.Equal("changed text", behavior.Value);
	}

	[Theory]
	[InlineData("")]
	[InlineData(null)]
	public void ValuePropertyName_NullOrEmpty_DoesNotThrowAndLeavesValueUnset(string? valuePropertyName)
	{
		var entry = new Entry { Text = "text" };
		var behavior = new ObjectValueCapturingBehavior { ValuePropertyName = valuePropertyName };

		entry.Behaviors.Add(behavior);
		entry.Text = "changed";

		Assert.Null(behavior.Value);
	}

	[Theory]
	[InlineData(nameof(PlainPropertyView.Plain))]
	[InlineData("Text.Length")]
	[InlineData("DoesNotExist")]
	[InlineData("NotBindable")]
	public void Attach_WithNameThatIsNoBindableProperty_ThrowsArgumentExceptionSynchronously(string valuePropertyName)
	{
		var view = new PlainPropertyView { Text = "text", Plain = "plain" };
		var behavior = new ValueCapturingBehavior { ValuePropertyName = valuePropertyName };

		var exception = Assert.Throws<ArgumentException>(() => view.Behaviors.Add(behavior));

		Assert.Equal(nameof(ValidationBehavior.ValuePropertyName), exception.ParamName);
		Assert.Contains(valuePropertyName, exception.Message, StringComparison.Ordinal);
		Assert.Contains(nameof(PlainPropertyView), exception.Message, StringComparison.Ordinal);
		Assert.Contains("{x:Null}", exception.Message, StringComparison.Ordinal);
		Assert.Null(behavior.Value);
	}

	[Fact]
	public void ValuePropertyName_SetToBindablePropertyAfterAttachingWithNullName_Binds()
	{
		var view = new CodeView { Code = "A1" };
		var behavior = new ValueCapturingBehavior { ValuePropertyName = null };
		view.Behaviors.Add(behavior);

		behavior.ValuePropertyName = nameof(CodeView.Code);
		view.Code = "B2";

		Assert.Equal("B2", behavior.Value);
	}

	[Theory]
	[InlineData(nameof(PlainPropertyView.Plain))]
	[InlineData("DoesNotExist")]
	[InlineData("Text.Length")]
	public void ValuePropertyName_ChangedToBadNameWhileAttached_ThrowsAndStopsFollowingOldProperty(string valuePropertyName)
	{
		var view = new PlainPropertyView { Text = "text", Plain = "plain" };
		var behavior = new ValueCapturingBehavior();
		view.Behaviors.Add(behavior);
		Assert.Equal("text", behavior.Value);

		var exception = Assert.Throws<ArgumentException>(() => behavior.ValuePropertyName = valuePropertyName);
		view.Text = "changed";

		Assert.Equal(nameof(ValidationBehavior.ValuePropertyName), exception.ParamName);
		Assert.Equal("text", behavior.Value);
	}

	[Fact]
	public void ValuePropertyName_ChangedToBadNameWhileDetached_ThrowsOnNextAttach()
	{
		var view = new PlainPropertyView { Plain = "plain" };
		var behavior = new ValueCapturingBehavior();

		behavior.ValuePropertyName = nameof(PlainPropertyView.Plain);

		Assert.Throws<ArgumentException>(() => view.Behaviors.Add(behavior));
	}

	[Fact]
	public void Attach_WithBadNameAndSuppressedExceptions_TracesAndLeavesValueUnbound()
	{
		var originalSetting = Options.ShouldSuppressExceptionsInBehaviors;
		var listener = new CapturingTraceListener();
		Trace.Listeners.Add(listener);

		try
		{
			new Options().SetShouldSuppressExceptionsInBehaviors(true);
			var view = new PlainPropertyView { Text = "text", Plain = "plain" };
			var behavior = new ValueCapturingBehavior { ValuePropertyName = nameof(PlainPropertyView.Plain) };

			view.Behaviors.Add(behavior);
			view.Plain = "changed";

			Assert.Null(behavior.Value);
			Assert.Contains(listener.Messages, message => message.Contains(nameof(PlainPropertyView.Plain), StringComparison.Ordinal) && message.Contains(nameof(PlainPropertyView), StringComparison.Ordinal));
		}
		finally
		{
			Trace.Listeners.Remove(listener);
			new Options().SetShouldSuppressExceptionsInBehaviors(originalSetting);
		}
	}

	[Fact]
	public void ValuePropertyName_ChangedToBadNameWhileAttachedAndSuppressedExceptions_TracesAndUnbindsValue()
	{
		var originalSetting = Options.ShouldSuppressExceptionsInBehaviors;
		var listener = new CapturingTraceListener();
		Trace.Listeners.Add(listener);

		try
		{
			new Options().SetShouldSuppressExceptionsInBehaviors(true);
			var view = new PlainPropertyView { Text = "text", Plain = "plain" };
			var behavior = new ValueCapturingBehavior();
			view.Behaviors.Add(behavior);

			behavior.ValuePropertyName = "DoesNotExist";
			view.Text = "changed";

			Assert.Equal("text", behavior.Value);
			Assert.Contains(listener.Messages, message => message.Contains("DoesNotExist", StringComparison.Ordinal));
		}
		finally
		{
			Trace.Listeners.Remove(listener);
			new Options().SetShouldSuppressExceptionsInBehaviors(originalSetting);
		}
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	public void NullOrEmptyValuePropertyName_KeepsCompiledBindingOnValueAcrossAttachDetachAndReattach(string? valuePropertyName)
	{
		var entry = new Entry { Text = "entry text" };
		var source = new PlainPropertyView { Plain = "first" };
		var behavior = new ValueCapturingBehavior { ValuePropertyName = valuePropertyName };
		behavior.SetBinding(ValidationBehavior.ValueProperty, Binding.Create(static (PlainPropertyView view) => view.Plain, BindingMode.OneWay, source: source));

		entry.Behaviors.Add(behavior);
		Assert.Equal("first", behavior.Value);
		source.Plain = "second";
		Assert.Equal("second", behavior.Value);

		entry.Behaviors.Remove(behavior);
		source.Plain = "third";
		Assert.Equal("third", behavior.Value);

		entry.Behaviors.Add(behavior);
		source.Plain = "fourth";
		Assert.Equal("fourth", behavior.Value);
		entry.Text = "ignored";
		Assert.Equal("fourth", behavior.Value);
	}

	[Fact]
	public void ValuePropertyName_SwitchedFromTextToNullToText_KeepsUserBindingOnlyWhileNull()
	{
		var entry = new Entry { Text = "entry text" };
		var source = new PlainPropertyView { Plain = "plain" };
		var behavior = new ValueCapturingBehavior();
		entry.Behaviors.Add(behavior);
		Assert.Equal("entry text", behavior.Value);

		behavior.ValuePropertyName = null;
		entry.Text = "ignored";
		Assert.Equal("entry text", behavior.Value);

		behavior.SetBinding(ValidationBehavior.ValueProperty, Binding.Create(static (PlainPropertyView view) => view.Plain, BindingMode.OneWay, source: source));
		Assert.Equal("plain", behavior.Value);
		source.Plain = "changed plain";
		Assert.Equal("changed plain", behavior.Value);

		behavior.ValuePropertyName = nameof(Entry.Text);
		Assert.Equal("ignored", behavior.Value);
		entry.Text = "followed again";
		Assert.Equal("followed again", behavior.Value);
	}

	[Fact]
	public void ValuePropertyName_SwitchedFromBindablePropertyToNull_RemovesOnlyTheBindingOfTheBehavior()
	{
		var entry = new Entry { Text = "text" };
		var behavior = new ValueCapturingBehavior();
		entry.Behaviors.Add(behavior);

		behavior.ValuePropertyName = null;
		entry.Text = "changed";

		Assert.Equal("text", behavior.Value);
	}

	[Fact]
	public void DatePicker_Date_Binds()
	{
		var datePicker = new DatePicker { Date = new DateTime(2026, 1, 2) };
		var behavior = new ObjectValueCapturingBehavior { ValuePropertyName = nameof(DatePicker.Date) };

		datePicker.Behaviors.Add(behavior);
		Assert.Equal<object?>(new DateTime(2026, 1, 2), behavior.Value);

		datePicker.Date = new DateTime(2026, 3, 4);
		Assert.Equal<object?>(new DateTime(2026, 3, 4), behavior.Value);
	}

	[Fact]
	public void MultiValidationBehavior_ChildrenWithBindablePropertyNamesAttachWithoutThrowing()
	{
		var entry = new Entry { Text = "ab" };
		var child = new TextValidationBehavior { MinimumLength = 1 };
		var multiBehavior = new MultiValidationBehavior { Children = { child } };

		entry.Behaviors.Add(multiBehavior);

		Assert.Equal("ab", multiBehavior.Value);
	}

	[Fact]
	public async Task Detached_StopsFollowing_AndReattachedFollowsAgain()
	{
		var entry = new Entry { Text = "first" };
		var behavior = new ValueCapturingBehavior();

		entry.Behaviors.Add(behavior);
		entry.Behaviors.Remove(behavior);
		entry.Text = "while detached";
		Assert.Equal("first", behavior.Value);

		entry.Behaviors.Add(behavior);
		await WaitUntil(() => Equals(behavior.Value, "while detached"));
		Assert.Equal("while detached", behavior.Value);

		entry.Text = "after reattach";
		Assert.Equal("after reattach", behavior.Value);
	}

	[Fact]
	public async Task Reattached_ToDifferentView_FollowsNewViewOnly()
	{
		var firstEntry = new Entry { Text = "first" };
		var secondEntry = new Entry { Text = "second" };
		var behavior = new ValueCapturingBehavior();

		firstEntry.Behaviors.Add(behavior);
		firstEntry.Behaviors.Remove(behavior);
		secondEntry.Behaviors.Add(behavior);
		await WaitUntil(() => Equals(behavior.Value, "second"));

		firstEntry.Text = "ignored";
		Assert.Equal("second", behavior.Value);

		secondEntry.Text = "changed";

		Assert.Equal("changed", behavior.Value);
	}

	[Fact]
	public void OverriddenDefaultValuePropertyName_IsUsed()
	{
		var view = new CodeView { Code = "A1" };
		var behavior = new CodeValidationBehavior();

		view.Behaviors.Add(behavior);
		view.Code = "B2";

		Assert.Equal(nameof(CodeView.Code), behavior.ValuePropertyName);
		Assert.Equal("B2", behavior.Value);
	}

	[Fact]
	public async Task MultiValidationBehavior_FollowsTextAndValidatesChildren()
	{
		var entry = new Entry { Text = "ab" };
		var child = new ValueCapturingBehavior();
		var lengthChild = new TextValidationBehavior { MinimumLength = 3 };
		var multiBehavior = new MultiValidationBehavior { Children = { child, lengthChild } };

		entry.Behaviors.Add(multiBehavior);
		entry.Text = "abc";
		await multiBehavior.ForceValidate(TestContext.Current.CancellationToken);

		Assert.Equal("abc", multiBehavior.Value);
		Assert.Equal("abc", child.Value);
		Assert.True(lengthChild.IsValid);
		Assert.True(multiBehavior.IsValid);
	}

	[Theory]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void ValuePropertyName_WithI_ResolvesUnderTurkishCulture(string cultureName)
	{
		var originalCulture = CultureInfo.CurrentCulture;
		var originalUICulture = CultureInfo.CurrentUICulture;

		try
		{
			CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo(cultureName);

			var view = new CodeView { Id2 = "I1" };
			var behavior = new ValueCapturingBehavior { ValuePropertyName = nameof(CodeView.Id2) };

			view.Behaviors.Add(behavior);
			view.Id2 = "ı2";

			Assert.Equal("ı2", behavior.Value);
		}
		finally
		{
			CultureInfo.CurrentCulture = originalCulture;
			CultureInfo.CurrentUICulture = originalUICulture;
		}
	}

	[Theory]
	[InlineData(typeof(ValidationBehavior))]
	[InlineData(typeof(ValidationBehavior<string>))]
	[InlineData(typeof(TextValidationBehavior))]
	[InlineData(typeof(CharactersValidationBehavior))]
	[InlineData(typeof(EmailValidationBehavior))]
	[InlineData(typeof(NumericValidationBehavior))]
	[InlineData(typeof(RequiredStringValidationBehavior))]
	[InlineData(typeof(UriValidationBehavior))]
	[InlineData(typeof(MultiValidationBehavior))]
	public void ValidationBehaviors_DoNotRequireUnreferencedCode(Type behaviorType)
	{
		Assert.Null(behaviorType.GetCustomAttribute<RequiresUnreferencedCodeAttribute>(inherit: false));
	}

	[Fact]
	public void DefaultValuePropertyName_FollowsLabelText()
	{
		var label = new Label { Text = "first" };
		var behavior = new ValueCapturingBehavior();

		label.Behaviors.Add(behavior);
		label.Text = "second";

		Assert.Equal("second", behavior.Value);
	}

	[Fact]
	public void DefaultValuePropertyName_FollowsButtonText()
	{
		var button = new Button { Text = "first" };
		var behavior = new ValueCapturingBehavior();

		button.Behaviors.Add(behavior);
		button.Text = "second";

		Assert.Equal("second", behavior.Value);
	}

	[Fact]
	public void ValuePropertyName_FollowsBindablePropertyOfMauiViewThatIsNotText()
	{
		var checkBox = new CheckBox { IsChecked = false };
		var behavior = new ObjectValueCapturingBehavior { ValuePropertyName = nameof(CheckBox.IsChecked) };

		checkBox.Behaviors.Add(behavior);
		Assert.Equal<object?>(false, behavior.Value);

		checkBox.IsChecked = true;
		Assert.Equal<object?>(true, behavior.Value);
	}

	[Theory]
	[InlineData("tr-TR", "IsChecked", true)]
	[InlineData("az-Latn-AZ", "IsChecked", true)]
	[InlineData("tr-TR", "isChecked", false)]
	[InlineData("tr-TR", "ISCHECKED", false)]
	[InlineData("tr-TR", "\u0130sChecked", false)]
	[InlineData("tr-TR", "\u0131sChecked", false)]
	[InlineData("az-Latn-AZ", "isChecked", false)]
	[InlineData("az-Latn-AZ", "\u0130sChecked", false)]
	[InlineData("az-Latn-AZ", "\u0131sChecked", false)]
	public void ValuePropertyName_ResolvesOnlyTheExactBindablePropertyNameWhateverTheCulture(string cultureName, string valuePropertyName, bool isBound)
	{
		var originalCulture = CultureInfo.CurrentCulture;
		var originalUICulture = CultureInfo.CurrentUICulture;

		try
		{
			CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo(cultureName);
			var checkBox = new CheckBox { IsChecked = true };
			var behavior = new ObjectValueCapturingBehavior { ValuePropertyName = valuePropertyName };

			if (isBound)
			{
				checkBox.Behaviors.Add(behavior);
				Assert.Equal<object?>(true, behavior.Value);
			}
			else
			{
				Assert.Throws<ArgumentException>(() => checkBox.Behaviors.Add(behavior));
				Assert.Null(behavior.Value);
			}
		}
		finally
		{
			CultureInfo.CurrentCulture = originalCulture;
			CultureInfo.CurrentUICulture = originalUICulture;
		}
	}

	[Theory]
	[InlineData("tr-TR", "text")]
	[InlineData("az-Latn-AZ", "text")]
	[InlineData("tr-TR", "TEXT")]
	public void ValuePropertyName_WithLowercaseTextUnderTurkishCulture_IsNotABindablePropertyName(string cultureName, string valuePropertyName)
	{
		var originalCulture = CultureInfo.CurrentCulture;
		var originalUICulture = CultureInfo.CurrentUICulture;

		try
		{
			CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo(cultureName);
			var entry = new Entry { Text = "text" };
			var behavior = new ValueCapturingBehavior { ValuePropertyName = valuePropertyName };

			var exception = Assert.Throws<ArgumentException>(() => entry.Behaviors.Add(behavior));

			Assert.Equal(nameof(ValidationBehavior.ValuePropertyName), exception.ParamName);
		}
		finally
		{
			CultureInfo.CurrentCulture = originalCulture;
			CultureInfo.CurrentUICulture = originalUICulture;
		}
	}

	[Fact]
	public async Task NumericValidationBehavior_WithCommaDecimalUnderTurkish_ValidatesTheValueFollowedThroughTheTypedBinding()
	{
		var originalCulture = CultureInfo.CurrentCulture;
		var originalUICulture = CultureInfo.CurrentUICulture;

		try
		{
			CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
			var entry = new Entry { Text = "3,5" };
			var behavior = new NumericValidationBehavior { MinimumValue = 3, MaximumValue = 4, MaximumDecimalPlaces = 1 };

			entry.Behaviors.Add(behavior);
			await behavior.ForceValidate(TestContext.Current.CancellationToken);

			Assert.Equal("3,5", behavior.Value);
			Assert.True(behavior.IsValid);

			entry.Text = "35,5";
			await behavior.ForceValidate(TestContext.Current.CancellationToken);

			Assert.False(behavior.IsValid);
		}
		finally
		{
			CultureInfo.CurrentCulture = originalCulture;
			CultureInfo.CurrentUICulture = originalUICulture;
		}
	}

	[Fact]
	public void Attach_OfDefaultTextNameToViewWithoutTextProperty_TracesWarningAndLeavesValueUnbound()
	{
		var listener = new CapturingTraceListener();
		Trace.Listeners.Add(listener);

		try
		{
			var view = new CodeView { Code = "A1" };
			var behavior = new ValueCapturingBehavior();

			view.Behaviors.Add(behavior);
			view.Code = "B2";

			Assert.Null(behavior.Value);
			Assert.Contains(listener.Messages, message => message.Contains(nameof(ValueCapturingBehavior), StringComparison.Ordinal)
				&& message.Contains(nameof(Entry.Text), StringComparison.Ordinal)
				&& message.Contains(nameof(CodeView), StringComparison.Ordinal)
				&& message.Contains("{x:Null}", StringComparison.Ordinal));
		}
		finally
		{
			Trace.Listeners.Remove(listener);
		}
	}

	[Fact]
	public void Attach_OfPickerWithTextValidationBehaviorAndNoValuePropertyName_DoesNotThrowAndLeavesValueUnbound()
	{
		var picker = new Picker { ItemsSource = new[] { "a", "b" }, SelectedIndex = 1 };
		var behavior = new TextValidationBehavior { MinimumLength = 1 };

		picker.Behaviors.Add(behavior);

		Assert.Null(behavior.Value);
	}

	[Fact]
	public void Attach_OfPickerWithValuePropertyNameBoundToViewModelThatArrivesLater_BindsOnceTheNameArrives()
	{
		var picker = new Picker { ItemsSource = new[] { "a", "b" }, SelectedIndex = 0 };
		var behavior = new ObjectValueCapturingBehavior();
		behavior.SetBinding(ValidationBehavior.ValuePropertyNameProperty, new Binding(nameof(NameViewModel.PropName)));

		picker.Behaviors.Add(behavior);
		Assert.Null(behavior.Value);

		behavior.BindingContext = new NameViewModel { PropName = nameof(Picker.SelectedIndex) };
		Assert.Equal<object?>(0, behavior.Value);

		picker.SelectedIndex = 1;
		Assert.Equal<object?>(1, behavior.Value);
	}

	[Fact]
	public void ValuePropertyName_BoundNameThatArrivesInvalid_ThrowsAtThatChange()
	{
		var picker = new Picker();
		var behavior = new ObjectValueCapturingBehavior();
		behavior.SetBinding(ValidationBehavior.ValuePropertyNameProperty, new Binding(nameof(NameViewModel.PropName)));
		picker.Behaviors.Add(behavior);

		var exception = Assert.Throws<ArgumentException>(() => behavior.BindingContext = new NameViewModel { PropName = "DoesNotExist" });

		Assert.Equal(nameof(ValidationBehavior.ValuePropertyName), exception.ParamName);
	}

	[Fact]
	public void ValuePropertyName_BoundNameThatArrivesInvalidAndSuppressedExceptions_TracesAtThatChange()
	{
		var originalSetting = Options.ShouldSuppressExceptionsInBehaviors;
		var listener = new CapturingTraceListener();
		Trace.Listeners.Add(listener);

		try
		{
			new Options().SetShouldSuppressExceptionsInBehaviors(true);
			var picker = new Picker();
			var behavior = new ObjectValueCapturingBehavior();
			behavior.SetBinding(ValidationBehavior.ValuePropertyNameProperty, new Binding(nameof(NameViewModel.PropName)));
			picker.Behaviors.Add(behavior);

			behavior.BindingContext = new NameViewModel { PropName = "DoesNotExist" };

			Assert.Null(behavior.Value);
			Assert.Contains(listener.Messages, message => message.Contains("DoesNotExist", StringComparison.Ordinal));
		}
		finally
		{
			Trace.Listeners.Remove(listener);
			new Options().SetShouldSuppressExceptionsInBehaviors(originalSetting);
		}
	}

	[Fact]
	public void Attach_OfPickerWithExplicitNameThatIsNotTheDefault_StillThrows()
	{
		var picker = new Picker();
		var behavior = new ObjectValueCapturingBehavior { ValuePropertyName = nameof(DatePicker.Date) };

		var exception = Assert.Throws<ArgumentException>(() => picker.Behaviors.Add(behavior));

		Assert.Equal(nameof(ValidationBehavior.ValuePropertyName), exception.ParamName);
	}

	[Fact]
	public void Attach_WithNameChangedAwayFromTheDefaultAndBackToIt_TracesWarningAndLeavesValueUnbound()
	{
		var listener = new CapturingTraceListener();
		Trace.Listeners.Add(listener);

		try
		{
			var picker = new Picker();
			var behavior = new ObjectValueCapturingBehavior { ValuePropertyName = nameof(Picker.Title) };
			behavior.ValuePropertyName = nameof(Entry.Text);

			picker.Behaviors.Add(behavior);

			Assert.Null(behavior.Value);
			Assert.Contains(listener.Messages, message => message.Contains("has its default", StringComparison.Ordinal));
		}
		finally
		{
			Trace.Listeners.Remove(listener);
		}
	}

	[Fact]
	public void BindingContext_SetToNullOnAttachedPicker_FallsBackToTheDefaultNameWithATraceInsteadOfThrowing()
	{
		var listener = new CapturingTraceListener();
		Trace.Listeners.Add(listener);

		try
		{
			var picker = new Picker { ItemsSource = new[] { "a", "b" }, SelectedIndex = 0 };
			var behavior = new ObjectValueCapturingBehavior();
			behavior.SetBinding(ValidationBehavior.ValuePropertyNameProperty, new Binding(nameof(NameViewModel.PropName)));
			picker.Behaviors.Add(behavior);
			behavior.BindingContext = new NameViewModel { PropName = nameof(Picker.SelectedIndex) };
			Assert.Equal<object?>(0, behavior.Value);

			behavior.BindingContext = null;
			picker.SelectedIndex = 1;

			Assert.Equal(nameof(Entry.Text), behavior.ValuePropertyName);
			Assert.NotEqual<object?>(1, behavior.Value);
			Assert.Contains(listener.Messages, message => message.Contains("has its default", StringComparison.Ordinal));
		}
		finally
		{
			Trace.Listeners.Remove(listener);
		}
	}

	[Fact]
	public void ClearValue_OfValuePropertyNameOnAttachedPicker_FallsBackToTheDefaultNameWithATraceInsteadOfThrowing()
	{
		var listener = new CapturingTraceListener();
		Trace.Listeners.Add(listener);

		try
		{
			var picker = new Picker { ItemsSource = new[] { "a", "b" }, SelectedIndex = 0 };
			var behavior = new ObjectValueCapturingBehavior { ValuePropertyName = nameof(Picker.SelectedIndex) };
			picker.Behaviors.Add(behavior);

			behavior.ClearValue(ValidationBehavior.ValuePropertyNameProperty);
			picker.SelectedIndex = 1;

			Assert.Equal(nameof(Entry.Text), behavior.ValuePropertyName);
			Assert.NotEqual<object?>(1, behavior.Value);
			Assert.Contains(listener.Messages, message => message.Contains("has its default", StringComparison.Ordinal));
		}
		finally
		{
			Trace.Listeners.Remove(listener);
		}
	}

	[Theory]
	[InlineData("tr-TR", "isChecked")]
	[InlineData("tr-TR", "ISCHECKED")]
	[InlineData("tr-TR", "İsChecked")]
	[InlineData("tr-TR", "ısChecked")]
	[InlineData("az-Latn-AZ", "isChecked")]
	[InlineData("az-Latn-AZ", "ISCHECKED")]
	[InlineData("az-Latn-AZ", "İsChecked")]
	[InlineData("az-Latn-AZ", "ısChecked")]
	public void Attach_OfEntryWithNameDifferingFromTheDefaultOnlyByCasing_ThrowsUnderTurkishAndAzeriCultures(string cultureName, string name)
	{
		var originalCulture = CultureInfo.CurrentCulture;
		var originalUICulture = CultureInfo.CurrentUICulture;

		try
		{
			CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo(cultureName);
			var behavior = new IsCheckedDefaultBehavior { ValuePropertyName = name };

			Assert.Throws<ArgumentException>(() => new Entry().Behaviors.Add(behavior));
		}
		finally
		{
			CultureInfo.CurrentCulture = originalCulture;
			CultureInfo.CurrentUICulture = originalUICulture;
		}
	}

	[Theory]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void Attach_OfEntryWithTheDefaultNameIsChecked_TracesInsteadOfThrowingUnderTurkishAndAzeriCultures(string cultureName)
	{
		var originalCulture = CultureInfo.CurrentCulture;
		var originalUICulture = CultureInfo.CurrentUICulture;
		var listener = new CapturingTraceListener();
		Trace.Listeners.Add(listener);

		try
		{
			CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo(cultureName);
			var behavior = new IsCheckedDefaultBehavior { ValuePropertyName = nameof(CheckBox.IsChecked) };

			new Entry().Behaviors.Add(behavior);

			Assert.Contains(listener.Messages, message => message.Contains("has its default", StringComparison.Ordinal));
		}
		finally
		{
			Trace.Listeners.Remove(listener);
			CultureInfo.CurrentCulture = originalCulture;
			CultureInfo.CurrentUICulture = originalUICulture;
		}
	}

	[Fact]
	public void Attach_OfSubclassWhoseDefaultValuePropertyNameIsClrProperty_TracesWarningAndLeavesValueUnbound()
	{
		var view = new PlainPropertyView { Plain = "plain" };
		var behavior = new PlainValidationBehavior();

		view.Behaviors.Add(behavior);

		Assert.Null(behavior.Value);
	}

	sealed class NameViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler? PropertyChanged;

		public string? PropName
		{
			get;
			set
			{
				field = value;
				PropertyChanged?.Invoke(this, new(nameof(PropName)));
			}
		}
	}

	static async Task WaitUntil(Func<bool> condition)
	{
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

		while (!condition())
		{
			await Task.Delay(10, timeout.Token);
		}
	}

	sealed class CapturingTraceListener : TraceListener
	{
		public List<string> Messages { get; } = [];

		public override void Write(string? message) => AddMessage(message);

		public override void WriteLine(string? message) => AddMessage(message);

		void AddMessage(string? message)
		{
			if (message is not null)
			{
				lock (Messages)
				{
					Messages.Add(message);
				}
			}
		}
	}

	sealed class ValueCapturingBehavior : ValidationBehavior<string>
	{
		protected override ValueTask<bool> ValidateAsync(string? value, CancellationToken token) => ValueTask.FromResult(true);
	}

	sealed class ObjectValueCapturingBehavior : ValidationBehavior
	{
		protected override ValueTask<bool> ValidateAsync(object? value, CancellationToken token) => ValueTask.FromResult(true);
	}

	sealed class IsCheckedDefaultBehavior : ValidationBehavior<string>
	{
		protected override string DefaultValuePropertyName => nameof(CheckBox.IsChecked);

		protected override ValueTask<bool> ValidateAsync(string? value, CancellationToken token) => ValueTask.FromResult(true);
	}

	sealed class PlainValidationBehavior : ValidationBehavior<string>
	{
		protected override string DefaultValuePropertyName => nameof(PlainPropertyView.Plain);

		protected override ValueTask<bool> ValidateAsync(string? value, CancellationToken token) => ValueTask.FromResult(true);
	}

	sealed class CodeValidationBehavior : ValidationBehavior<string>
	{
		protected override string DefaultValuePropertyName => nameof(CodeView.Code);

		protected override ValueTask<bool> ValidateAsync(string? value, CancellationToken token) => ValueTask.FromResult(true);
	}

	class CodeView : View
	{
		public static readonly BindableProperty CodeProperty = BindableProperty.Create(nameof(Code), typeof(string), typeof(CodeView));
		public static readonly BindableProperty Id2Property = BindableProperty.Create(nameof(Id2), typeof(string), typeof(CodeView));

		public string? Code
		{
			get => (string?)GetValue(CodeProperty);
			set => SetValue(CodeProperty, value);
		}

		public string? Id2
		{
			get => (string?)GetValue(Id2Property);
			set => SetValue(Id2Property, value);
		}
	}

	sealed class DerivedCodeView : CodeView;

	public sealed class PlainPropertyView : View
	{
		public static readonly string NotBindableProperty = "not a BindableProperty";
		public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(PlainPropertyView));

		string? plain;

		public string? Text
		{
			get => (string?)GetValue(TextProperty);
			set => SetValue(TextProperty, value);
		}

		public string? Plain
		{
			get => plain;
			set
			{
				plain = value;
				OnPropertyChanged();
			}
		}
	}
}