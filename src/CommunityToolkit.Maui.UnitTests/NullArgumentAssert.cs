using Xunit;

namespace CommunityToolkit.Maui.UnitTests;

static class NullArgumentAssert
{
	// Task-returning members must throw before they return the Task, like the .NET libraries do, so no call site can drop a null argument unnoticed by not awaiting the Task
	public static void Throws(string expectedParameterName, Action action)
	{
		var exception = Assert.Throws<ArgumentNullException>(action);

		Assert.Equal(expectedParameterName, exception.ParamName);
	}
}