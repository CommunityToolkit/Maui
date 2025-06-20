using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CommunityToolkit.Maui.Services;

/// <inheritdoc cref="IPopupService"/>
public class PopupService : IPopupService
{
	static readonly Dictionary<Type, Type> viewModelToViewMappings = [];

	readonly IServiceProvider serviceProvider;

	/// <summary>
	/// Creates a new instance of <see cref="PopupService"/>.
	/// </summary>
	/// <param name="serviceProvider">The <see cref="IServiceProvider"/> implementation.</param>
	[ActivatorUtilitiesConstructor]
	public PopupService(IServiceProvider serviceProvider)
	{
		this.serviceProvider = serviceProvider;
	}

	/// <summary>
	/// Creates a new instance of <see cref="PopupService"/>.
	/// </summary>
	public PopupService()
	{
		serviceProvider = IPlatformApplication.Current?.Services
						  ?? throw new InvalidOperationException("Could not locate IServiceProvider");
	}

	/// <inheritdoc />
	/// <remarks>This is an <see langword="async"/> <see langword="void"/> method. Use <see cref="ShowPopupAsync{T}(Microsoft.Maui.Controls.INavigation,CommunityToolkit.Maui.IPopupOptions?,System.Threading.CancellationToken)"/> to <see langword="await"/> this method</remarks>
	public void ShowPopup<T>(Page page, IPopupOptions? options = null) where T : notnull
	{
		ArgumentNullException.ThrowIfNull(page);
		ShowPopup<T>(page.Navigation, options);
	}

	/// <inheritdoc />
	/// <remarks>This is an <see langword="async"/> <see langword="void"/> method. Use <see cref="ShowPopupAsync{T}(Microsoft.Maui.Controls.INavigation,CommunityToolkit.Maui.IPopupOptions?,System.Threading.CancellationToken)"/> to <see langword="await"/> this method</remarks>
	public void ShowPopup<T>(INavigation navigation, IPopupOptions? options = null) where T : notnull
	{
		ArgumentNullException.ThrowIfNull(navigation);

		var popupContent = GetPopupContent(serviceProvider.GetRequiredService<T>());

		navigation.ShowPopup(popupContent, options);
	}

	/// <inheritdoc />
	/// <remarks>This is an <see langword="async"/> <see langword="void"/> method. Use <see cref="ShowPopupAsync{T}(Microsoft.Maui.Controls.Shell,CommunityToolkit.Maui.IPopupOptions?,IDictionary{string,object}?, System.Threading.CancellationToken)"/> to <see langword="await"/> this method</remarks>
	public void ShowPopup<T>(Shell shell, IPopupOptions? options = null, IDictionary<string, object>? shellParameters = null) where T : notnull
	{
		ArgumentNullException.ThrowIfNull(shell);

		var popupContent = GetPopupContent(serviceProvider.GetRequiredService<T>());
		shell.ShowPopup(popupContent, options, shellParameters);
	}

	/// <inheritdoc />
	public Task<IPopupResult> ShowPopupAsync<T>(Page page, IPopupOptions? options = null, CancellationToken cancellationToken = default) where T : notnull
	{
		ArgumentNullException.ThrowIfNull(page);

		return ShowPopupAsync<T>(page.Navigation, options, cancellationToken);
	}

	/// <inheritdoc />
	public Task<IPopupResult> ShowPopupAsync<T>(INavigation navigation, IPopupOptions? options = null, CancellationToken token = default)
		where T : notnull
	{
		ArgumentNullException.ThrowIfNull(navigation);

		token.ThrowIfCancellationRequested();

		var popupContent = GetPopupContent(serviceProvider.GetRequiredService<T>());

		return navigation.ShowPopupAsync(popupContent, options, token);
	}

