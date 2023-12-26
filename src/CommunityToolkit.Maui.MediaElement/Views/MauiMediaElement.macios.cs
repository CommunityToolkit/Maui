using AVKit;
using UIKit;
using Microsoft.Maui.Handlers;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The user-interface element that represents the <see cref="MediaElement"/> on iOS and macOS.
/// </summary>
public class MauiMediaElement : UIView
{
	AVPlayerViewController? playerViewController = null;
	Element? rootElement = null;
	
	/// <summary>
	/// Initializes a new instance of the <see cref="MauiMediaElement"/> class.
	/// </summary>
	/// <param name="playerViewController">The <see cref="AVPlayerViewController"/> that acts as the platform media player.</param>
	/// <param name="virtualView">The <see cref="Element"/> that is that virtual vie/>.</param>
	/// <exception cref="NullReferenceException">Thrown when <paramref name="playerViewController"/><c>.View</c> is <see langword="null"/>.</exception>
	public MauiMediaElement(AVPlayerViewController playerViewController, Element virtualView)
    {
        ArgumentNullException.ThrowIfNull(playerViewController.View);
        ArgumentNullException.ThrowIfNull(virtualView);

        this.playerViewController = playerViewController;

        // Zero out the original implementation
        //RemoveFromParents();

        TrySetSubview(virtualView);
    }

    void TrySetSubview(Element virtualView)
    {
        // Try and find parent controller on virtualView
        // If it's not found, try to find the view controller for the page
        var parentViewController = FindParentController(virtualView) ?? FindPageController(virtualView);

        if (parentViewController is null)
        {
            // If no ViewController was found, try to wait for adding the view to a parent
            // This is needed when the MediaElement is part of a CollectionView or CarouselView cell
            rootElement = FindRootElement(virtualView);
            rootElement.ParentChanging += ElementParentChanging;
        }
        else
        {
            if (rootElement is not null)
			{
				rootElement.ParentChanging -= ElementParentChanging;
			}

			rootElement = null;
        }

        SetSubviewWithParentController(parentViewController);
    }

    void SetSubviewWithParentController(UIViewController? parentViewController)
    {
        RemoveFromParents();

        if (playerViewController is not { View: not null })
        {
	        return;
        }

        playerViewController.View.Frame = Bounds;

#if IOS16_0_OR_GREATER || MACCATALYST16_1_OR_GREATER
        // On iOS 16+ and macOS 13+ the AVPlayerViewController has to be added to a parent ViewController, otherwise the transport controls won't be displayed.

        parentViewController ??= WindowStateManager.Default.GetCurrentUIViewController();

        if (parentViewController?.View is not null)
        {
	        // Zero out the safe area insets of the AVPlayerViewController
	        UIEdgeInsets insets = parentViewController.View.SafeAreaInsets;
	        playerViewController.AdditionalSafeAreaInsets =
		        new UIEdgeInsets(insets.Top * -1, insets.Left, insets.Bottom * -1, insets.Right);
	        // Add the View from the AVPlayerViewController to the parent ViewController
	        parentViewController.AddChildViewController(playerViewController);
	        parentViewController.View.AddSubview(playerViewController.View);

	        playerViewController.DidMoveToParentViewController(parentViewController);
        }
#endif

        AddSubview(playerViewController.View);
    }

    void RemoveFromParents()
    {
        foreach (var subview in Subviews)
        {
            subview.RemoveFromSuperview();
        }

        playerViewController?.RemoveFromParentViewController();
        playerViewController?.View?.RemoveFromSuperview();
    }

    UIViewController? FindParentController(Element element)
    {
        // Does this element have a ViewController?
        if (element.Handler?.PlatformView is UIResponder { NextResponder: UIViewController viewController })
        {
            // Is it the right ViewController?
            var controller = GetCorrectController(viewController);
            if (controller is not null)
            {
                return controller;
            }
        }

        // Try to find the controller in the parent element
        if (element.Parent is not null)
        {
            return FindParentController(element.Parent);
        }

        return null;
    }

    static UIViewController? GetCorrectController(UIViewController controller)
    {
        return controller switch
        {
            NavigationRenderer => null, // If a NavigationPage is used, the Page ViewController needs to be used instead
            ShellFlyoutRenderer => controller, // Shell support
            UICollectionViewController => controller, // CollectionView and CarouselView support
            UINavigationController navigationController => navigationController.VisibleViewController, // Simple Shell support
            _ => null
        };
    }

    static UIViewController? FindPageController(Element element)
    {
        if (element.Handler is PageHandler pageHandler)
        {
            return pageHandler.ViewController;
        }

        return FindPageController(element.Parent);
    }

    static Element FindRootElement(Element element)
    {
        while (element.Parent is not null)
        {
            element = element.Parent;
        }

        return element;
    }

    void ElementParentChanging(object? sender, ParentChangingEventArgs e)
    {
        TrySetSubview(e.NewParent);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="disposing"></param>
    protected override void Dispose(bool disposing)
    {
	    base.Dispose(disposing);

	    if (rootElement is not null)
	    {
		    rootElement.ParentChanging -= ElementParentChanging;
	    }

	    playerViewController = null;
	    rootElement = null;
    }
}