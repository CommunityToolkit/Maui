using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using UIKit;

namespace CommunityToolkit.Core.Platform;

public class PopupRenderer : UIViewController
{
	bool isDisposed;
	readonly IMauiContext mauiContext;

	public PageHandler? Control { get; private set; }

	public IBasePopup? VirtualView { get; private set; }

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

		_ = VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} cannot be null");
		ModalInPopover = !VirtualView.IsLightDismissEnabled;
	}

	//public SizeRequest GetDesiredSize(double widthConstraint, double heightConstraint) =>
	//	NativeView.GetSizeRequest(widthConstraint, heightConstraint);

	public void SetElement(IBasePopup element)
	{
		VirtualView = element;
		ModalPresentationStyle = UIModalPresentationStyle.Popover;
		CreateControl();
		SetViewController();
		SetPresentationController();
		SetView();
		AddToCurrentPageViewController();
	}

	void CreateControl()
	{
		_ = VirtualView ?? throw new NullReferenceException($"{nameof(VirtualView)} cannot be null.");

		var view = (View?)VirtualView.Content;
		var contentPage = new ContentPage { Content = view, BackgroundColor = Colors.Orange };

		contentPage.Parent = Application.Current?.MainPage;
		//contentPage.SetBinding(VisualElement.BindingContextProperty, new Binding { Source = VirtualView, Path = VisualElement.BindingContextProperty.PropertyName });
		Control = (PageHandler)contentPage.ToHandler(mauiContext);
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
		_ = VirtualView ?? throw new NullReferenceException($"{nameof(VirtualView)} cannot be null.");

		if (VirtualView.Handler is null)
		{
			return;
		}

		VirtualView.Handler.Invoke(nameof(IBasePopup.LightDismiss));
	}

	void AddToCurrentPageViewController()
	{
		_ = ViewController ?? throw new NullReferenceException($"{nameof(ViewController)} cannot be null.");
		_ = VirtualView ?? throw new NullReferenceException($"{nameof(VirtualView)} cannot be null.");

		ViewController.PresentViewController(this, true, () => VirtualView.Handler?.Invoke(nameof(IBasePopup.OnOpened)));
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
			if (VirtualView != null)
			{
				VirtualView = null;

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
