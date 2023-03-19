using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// This a basic implementation of the LazyView based on <see cref="BaseLazyView"/> use this an example to create yours
/// </summary>
/// <typeparam name="TView">Any <see cref="View"/></typeparam>
public class LazyView<TView> : BaseLazyView where TView : View, new()
{
	/// <summary>
	/// This method initializes your <see cref="LazyView{TView}"/>.
	/// </summary>
	/// <returns><see cref="ValueTask"/></returns>
	public override ValueTask LoadViewAsync()
	{
		View view = new TView { BindingContext = BindingContext };

		Content = view;

		SetIsLoaded(true);
		return new ValueTask(Task.FromResult(true));
	}
}