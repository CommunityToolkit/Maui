using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// This behaviors allows you to select all text at once.
/// </summary>
public partial class SelectAllTextBehavior : BaseBehavior<InputView>
{
	/// <inheritdoc />
	protected override async void OnAttachedTo(InputView bindable)
	{
		base.OnAttachedTo(bindable);
		await Task.Delay(5000);
		OnPlatformkAttachedBehavior(bindable);
	}

	/// <inheritdoc />
	protected override void OnDetachingFrom(InputView bindable)
	{
		base.OnDetachingFrom(bindable);
		OnPlatformDeattachedBehavior(bindable);
	}

	partial void OnPlatformkAttachedBehavior(InputView view);
	partial void OnPlatformDeattachedBehavior(InputView view);
}