	/// <inheritdoc />
	public Task<IPopupResult> ShowPopupAsync<T>(Shell shell, IPopupOptions? options, IDictionary<string, object>? shellParameters, CancellationToken token) where T : notnull
	{
		ArgumentNullException.ThrowIfNull(shell);

		token.ThrowIfCancellationRequested();

		var popupContent = GetPopupContent(serviceProvider.GetRequiredService<T>());

		return shell.ShowPopupAsync(popupContent, options, shellParameters, token);
	}

	/// <inheritdoc />
	public Task<IPopupResult<TResult>> ShowPopupAsync<T, TResult>(Page page, IPopupOptions? options = null, CancellationToken cancellationToken = default) where T : notnull
	{
		ArgumentNullException.ThrowIfNull(page);

		return ShowPopupAsync<T, TResult>(page.Navigation, options, cancellationToken);
	}

	/// <inheritdoc />
	public Task<IPopupResult<TResult>> ShowPopupAsync<T, TResult>(INavigation navigation,
		IPopupOptions? options = null,
		CancellationToken token = default)
		where T : notnull
	{
		ArgumentNullException.ThrowIfNull(navigation);

		token.ThrowIfCancellationRequested();
		var popupContent = GetPopupContent(serviceProvider.GetRequiredService<T>());

		return navigation.ShowPopupAsync<TResult>(popupContent, options, token);
	}

	/// <inheritdoc />
	public Task<IPopupResult<TResult>> ShowPopupAsync<T, TResult>(Shell shell, IPopupOptions? options, IDictionary<string, object>? shellParameters = null, CancellationToken token = default) where T : notnull
	{
		ArgumentNullException.ThrowIfNull(shell);

		token.ThrowIfCancellationRequested();
		var popupContent = GetPopupContent(serviceProvider.GetRequiredService<T>());

		return shell.ShowPopupAsync<TResult>(popupContent, options, shellParameters, token);
	}

	/// <inheritdoc />
	public Task<IPopupResult> ClosePopupAsync(Page page, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(page);
		return ClosePopupAsync(page.Navigation, cancellationToken);
	}

	/// <inheritdoc />
	public Task<IPopupResult<T>> ClosePopupAsync<T>(Page page, T result, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(page);
		return ClosePopupAsync(page.Navigation, result, cancellationToken);
	}

	/// <inheritdoc />
	public Task<IPopupResult> ClosePopupAsync(INavigation navigation, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(navigation);
		cancellationToken.ThrowIfCancellationRequested();

		return navigation.ClosePopupAsync(cancellationToken);
	}

	/// <inheritdoc />
	public Task<IPopupResult<T>> ClosePopupAsync<T>(INavigation navigation, T result, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(navigation);
		cancellationToken.ThrowIfCancellationRequested();

		return navigation.ClosePopupAsync(result, cancellationToken);
	}

	internal static void AddPopup<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPopupView>(
		IServiceCollection services, ServiceLifetime lifetime)
		where TPopupView : IView
	{
		viewModelToViewMappings.TryAdd(typeof(TPopupView), typeof(TPopupView));
		services.Add(new ServiceDescriptor(typeof(TPopupView), typeof(TPopupView), lifetime));
	}

	internal static void AddPopup<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPopupView, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPopupViewModel>
		(IServiceCollection services, ServiceLifetime lifetime)
		where TPopupView : IView
		where TPopupViewModel : notnull
	{
		viewModelToViewMappings.TryAdd(typeof(TPopupViewModel), typeof(TPopupView));

		services.TryAdd(new ServiceDescriptor(typeof(TPopupView), typeof(TPopupView), lifetime));
		services.TryAdd(new ServiceDescriptor(typeof(TPopupViewModel), typeof(TPopupViewModel), lifetime));
	}

	View GetPopupContent<T>(T bindingContext)
	{
		if (bindingContext is View view)
		{
			return view;
		}

		if (serviceProvider.GetRequiredService(viewModelToViewMappings[typeof(T)]) is View content)
		{
			return content;
		}

		throw new InvalidOperationException($"Could not locate {typeof(T).FullName}");
	}
}