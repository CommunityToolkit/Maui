using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.Pages.Behaviors;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public class BehaviorsGalleryViewModel : BaseGalleryViewModel
{
	readonly MaskedBehaviorPage _maskedBehaviorPage;
	readonly UriValidationBehaviorPage _uriValidationBehaviorPage;
	readonly EventToCommandBehaviorPage _eventToCommandBehaviorPage;
	readonly TextValidationBehaviorPage _textValidationBehaviorPage;
	readonly EmailValidationBehaviorPage _emailValidationBehaviorPage;
	readonly MultiValidationBehaviorPage _multiValidationBehaviorPage;
	readonly MaxLengthReachedBehaviorPage _maxLengthReachedBehaviorPage;
	readonly NumericValidationBehaviorPage _numericValidationBehaviorPage;
	readonly UserStoppedTypingBehaviorPage _userStoppedTypingBehaviorPage;
	readonly CharactersValidationBehaviorPage _charactersValidationBehaviorPage;
	readonly ProgressBarAnimationBehaviorPage _progressBarAnimationBehaviorPage;
	readonly SetFocusOnEntryCompletedBehaviorPage _setFocusOnEntryCompletedBehaviorPage;
	readonly RequiredStringValidationBehaviorPage _requiredStringValidationBehaviorPage;

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
	{
		_maskedBehaviorPage = maskedBehaviorPage;
		_uriValidationBehaviorPage = uriValidationBehaviorPage;
		_eventToCommandBehaviorPage = eventToCommandBehaviorPage;
		_textValidationBehaviorPage = textValidationBehaviorPage;
		_emailValidationBehaviorPage = emailValidationBehaviorPage;
		_multiValidationBehaviorPage = multiValidationBehaviorPage;
		_maxLengthReachedBehaviorPage = maxLengthReachedBehaviorPage;
		_numericValidationBehaviorPage = numericValidationBehaviorPage;
		_userStoppedTypingBehaviorPage = userStoppedTypingBehaviorPage;
		_charactersValidationBehaviorPage = charactersValidationBehaviorPage;
		_progressBarAnimationBehaviorPage = progressBarAnimationBehaviorPage;
		_setFocusOnEntryCompletedBehaviorPage = setFocusOnEntryCompletedBehaviorPage;
		_requiredStringValidationBehaviorPage = requiredStringValidationBehaviorPage;
	}

	protected override IEnumerable<SectionModel> CreateItems() => new[]
	{
		new SectionModel(
			_eventToCommandBehaviorPage,
			nameof(EventToCommandBehavior),
			"Turns any event into a command that can be bound to"),
		new SectionModel(
			_maskedBehaviorPage,
			nameof(MaskedBehavior),
			"Masked text in entry with specific pattern"),
		new SectionModel(
			_userStoppedTypingBehaviorPage,
			nameof(UserStoppedTypingBehavior),
			"This behavior waits for the user to stop typing and then executes a Command"),
		new SectionModel(
			_maxLengthReachedBehaviorPage,
			nameof(MaxLengthReachedBehavior),
			"This behavior invokes an EventHandler and executes a Command when the MaxLength of an InputView has been reached"),
		new SectionModel(
			_progressBarAnimationBehaviorPage,
			nameof(ProgressBarAnimationBehavior),
			"Animate the progress for the ProgressBar"),
		new SectionModel(
			_setFocusOnEntryCompletedBehaviorPage,
			nameof(SetFocusOnEntryCompletedBehavior),
			"Set focus to another element when an entry is completed"),
		new SectionModel(
			_charactersValidationBehaviorPage,
			nameof(CharactersValidationBehavior),
			"Changes an Entry's text color when an invalid string is provided."),
		new SectionModel(
			_textValidationBehaviorPage,
			nameof(TextValidationBehavior),
			"Changes an Entry's text color when text validation is failed (based on regex)"),
		new SectionModel(
			_multiValidationBehaviorPage,
			nameof(MultiValidationBehavior),
			"Combines multiple validation behavior"),
		new SectionModel(
			_uriValidationBehaviorPage,
			nameof(UriValidationBehavior),
			"Changes an Entry's text color when an invalid URI is provided"),
		new SectionModel(
			_requiredStringValidationBehaviorPage,
			nameof(RequiredStringValidationBehavior),
			"Changes an Entry's text color when a required string is not provided"),
		new SectionModel(
			_numericValidationBehaviorPage,
			nameof(NumericValidationBehavior),
			"Changes an Entry's text color when an invalid number is provided"),
		new SectionModel(
			_emailValidationBehaviorPage,
			nameof(EmailValidationBehavior),
			"Changes an Entry's text color when an invalid e-mail address is provided"),
	};
}