using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.Pages.Behaviors;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public class BehaviorsGalleryViewModel : BaseGalleryViewModel
{
	public BehaviorsGalleryViewModel(MaskedBehaviorPage maskedBehaviorPage,
										UriValidationBehaviorPage uriValidationBehaviorPage,
										EventToCommandBehaviorPage eventToCommandBehaviorPage,
										TextValidationBehaviorPage textValidationBehaviorPage,
										EmailValidationBehaviorPage emailValidationBehaviorPage,
										MultiValidationBehaviorPage multiValidationBehaviorPage,
										MaxLengthReachedBehaviorPage maxLengthReachedBehaviorPage,
										NumericValidationBehaviorPage numericValidationBehaviorPage,
										UserStoppedTypingBehaviorPage userStoppedTypingBehaviorPage,
										CharactersValidationBehaviorPage charactersValidationBehaviorPage,
										ProgressBarAnimationBehaviorPage progressBarAnimationBehaviorPage,
										SetFocusOnEntryCompletedBehaviorPage setFocusOnEntryCompletedBehaviorPage,
										RequiredStringValidationBehaviorPage requiredStringValidationBehaviorPage)
		: base(new[]
		{
			new SectionModel(
				eventToCommandBehaviorPage,
				nameof(EventToCommandBehavior),
				"Turns any event into a command that can be bound to"),

			new SectionModel(
				maskedBehaviorPage,
				nameof(MaskedBehavior),
				"Masked text in entry with specific pattern"),

			new SectionModel(
				userStoppedTypingBehaviorPage,
				nameof(UserStoppedTypingBehavior),
				"This behavior waits for the user to stop typing and then executes a Command"),

			new SectionModel(
				maxLengthReachedBehaviorPage,
				nameof(MaxLengthReachedBehavior),
				"This behavior invokes an EventHandler and executes a Command when the MaxLength of an InputView has been reached"),

			new SectionModel(
				progressBarAnimationBehaviorPage,
				nameof(ProgressBarAnimationBehavior),
				"Animate the progress for the ProgressBar"),

			new SectionModel(
				setFocusOnEntryCompletedBehaviorPage,
				nameof(SetFocusOnEntryCompletedBehavior),
				"Set focus to another element when an entry is completed"),

			new SectionModel(
				charactersValidationBehaviorPage,
				nameof(CharactersValidationBehavior),
				"Changes an Entry's text color when an invalid string is provided."),

			new SectionModel(
				textValidationBehaviorPage,
				nameof(TextValidationBehavior),
				"Changes an Entry's text color when text validation is failed (based on regex)"),

			new SectionModel(
				multiValidationBehaviorPage,
				nameof(MultiValidationBehavior),
				"Combines multiple validation behavior"),

			new SectionModel(
				uriValidationBehaviorPage,
				nameof(UriValidationBehavior),
				"Changes an Entry's text color when an invalid URI is provided"),

			new SectionModel(
				requiredStringValidationBehaviorPage,
				nameof(RequiredStringValidationBehavior),
				"Changes an Entry's text color when a required string is not provided"),

			new SectionModel(
				numericValidationBehaviorPage,
				nameof(NumericValidationBehavior),
				"Changes an Entry's text color when an invalid number is provided"),

			new SectionModel(
				emailValidationBehaviorPage,
				nameof(EmailValidationBehavior),
				"Changes an Entry's text color when an invalid e-mail address is provided"),
			}
		)
	{

	}		
}