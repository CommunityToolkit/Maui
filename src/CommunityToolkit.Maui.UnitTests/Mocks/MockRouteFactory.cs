using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

public class MockRouteFactory : RouteFactory
{
	public bool WasInvoked { get; set; }

	public override Element GetOrCreate()
	{
		return new Page();
	}

	public override Element GetOrCreate(IServiceProvider services)
	{
		WasInvoked = true;
		return services.GetService<MockPage>() ?? new Page();
	}
}
