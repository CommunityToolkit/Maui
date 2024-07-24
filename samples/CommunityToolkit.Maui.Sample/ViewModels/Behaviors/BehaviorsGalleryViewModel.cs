using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Sample.Models;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public class BehaviorsGalleryViewModel() : BaseGalleryViewModel(
[
	SectionModel.Create<EventToCommandBehaviorViewModel>(nameof(EventToCommandBehavior), "Turns any event into a command that can be bound to"),
	SectionModel.Create<MaskedBehaviorViewModel>(nameof(MaskedBehavior), "Masked text in entry with specific pattern"),
	SectionModel.Create<UserStoppedTypingBehaviorViewModel>(nameof(UserStoppedTypingBehavior), "This behavior waits for the user to stop typing and then executes a Command"),
	SectionModel.Create<MaxLengthReachedBehaviorViewModel>(nameof(MaxLengthReachedBehavior), "This behavior invokes an EventHandler and executes a Command when the MaxLength of an InputView has been reached"),
	SectionModel.Create<ProgressBarAnimationBehaviorViewModel>(nameof(ProgressBarAnimationBehavior), "Animate the progress for the ProgressBar"),
	SectionModel.Create<SetFocusOnEntryCompletedBehaviorViewModel>(nameof(SetFocusOnEntryCompletedBehavior), "Set focus to another element when an entry is completed"),
	SectionModel.Create<CharactersValidationBehaviorViewModel>(nameof(CharactersValidationBehavior), "Changes an Entry's text color when an invalid string is provided."),
	SectionModel.Create<TextValidationBehaviorViewModel>(nameof(TextValidationBehavior), "Changes an Entry's text color when text validation is failed (based on regex)"),
	SectionModel.Create<MultiValidationBehaviorViewModel>(nameof(MultiValidationBehavior), "Combines multiple validation behavior"),
	SectionModel.Create<UriValidationBehaviorViewModel>(nameof(UriValidationBehavior), "Changes an Entry's text color when an invalid URI is provided"),
	SectionModel.Create<RequiredStringValidationBehaviorViewModel>(nameof(RequiredStringValidationBehavior), "Changes an Entry's text color when a required string is not provided"),
	SectionModel.Create<NumericValidationBehaviorViewModel>(nameof(NumericValidationBehavior), "Changes an Entry's text color when an invalid number is provided"),
	SectionModel.Create<EmailValidationBehaviorViewModel>(nameof(EmailValidationBehavior), "Changes an Entry's text color when an invalid e-mail address is provided"),
	SectionModel.Create<AnimationBehaviorViewModel>(nameof(AnimationBehavior), "Perform animation when a specified UI element event is triggered"),
	SectionModel.Create<SelectAllTextBehaviorViewModel>(nameof(SelectAllTextBehavior), "Select all text inside the Entry or Editor control."),
	SectionModel.Create<IconTintColorBehaviorViewModel>(nameof(IconTintColorBehavior), "Tint an icon with the selected color."),
	SectionModel.Create<StatusBarBehaviorViewModel>(nameof(StatusBarBehavior), "Change the Status Bar color."),
	SectionModel.Create<TouchBehaviorViewModel>(nameof(TouchBehavior), "Alter a views appearance when responding to touch events (normal, pressed, hovered, long press).")
])
{

}