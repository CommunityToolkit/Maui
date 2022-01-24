using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.Pages.Behaviors;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public class BehaviorsGalleryViewModel : BaseGalleryViewModel
{
	public BehaviorsGalleryViewModel()
		: base(new[]
		{
			SectionModel.Create<EventToCommandBehaviorPage>(nameof(EventToCommandBehavior),
				"Turns any event into a command that can be bound to"),

			SectionModel.Create<MaskedBehaviorPage>(nameof(MaskedBehavior),
				"Masked text in entry with specific pattern"),

			SectionModel.Create<UserStoppedTypingBehaviorPage>(nameof(UserStoppedTypingBehavior),
				"This behavior waits for the user to stop typing and then executes a Command"),

			SectionModel.Create<MaxLengthReachedBehaviorPage>(nameof(MaxLengthReachedBehavior),
				"This behavior invokes an EventHandler and executes a Command when the MaxLength of an InputView has been reached"),

			SectionModel.Create<ProgressBarAnimationBehaviorPage>(nameof(ProgressBarAnimationBehavior),
				"Animate the progress for the ProgressBar"),

			SectionModel.Create<SetFocusOnEntryCompletedBehaviorPage>(nameof(SetFocusOnEntryCompletedBehavior),
				"Set focus to another element when an entry is completed"),

			SectionModel.Create<CharactersValidationBehaviorPage>(nameof(CharactersValidationBehavior),
				"Changes an Entry's text color when an invalid string is provided."),

			SectionModel.Create<TextValidationBehaviorPage>(nameof(TextValidationBehavior),
				"Changes an Entry's text color when text validation is failed (based on regex)"),

			SectionModel.Create<MultiValidationBehaviorPage>(nameof(MultiValidationBehavior),
				"Combines multiple validation behavior"),

			SectionModel.Create<UriValidationBehaviorPage>(nameof(UriValidationBehavior),
				"Changes an Entry's text color when an invalid URI is provided"),

			SectionModel.Create<RequiredStringValidationBehaviorPage>(nameof(RequiredStringValidationBehavior),
				"Changes an Entry's text color when a required string is not provided"),

			SectionModel.Create<NumericValidationBehaviorPage>(nameof(NumericValidationBehavior),
				"Changes an Entry's text color when an invalid number is provided"),

			SectionModel.Create<EmailValidationBehaviorPage>(nameof(EmailValidationBehavior),
				"Changes an Entry's text color when an invalid e-mail address is provided"),
			}
		)
	{

	}
}