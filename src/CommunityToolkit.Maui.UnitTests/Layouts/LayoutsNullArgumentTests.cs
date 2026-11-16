using CommunityToolkit.Maui.Layouts;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Layouts;

#pragma warning disable CS8604, CS8625 // Possible null reference argument; cannot convert null literal to non-nullable reference type.
public class LayoutsNullArgumentTests : BaseTest
{
	[Fact]
	public void DockLayout_NullView_ShouldThrowArgumentNullException()
	{
		var dockLayout = new DockLayout();

		NullArgumentAssert.Throws("view", () => dockLayout.GetDockPosition(null));
		NullArgumentAssert.Throws("view", () => dockLayout.Add(null));
	}

	[Fact]
	public void ChangeStateWithAnimation_NullBindable_ShouldThrowArgumentNullExceptionBeforeItReturnsTheTask()
	{
		var token = TestContext.Current.CancellationToken;

		NullArgumentAssert.Throws("bindable", () => { _ = StateContainer.ChangeStateWithAnimation(null, "state", token); });
		NullArgumentAssert.Throws("bindable", () => { _ = StateContainer.ChangeStateWithAnimation(null, "state", (Animation?)null, (Animation?)null, token); });
		NullArgumentAssert.Throws("bindable", () => { _ = StateContainer.ChangeStateWithAnimation(null, "state", (Func<VisualElement, CancellationToken, Task>?)null, (Func<VisualElement, CancellationToken, Task>?)null, token); });
	}
}
#pragma warning restore CS8604, CS8625