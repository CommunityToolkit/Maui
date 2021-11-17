using System.Threading;
using System.Threading.Tasks;

namespace CommunityToolkit.Maui.Behaviors;

public class MockValidationBehavior : ValidationBehavior
{
	public object? ExpectedValue { get; set; }
	public bool SimulateValidationDelay { get; set; } = false;
	protected override async ValueTask<bool> ValidateAsync(object? value, CancellationToken token)
	{
		if (SimulateValidationDelay)
		{
			await Task.Delay(1000, token);
		}

		return value == ExpectedValue;
	}
}
