using System.ComponentModel;
using CommunityToolkit.Maui.Behaviors;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

// The protected overrides are only called by .NET MAUI with the view they are attached to, so the tests call them through a subclass
#pragma warning disable CS8604, CS8625 // Possible null reference argument; cannot convert null literal to non-nullable reference type.
public class BehaviorsNullArgumentTests : BaseTest
{
	[Fact]
	public void BaseBehavior_NullBindable_ShouldThrowArgumentNullException()
	{
		var behavior = new ExposedBaseBehavior();

		NullArgumentAssert.Throws("bindable", () => behavior.InvokeOnAttachedTo(null));
		NullArgumentAssert.Throws("bindable", () => behavior.InvokeOnDetachingFrom(null));
	}

	[Fact]
	public void AnimationBehavior_NullBindable_ShouldThrowArgumentNullException()
	{
		var behavior = new ExposedAnimationBehavior();

		NullArgumentAssert.Throws("bindable", () => behavior.InvokeOnAttachedTo(null));
		NullArgumentAssert.Throws("bindable", () => behavior.InvokeOnDetachingFrom(null));
	}

	[Fact]
	public void EventToCommandBehavior_NullBindable_ShouldThrowArgumentNullException()
	{
		var behavior = new ExposedEventToCommandBehavior();

		NullArgumentAssert.Throws("bindable", () => behavior.InvokeOnAttachedTo(null));
		NullArgumentAssert.Throws("bindable", () => behavior.InvokeOnDetachingFrom(null));
	}

	[Fact]
	public void ImpliedOrderGridBehavior_NullBindable_ShouldThrowArgumentNullException()
	{
		var behavior = new ExposedImpliedOrderGridBehavior();

		NullArgumentAssert.Throws("bindable", () => behavior.InvokeOnAttachedTo(null));
		NullArgumentAssert.Throws("bindable", () => behavior.InvokeOnDetachingFrom(null));
	}

	[Fact]
	public void EmailValidationBehavior_NullBindable_ShouldThrowArgumentNullException()
	{
		var behavior = new ExposedEmailValidationBehavior();

		NullArgumentAssert.Throws("bindable", () => behavior.InvokeOnAttachedTo(null));
		NullArgumentAssert.Throws("bindable", () => behavior.InvokeOnDetachingFrom(null));
	}

	[Fact]
	public void ValidationBehavior_NullArguments_ShouldThrowArgumentNullException()
	{
		var behavior = new ExposedRequiredStringValidationBehavior();
		var entry = new Entry();
		var eventArgs = new PropertyChangedEventArgs(nameof(Entry.IsFocused));

		NullArgumentAssert.Throws("bindable", () => behavior.InvokeOnAttachedTo(null));
		NullArgumentAssert.Throws("bindable", () => behavior.InvokeOnDetachingFrom(null));
		NullArgumentAssert.Throws("sender", () => behavior.InvokeOnViewPropertyChanged(null, eventArgs));
		NullArgumentAssert.Throws("e", () => behavior.InvokeOnViewPropertyChanged(entry, null));
	}

	[Fact]
	public void MaskedBehavior_NullEventArgs_ShouldThrowArgumentNullException()
	{
		var behavior = new ExposedMaskedBehavior();

		NullArgumentAssert.Throws("e", () => behavior.InvokeOnViewPropertyChanged(new Entry(), null));
	}

	[Fact]
	public void MaxLengthReachedBehavior_NullEventArgs_ShouldThrowArgumentNullException()
	{
		var behavior = new ExposedMaxLengthReachedBehavior();

		NullArgumentAssert.Throws("e", () => behavior.InvokeOnViewPropertyChanged(new Entry(), null));
	}

	[Fact]
	public void UserStoppedTypingBehavior_NullArguments_ShouldThrowArgumentNullException()
	{
		var behavior = new ExposedUserStoppedTypingBehavior();
		var eventArgs = new PropertyChangedEventArgs(nameof(Entry.Text));

		NullArgumentAssert.Throws("sender", () => behavior.InvokeOnViewPropertyChanged(null, eventArgs));
		NullArgumentAssert.Throws("e", () => behavior.InvokeOnViewPropertyChanged(new Entry(), null));
	}

