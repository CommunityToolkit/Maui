using CommunityToolkit.Maui.Animations;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

public class FadeAnimationNullArgumentTests : BaseTest
{
	[Fact]
	public void Animate_NullView_ShouldThrowArgumentNullExceptionBeforeItReturnsTheTask()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		NullArgumentAssert.Throws("view", () => { _ = new FadeAnimation().Animate(null, TestContext.Current.CancellationToken); });
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}