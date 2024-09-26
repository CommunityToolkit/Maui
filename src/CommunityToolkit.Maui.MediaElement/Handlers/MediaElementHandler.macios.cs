using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using AVKit;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.Core.Handlers;

public partial class MediaElementHandler : ViewHandler<MediaElement, MauiMediaElement>, IDisposable
{
	AVPlayerViewController? playerViewController;

	/// <inheritdoc/>
	/// <exception cref="NullReferenceException">Thrown if <see cref="MauiContext"/> is <see langword="null"/>.</exception>
	protected override MauiMediaElement CreatePlatformView()
	{
		if (MauiContext is null)
		{
			throw new InvalidOperationException($"{nameof(MauiContext)} cannot be null");
		}

		mediaManager ??= new(MauiContext,
			VirtualView,
			Dispatcher.GetForCurrentThread() ?? throw new InvalidOperationException($"{nameof(IDispatcher)} cannot be null"),
			loggerFactory);

		(_, playerViewController) = mediaManager.CreatePlatformView();

		if (VirtualView.TryFindParent<Page>(out var page)
			&& page.Handler is PageHandler { ViewController: not null } pageHandler)
		{
			return new(playerViewController, pageHandler.ViewController);
		}

		// The top-most parent is null when MediaElement is placed in a DataTemplate because DataTemplates defer loading until they are about to be displayed on the screen 
		// Subscribe to ParentChanged and set the UIViewController once the DataTemplate's Parent has been set
		VirtualView.GetTopMostParent().ParentChanged += HandleMediaElementParentChanged;
		return new(playerViewController, null);
	}

	/// <inheritdoc/>
	protected override void DisconnectHandler(MauiMediaElement platformView)
	{
		platformView.Dispose();
		Dispose();

		base.DisconnectHandler(platformView);
	}

	partial void PlatformDispose()
	{
		playerViewController?.Dispose();
		playerViewController = null;
	}

	void HandleMediaElementParentChanged(object? sender, EventArgs e)
	{
		ArgumentNullException.ThrowIfNull(sender);

		if (playerViewController is null)
		{
			throw new InvalidOperationException($"{nameof(playerViewController)} must be set in the {nameof(CreatePlatformView)} method");
		}

		if (VirtualView.TryFindParent<ItemsView>(out var itemsView) && itemsView.Handler is not null)
		{
			var parentViewController = itemsView.Handler switch
			{
				CarouselViewHandler carouselViewHandler => carouselViewHandler.ViewController ?? GetInternalController(carouselViewHandler),
				CollectionViewHandler collectionViewHandler => collectionViewHandler.ViewController ?? GetInternalController(collectionViewHandler),
				_ => throw new NotSupportedException($"{itemsView.Handler.GetType()} not yet supported")
			};
			
			parentViewController.AddChildViewController(playerViewController);

			VirtualView.ParentChanged -= HandleMediaElementParentChanged;
		}

		// The Controller we need is a `protected internal` property in the ItemsViewContoller class: https://github.com/dotnet/maui/blob/cf002538cb73db4bf187a51e4786d7478a7025ee/src/Controls/src/Core/Handlers/Items/ItemsViewHandler.iOS.cs#L39
		// In this method, we must use reflection to get the value of its backing field 
		static ItemsViewController<TItemsView> GetInternalController<TItemsView>(ItemsViewHandler<TItemsView> handler) where TItemsView : ItemsView
		{
			var nonPublicInstanceFields = typeof(ItemsViewHandler<TItemsView>).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

			var controllerProperty = nonPublicInstanceFields.Single(x => x.FieldType == typeof(ItemsViewController<TItemsView>));
			return (ItemsViewController<TItemsView>)(controllerProperty.GetValue(handler) ?? throw new InvalidOperationException($"Unable to get the value for the Controller property on {handler.GetType()}"));
		}
	}
}

static class ParentPage
{
	public static bool TryFindParent<T>(this VisualElement? child, [NotNullWhen(true)] out T? parent) where T : VisualElement
	{
		while (child is not null)
		{
			if (child.Parent is T element)
			{
				parent = element;
				return true;
			}

			child = child.Parent as VisualElement;
		}

		parent = null;
		return false;
	}

	public static Element GetTopMostParent(this Element child)
	{
		while (child.Parent is not null)
		{
			child = child.Parent;
		}

		return child;
	}
}