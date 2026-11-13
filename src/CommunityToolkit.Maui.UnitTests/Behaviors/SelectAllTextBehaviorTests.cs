using CommunityToolkit.Maui.Behaviors;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class SelectAllTextBehaviorTests() : BaseBehaviorTest<SelectAllTextBehavior, InputView>(new SelectAllTextBehavior(), new Entry())
{
	[Fact]
	public void VerifyAttachToEntryAndEditorSucceeds()
	{
		var entry = new Entry
		{
			Behaviors = { new SelectAllTextBehavior() }
		};

		var editor = new Editor
		{
			Behaviors = { new SelectAllTextBehavior() }
		};

		Assert.Single(entry.Behaviors.OfType<SelectAllTextBehavior>());
		Assert.Single(editor.Behaviors.OfType<SelectAllTextBehavior>());
	}
}