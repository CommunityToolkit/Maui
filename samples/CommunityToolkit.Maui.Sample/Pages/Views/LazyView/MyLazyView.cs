using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views.LazyView;

public class MyLazyView : LazyView<MyView>
{
	public override async ValueTask LoadViewAsync()
	{
		await base.LoadViewAsync();
	}
}