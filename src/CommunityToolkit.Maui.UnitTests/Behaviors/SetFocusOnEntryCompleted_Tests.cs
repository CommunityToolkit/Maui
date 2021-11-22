using CommunityToolkit.Maui.Behaviors;
using Microsoft.Maui.Controls;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class SetFocusOnEntryCompleted_Tests : BaseTest
{
	[Fact]
	public void DoesNotSetFocusBeforeCompletion()
	{
		// arrange
		var entry2 = CreateEntry();
		var entry1 = CreateEntry(entry2);

		// act
		entry1.Focus();
		entry1.Text = "text";

		// assert
		Assert.False(entry2.IsFocused);
	}

	[Fact]
	public void SetsFocusWhenCompleted()
	{
		// arrange
		var entry2 = CreateEntry();
		var entry1 = CreateEntry(entry2);

		// act
		entry1.Focus();
		entry1.SendCompleted();

		// assert
		Assert.True(entry2.IsFocused);
	}

	public Entry CreateEntry(VisualElement? nextElement = null)
	{
		var entry = new Entry();

		// We simulate Focus/Unfocus behavior ourselves
		// because unit tests doesn't have "platform-specific" part
		// where IsFocused is controlled in the real app
		entry.FocusChangeRequested += (s, e) => entry.SetValue(VisualElement.IsFocusedPropertyKey, e.Focus);

		if (nextElement != null)
			SetFocusOnEntryCompletedBehavior.SetNextElement(entry, nextElement);

		return entry;
	}
}