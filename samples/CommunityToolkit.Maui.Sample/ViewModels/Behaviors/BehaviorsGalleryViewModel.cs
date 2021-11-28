using System.Collections.Generic;
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
		};
}