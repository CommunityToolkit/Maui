using System;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using UIKit;

namespace CommunityToolkit.Maui.Platform;

public class PopupRenderer : UIViewController
{
	bool isDisposed;
	readonly IMauiContext mauiContext;

	public PageHandler? Control { get; private set; }

	public IBasePopup? Element { get; private set; }

	public UIView? NativeView => View;

	public UIViewController? ViewController { get; private set; }

	//	[Preserve(Conditional = true)]
	public PopupRenderer(IMauiContext mauiContext)
	{
		this.mauiContext = mauiContext;
		ModalInPopover = true;
	}

	public void SetElementSize(Size size) =>
		Control?.ContainerView?.SizeThatFits(size);

	public override void ViewDidLayoutSubviews()
	{
		base.ViewDidLayoutSubviews();

		_ = View ?? throw new InvalidOperationException($"{nameof(View)} cannot be null");
		SetElementSize(new Size(View.Bounds.Width, View.Bounds.Height));
	}

	public override void ViewDidAppear(bool animated)
	{
		base.ViewDidAppear(animated);

		_ = Element ?? throw new InvalidOperationException($"{nameof(Element)} cannot be null");
		ModalInPopover = !Element.IsLightDismissEnabled;
	}

	//public SizeRequest GetDesiredSize(double widthConstraint, double heightConstraint) =>
	//	NativeView.GetSizeRequest(widthConstraint, heightConstraint);

	public void SetElement(IBasePopup element)
	{
		if (element is not IBasePopup)
		{
			throw new ArgumentException(nameof(element), "Element is not of type " + typeof(IBasePopup));
		}

		Element = element;
		ModalPresentationStyle = UIModalPresentationStyle.Popover;
		CreateControl();
		SetViewController();
		SetPresentationController();
		SetView();
		AddToCurrentPageViewController();
	}

	void CreateControl()
	{
		_ = Element ?? throw new NullReferenceException($"{nameof(Element)} cannot be null.");

		var view = (View?)Element.Content;
		var contentPage = new ContentPage { Content = view };

		Control = (PageHandler)contentPage.ToHandler(mauiContext);

		contentPage.Parent = Application.Current?.MainPage;
		contentPage.SetBinding(VisualElement.BindingContextProperty, new Binding { Source = Element, Path = VisualElement.BindingContextProperty.PropertyName });
	}

	void SetViewController()
	{
		Page currentPageRenderer;
		var modalStackCount = Application.Current?.MainPage?.Navigation?.ModalStack?.Count ?? 0;
		var stackCount = Application.Current?.MainPage?.Navigation?.NavigationStack?.Count ?? 0;
		var mainPage = Application.Current?.MainPage ?? throw new NullReferenceException(nameof(Application.Current.MainPage));
		if (modalStackCount > 0)
		{
			var index = modalStackCount - 1;
			currentPageRenderer = mainPage.Navigation.ModalStack[index];
		}
		else if (stackCount > 0)
		{
			var index = stackCount - 1;
			currentPageRenderer = mainPage.Navigation.NavigationStack[index];
		}
		else
		{
			currentPageRenderer = mainPage;
		}

		var viewController = (currentPageRenderer.Handler as PageHandler)?.ViewController;

		ViewController = viewController;
	}

	void SetView()
	{
		_ = View ?? throw new NullReferenceException($"{nameof(View)} cannot be null");
		_ = Control ?? throw new NullReferenceException($"{nameof(Control)} cannot be null");

		View.AddSubview(Control.ViewController?.View ?? throw new NullReferenceException());
		View.Bounds = new(0, 0, PreferredContentSize.Width, PreferredContentSize.Height);
		AddChildViewController(Control.ViewController);
	}

	void SetPresentationController()
	{
		var popOverDelegate = new PopoverDelegate();
		popOverDelegate.PopoverDismissed += HandlePopoverDelegateDismissed;

		((UIPopoverPresentationController)PresentationController).SourceView = ViewController?.View ?? throw new NullReferenceException();

		((UIPopoverPresentationController)PresentationController).Delegate = popOverDelegate;
	}

	void HandlePopoverDelegateDismissed(object? sender, UIPresentationController e)
	{
		_ = Element ?? throw new NullReferenceException($"{nameof(Element)} cannot be null.");

		if (Element.Handler is null)
		{
			return;
		}

		Element.Handler.Invoke(nameof(IBasePopup.LightDismiss));
	}

	void AddToCurrentPageViewController()
	{
		_ = ViewController ?? throw new NullReferenceException($"{nameof(ViewController)} cannot be null.");
		_ = Element ?? throw new NullReferenceException($"{nameof(Element)} cannot be null.");

		ViewController.PresentViewController(this, true, () => Element.Handler?.Invoke(nameof(IBasePopup.OnOpened)));
	}

	protected override void Dispose(bool disposing)
	{
		if (isDisposed)
		{
			return;
		}

		isDisposed = true;
		if (disposing)
		{
			if (Element != null)
			{
				Element = null;

				var presentationController = (UIPopoverPresentationController)PresentationController;
				if (presentationController != null)
				{
					presentationController.Delegate = null;
				}
			}
		}

		base.Dispose(disposing);
	}

	sealed class PopoverDelegate : UIPopoverPresentationControllerDelegate
	{
		readonly WeakEventManager popoverDismissedWeakEventManager = new();

		public event EventHandler<UIPresentationController> PopoverDismissed
		{
			add => popoverDismissedWeakEventManager.AddEventHandler(value);
			remove => popoverDismissedWeakEventManager.RemoveEventHandler(value);
		}

		public override UIModalPresentationStyle GetAdaptivePresentationStyle(UIPresentationController forPresentationController) =>
			UIModalPresentationStyle.None;

		public override void DidDismiss(UIPresentationController presentationController) =>
			popoverDismissedWeakEventManager.HandleEvent(this, presentationController, nameof(PopoverDismissed));
	}
}
