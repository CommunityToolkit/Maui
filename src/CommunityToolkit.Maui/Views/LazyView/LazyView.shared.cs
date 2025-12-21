namespace CommunityToolkit.Maui.Views;

/// <summary>
/// This a basic implementation implementing <see cref="LazyView"/>
/// </summary>
/// <typeparam name="TView">Any <see cref="View"/></typeparam>
public partial class LazyView<TView> : LazyView where TView : View, new()
{
	/// <summary>
	/// This method initializes <see cref="LazyView{TView}"/>.
	/// </summary>
	/// <returns><see cref="ValueTask"/></returns>
	public override ValueTask LoadViewAsync(CancellationToken token = default)
	{
		token.ThrowIfCancellationRequested();

		Content = new TView { BindingContext = BindingContext };

		SetHasLazyViewLoaded(true);

		return ValueTask.CompletedTask;
	}
}

/// <summary>
/// Abstract base class for <see cref="LazyView{TView}"/>
/// </summary>
public abstract partial class LazyView : ContentView
{
	/// <summary>
	/// Gets a value indicating whether the view has been loaded.
	/// </summary>
	[BindableProperty]
	public partial bool HasLazyViewLoaded { get; }

	/// <summary>
	/// Use this method to do the initialization of the <see cref="View"/> and change the status HasViewLoaded value here.
	/// </summary>
	/// <returns><see cref="ValueTask"/></returns>
	public abstract ValueTask LoadViewAsync(CancellationToken token = default);

	/// <inheritdoc/>
	protected override void OnBindingContextChanged()
	{
		if (Content is not null && Content is not ActivityIndicator)
		{
			Content.BindingContext = BindingContext;
		}
	}

	/// <summary>
	/// This method changes the value of the <see cref="HasLazyViewLoaded"/> property.
	/// </summary>
	/// <param name="hasLoaded"></param>
	protected void SetHasLazyViewLoaded(bool hasLoaded) => SetValue(hasLazyViewLoadedPropertyKey, hasLoaded);
}