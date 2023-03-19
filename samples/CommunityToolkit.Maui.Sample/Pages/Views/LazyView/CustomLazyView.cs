using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views.LazyView;

public class CustomLazyView<TView> : BaseLazyView where TView : View, new()
{
	public override async ValueTask LoadViewAsync()
	{
		// display a loading indicator
		Content = new ActivityIndicator { IsRunning = true, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };

		// simulate a long running task
		await Task.Delay(3000);

		// load the view
		View view = new TView { BindingContext = BindingContext };
		Content = view;
		SetIsLoaded(true);
	}
}

