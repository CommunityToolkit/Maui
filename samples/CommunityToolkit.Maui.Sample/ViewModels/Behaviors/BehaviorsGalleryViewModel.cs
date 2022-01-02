using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.Pages.Behaviors;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public class BehaviorsGalleryViewModel : BaseGalleryViewModel
{
	protected override IEnumerable<SectionModel> CreateItems() => new[]
	{
		new SectionModel(
			typeof(EventToCommandBehaviorPage),
			nameof(EventToCommandBehavior),
			"Turns any event into a command that can be bound to"),
		new SectionModel(
			typeof(MaskedBehaviorPage),
			nameof(MaskedBehavior),
			"Masked text in entry with specific pattern"),
		new SectionModel(
			typeof(UserStoppedTypingBehaviorPage),
			nameof(UserStoppedTypingBehavior),
			"This behavior waits for the user to stop typing and then executes a Command"),
		new SectionModel(
			typeof(MaxLengthReachedBehaviorPage),
			nameof(MaxLengthReachedBehavior),
			"This behavior invokes an EventHandler and executes a Command when the MaxLength of an InputView has been reached"),
		new SectionModel(
			typeof(ProgressBarAnimationBehaviorPage),
			nameof(ProgressBarAnimationBehavior),
			"Animate the progress for the ProgressBar"),
		new SectionModel(
			typeof(SetFocusOnEntryCompletedBehaviorPage),
			nameof(SetFocusOnEntryCompletedBehavior),
			"Set focus to another element when an entry is completed"),
		new SectionModel(
			typeof(CharactersValidationBehaviorPage),
			nameof(CharactersValidationBehavior),
			"Changes an Entry's text color when an invalid string is provided."),
		new SectionModel(
			typeof(TextValidationBehaviorPage),
			nameof(TextValidationBehavior),
			"Changes an Entry's text color when text validation is failed (based on regex)"),
		new SectionModel(
			typeof(MultiValidationBehaviorPage),
			nameof(MultiValidationBehavior),
			"Combines multiple validation behavior"),
		new SectionModel(
			typeof(UriValidationBehaviorPage),
			nameof(UriValidationBehavior),
			"Changes an Entry's text color when an invalid URI is provided"),
		new SectionModel(
			typeof(RequiredStringValidationBehaviorPage),
			nameof(RequiredStringValidationBehavior),
			"Changes an Entry's text color when a required string is not provided"),
		new SectionModel(
			typeof(NumericValidationBehaviorPage),
			nameof(NumericValidationBehavior),
			"Changes an Entry's text color when an invalid number is provided"),
		new SectionModel(
			typeof(EmailValidationBehaviorPage),
			nameof(EmailValidationBehavior),
			"Changes an Entry's text color when an invalid e-mail address is provided"),
	};
}