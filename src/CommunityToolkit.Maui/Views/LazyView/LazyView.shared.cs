namespace CommunityToolkit.Maui.Views;

/// <summary>
/// This a basic implementation implementing <see cref="LazyView"/>
/// </summary>
/// <typeparam name="TView">Any <see cref="View"/></typeparam>
public class LazyView<TView> : LazyView where TView : View, new()
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
public abstract class LazyView : ContentView
{
	internal static readonly BindablePropertyKey HasLazyViewLoadedPropertyKey = BindableProperty.CreateReadOnly(nameof(HasLazyViewLoaded), typeof(bool), typeof(LazyView), false);

	/// <summary>
	/// This is a read-only <see cref="BindableProperty"/> that indicates when the view is loaded.
	/// </summary>
	public static readonly BindableProperty HasLazyViewLoadedProperty = HasLazyViewLoadedPropertyKey.BindableProperty;

	/// <summary>
	/// This is a read-only property that indicates when the view is loaded.
	/// </summary>
	public bool HasLazyViewLoaded => (bool)GetValue(HasLazyViewLoadedProperty);

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
	/// This method change the value of the <see cref="HasLazyViewLoaded"/> property.
	/// </summary>
	/// <param name="hasLoaded"></param>
	protected void SetHasLazyViewLoaded(bool hasLoaded) => SetValue(HasLazyViewLoadedPropertyKey, hasLoaded);
}