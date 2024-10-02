using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views.LazyView;

class MyViewLazyView : LazyView<MyView>
{
	public override async ValueTask LoadViewAsync(CancellationToken token = default)
	{
		await base.LoadViewAsync(token);
	}
}