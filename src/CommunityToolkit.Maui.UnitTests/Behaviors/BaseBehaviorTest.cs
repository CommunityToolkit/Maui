using CommunityToolkit.Maui.Behaviors;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public abstract class BaseBehaviorTest<TBehavior, TView>() : BaseTest
	where TBehavior : ICommunityToolkitBehavior<TView>
	where TView : VisualElement
{
}