namespace CommunityToolkit.Maui.UnitTests.Mocks;

public class MockPageRouteFactory : RouteFactory
{
	public bool WasInvoked { get; set; }

	public override Element GetOrCreate()
	{
		return new Page();
	}

	public override Element GetOrCreate(IServiceProvider services)
	{
		WasInvoked = true;
		return services.GetService<MockPage>() ?? throw new InvalidOperationException($"{nameof(MockPage)} has not been added to `IServiceCollection`");
	}
}