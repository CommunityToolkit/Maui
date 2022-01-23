using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Sample.Models;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public class BehaviorsGalleryViewModel : BaseGalleryViewModel
{
	public BehaviorsGalleryViewModel()
		: base(new[]
		{
			new SectionModel(
				typeof(EventToCommandBehaviorViewModel),
				nameof(EventToCommandBehavior),
				"Turns any event into a command that can be bound to"),

			new SectionModel(
				typeof(MaskedBehaviorViewModel),
				nameof(MaskedBehavior),
				"Masked text in entry with specific pattern"),

			new SectionModel(
				typeof(UserStoppedTypingBehaviorViewModel),
				nameof(UserStoppedTypingBehavior),
				"This behavior waits for the user to stop typing and then executes a Command"),

			new SectionModel(
				typeof(MaxLengthReachedBehaviorViewModel),
				nameof(MaxLengthReachedBehavior),
				"This behavior invokes an EventHandler and executes a Command when the MaxLength of an InputView has been reached"),

			new SectionModel(
				typeof(ProgressBarAnimationBehaviorViewModel),
				nameof(ProgressBarAnimationBehavior),
				"Animate the progress for the ProgressBar"),

			new SectionModel(
				typeof(SetFocusOnEntryCompletedBehaviorViewModel),
				nameof(SetFocusOnEntryCompletedBehavior),
				"Set focus to another element when an entry is completed"),

			new SectionModel(
				typeof(CharactersValidationBehaviorViewModel),
				nameof(CharactersValidationBehavior),
				"Changes an Entry's text color when an invalid string is provided."),

			new SectionModel(
				typeof(TextValidationBehaviorViewModel),
				nameof(TextValidationBehavior),
				"Changes an Entry's text color when text validation is failed (based on regex)"),

			new SectionModel(
				typeof(MultiValidationBehaviorViewModel),
				nameof(MultiValidationBehavior),
				"Combines multiple validation behavior"),

			new SectionModel(
				typeof(UriValidationBehaviorViewModel),
				nameof(UriValidationBehavior),
				"Changes an Entry's text color when an invalid URI is provided"),

			new SectionModel(
				typeof(RequiredStringValidationBehaviorViewModel),
				nameof(RequiredStringValidationBehavior),
				"Changes an Entry's text color when a required string is not provided"),

			new SectionModel(
				typeof(NumericValidationBehaviorViewModel),
				nameof(NumericValidationBehavior),
				"Changes an Entry's text color when an invalid number is provided"),

			new SectionModel(
				typeof(EmailValidationBehaviorViewModel),
				nameof(EmailValidationBehavior),
				"Changes an Entry's text color when an invalid e-mail address is provided"),
			}
		)
	{

	}
}

public class UriValidationBehaviorViewModel:BaseViewModel
{
}

public class RequiredStringValidationBehaviorViewModel : BaseViewModel
{
}

public class NumericValidationBehaviorViewModel : BaseViewModel
{
}

public class MultiValidationBehaviorViewModel : BaseViewModel
{
}

public class TextValidationBehaviorViewModel : BaseViewModel
{
}

public class CharactersValidationBehaviorViewModel : BaseViewModel
{
}

public class SetFocusOnEntryCompletedBehaviorViewModel : BaseViewModel
{
}

public class EmailValidationBehaviorViewModel : BaseViewModel
{

}

public class MaskedBehaviorViewModel : BaseViewModel
{

}