	[Fact]
	public void StatusBarBehavior_NullArguments_ShouldThrowArgumentNullException()
	{
		var behavior = new ExposedStatusBarBehavior();

		NullArgumentAssert.Throws("page", () => behavior.InvokeOnAttachedTo(null, new object()));
		NullArgumentAssert.Throws("platformView", () => behavior.InvokeOnAttachedTo(new ContentPage(), null));
		NullArgumentAssert.Throws("page", () => behavior.InvokeOnDetachedFrom(null, new object()));
		NullArgumentAssert.Throws("platformView", () => behavior.InvokeOnDetachedFrom(new ContentPage(), null));
	}

	[Fact]
	public void MaxLengthReachedEventArgs_NullText_ShouldThrowArgumentNullException()
	{
		NullArgumentAssert.Throws("text", () => _ = new MaxLengthReachedEventArgs(null));
	}

	sealed class ExposedBaseBehavior : BaseBehavior<VisualElement>
	{
		public void InvokeOnAttachedTo(VisualElement bindable) => OnAttachedTo(bindable);

		public void InvokeOnDetachingFrom(VisualElement bindable) => OnDetachingFrom(bindable);
	}

	sealed class ExposedAnimationBehavior : AnimationBehavior
	{
		public void InvokeOnAttachedTo(VisualElement bindable) => OnAttachedTo(bindable);

		public void InvokeOnDetachingFrom(VisualElement bindable) => OnDetachingFrom(bindable);
	}

	sealed class ExposedEventToCommandBehavior : EventToCommandBehavior
	{
		public void InvokeOnAttachedTo(VisualElement bindable) => OnAttachedTo(bindable);

		public void InvokeOnDetachingFrom(VisualElement bindable) => OnDetachingFrom(bindable);
	}

	sealed class ExposedImpliedOrderGridBehavior : ImpliedOrderGridBehavior
	{
		public void InvokeOnAttachedTo(Grid bindable) => OnAttachedTo(bindable);

		public void InvokeOnDetachingFrom(Grid bindable) => OnDetachingFrom(bindable);
	}

	sealed class ExposedEmailValidationBehavior : EmailValidationBehavior
	{
		public void InvokeOnAttachedTo(VisualElement bindable) => OnAttachedTo(bindable);

		public void InvokeOnDetachingFrom(VisualElement bindable) => OnDetachingFrom(bindable);
	}

	sealed class ExposedRequiredStringValidationBehavior : RequiredStringValidationBehavior
	{
		public void InvokeOnAttachedTo(VisualElement bindable) => OnAttachedTo(bindable);

		public void InvokeOnDetachingFrom(VisualElement bindable) => OnDetachingFrom(bindable);

		public void InvokeOnViewPropertyChanged(VisualElement sender, PropertyChangedEventArgs e) => OnViewPropertyChanged(sender, e);
	}

	sealed class ExposedMaskedBehavior : MaskedBehavior
	{
		public void InvokeOnViewPropertyChanged(InputView sender, PropertyChangedEventArgs e) => OnViewPropertyChanged(sender, e);
	}

	sealed class ExposedMaxLengthReachedBehavior : MaxLengthReachedBehavior
	{
		public void InvokeOnViewPropertyChanged(InputView sender, PropertyChangedEventArgs e) => OnViewPropertyChanged(sender, e);
	}

	sealed class ExposedUserStoppedTypingBehavior : UserStoppedTypingBehavior
	{
		public void InvokeOnViewPropertyChanged(InputView sender, PropertyChangedEventArgs e) => OnViewPropertyChanged(sender, e);
	}

	sealed class ExposedStatusBarBehavior : StatusBarBehavior
	{
		public void InvokeOnAttachedTo(Page page, object platformView) => OnAttachedTo(page, platformView);

		public void InvokeOnDetachedFrom(Page page, object platformView) => OnDetachedFrom(page, platformView);
	}
}
#pragma warning restore CS8604, CS8625
