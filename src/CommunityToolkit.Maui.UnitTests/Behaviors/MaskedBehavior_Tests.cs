using System.ComponentModel;
using CommunityToolkit.Maui.Behaviors;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class MaskedBehavior_Tests : BaseTest
{
	[Theory]
	[InlineData(null, null, null)]
	[InlineData(null, "", "")]
	[InlineData("", "", "")]
	[InlineData("", "abc123", "abc123")]
	[InlineData("XX-XX-XXX", null, null)]
	[InlineData("XX-XX-XXX", "ab12cde", "ab-12-cde")]
	[InlineData("XX-XX-XXX", "ab12cdefg", "ab-12-cde")]
	[InlineData("XX/XX/XXXX", "05312021", "05/31/2021")]
	[InlineData("XX/XX/XXXX", "05312021abc", "05/31/2021")]
	public void ValidMaskTests(string? mask, string? input, string? expectedResult)
	{
		var maskedBehavior = new MaskedBehavior
		{
			Mask = mask
		};

		var entry = new Entry
		{
			Behaviors = { maskedBehavior },
			Text = input
		};

		Assert.Equal(expectedResult, entry.Text);
	}

	[Theory]
	[InlineData(null, 'c', null, null)]
	[InlineData(null, 'c', "", "")]
	[InlineData("AA-AA-AAA", 'A', null, null)]
	[InlineData("11-11-111", '1', "ab12cde", "ab-12-cde")]
	[InlineData("@@-@@-@@@", '@', "ab12cdefg", "ab-12-cde")]
	[InlineData("zz/zz/zzzz", 'z', "05312021", "05/31/2021")]
	[InlineData("!!/!!/!!!!", '!', "05312021abc", "05/31/2021")]
	public void ValidMaskWithUniqueUnmaskedCharacterTests(string? mask, char unmaskedCharacter, string? input, string? expectedResult)
	{
		var maskedBehavior = new MaskedBehavior
		{
			Mask = mask,
			UnmaskedCharacter = unmaskedCharacter
		};

		var entry = new Editor
		{
			Behaviors = { maskedBehavior },
			Text = input
		};

		Assert.Equal(expectedResult, entry.Text);
	}

	[Fact]
	public void AttachedToInvalidElementTest()
	{
		IReadOnlyList<VisualElement> invalidVisualElements = new[] 
		{
			new Button(),
			new Frame(),
			new Label(),
			new ProgressBar(),
			new VisualElement(),
			new View(),
		};

		foreach (var invalidVisualElement in invalidVisualElements)
		{
			Assert.Throws<InvalidOperationException>(() => invalidVisualElement.Behaviors.Add(new MaskedBehavior()));
		}
	}
}

