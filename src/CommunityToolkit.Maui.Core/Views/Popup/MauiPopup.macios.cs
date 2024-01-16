using System.Diagnostics.CodeAnalysis;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The native implementation of Popup control.
/// </summary>
public class MauiPopup : UIViewController
{
	readonly IMauiContext mauiContext;

	/// <summary>
	/// Constructor of <see cref="MauiPopup"/>.
	/// </summary>
	/// <param name="mauiContext">An instance of <see cref="IMauiContext"/>.</param>
	/// <exception cref="ArgumentNullException">If <paramref name="mauiContext"/> is null an exception will be thrown. </exception>
	public MauiPopup(IMauiContext mauiContext)
	{
		this.mauiContext = mauiContext ?? throw new ArgumentNullException(nameof(mauiContext));
	}

	/// <summary>
	/// An instance of the <see cref="PageHandler"/> that holds the <see cref="IPopup.Content"/>.
	/// </summary>
	public PageHandler? Control { get; private set; }

	/// <summary>
	/// An instance of the <see cref="IPopup"/>.
	/// </summary>
	public IPopup? VirtualView { get; private set; }

	internal UIViewController? ViewController { get; private set; }

	/// <summary>
	/// Method to update the Popup's size.
	/// </summary>
	/// <param name="size"></param>
	public void SetElementSize(Size size) =>
		Control?.ContainerView?.SizeThatFits(size);

	/// <inheritdoc/>
	public override void ViewDidLayoutSubviews()
	{
		base.ViewDidLayoutSubviews();

		_ = View ?? throw new InvalidOperationException($"{nameof(View)} cannot be null.");

		View.Superview.Layer.CornerRadius = 0.0f;
		View.Superview.Layer.MasksToBounds = false;
		PresentationController?.ContainerView?.Subviews.OfType<UIImageView>().FirstOrDefault()?.RemoveFromSuperview();

		SetElementSize(new Size(View.Bounds.Width, View.Bounds.Height));
	}

	/// <inheritdoc/>
	public override void ViewDidDisappear(bool animated)
	{
		if (ViewController?.View is UIView view)
		{
			var overlayView = GetOverlayView(view);
			overlayView.RemoveFromSuperview();
			overlayView.Dispose();
		}

		base.ViewDidDisappear(animated);
	}

	/// <inheritdoc/>
	public override void ViewWillTransitionToSize(CGSize toSize, IUIViewControllerTransitionCoordinator coordinator)
	{
		coordinator.AnimateAlongsideTransition(_ =>
		{
			// Before screen rotate
			if (ViewController?.View is UIView view)
			{
				var overlayView = GetOverlayView(view);
				overlayView.Frame = new CGRect(0, 0, view.Frame.Width, view.Frame.Height);
			}
		}, _ =>
		{
			// After screen rotate
			if (VirtualView is not null)
			{
				PopupExtensions.SetSize(this, VirtualView);
				PopupExtensions.SetLayout(this, VirtualView);
			}
		});
		base.ViewWillTransitionToSize(toSize, coordinator);
	}

	/// <summary>
	/// Method to initialize the native implementation.
	/// </summary>
	/// <param name="element">An instance of <see cref="IPopup"/>.</param>
	[MemberNotNull(nameof(VirtualView), nameof(ViewController))]
	public void SetElement(IPopup element)
	{
#if MACCATALYST
		if (element.Parent?.Handler is not PageHandler)
		{
			throw new InvalidOperationException($"The {nameof(element.Parent)} must be of type {typeof(PageHandler)}.");
		}
#endif

		VirtualView = element;
		ModalPresentationStyle = UIModalPresentationStyle.Popover;

		_ = View ?? throw new InvalidOperationException($"{nameof(View)} cannot be null.");
		_ = VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} cannot be null.");

#if MACCATALYST
		var pageHandler = VirtualView.Parent.Handler as PageHandler;
		var rootViewController = pageHandler?.ViewController ?? WindowStateManager.Default.GetCurrentUIViewController() ?? throw new InvalidOperationException($"{nameof(PageHandler.ViewController)} cannot be null.");
#else
		var rootViewController = WindowStateManager.Default.GetCurrentUIViewController() ?? throw new InvalidOperationException($"{nameof(PageHandler.ViewController)} cannot be null.");
#endif

		ViewController ??= rootViewController;
		SetDimmingBackgroundEffect();
	}

	void SetDimmingBackgroundEffect()
	{
		if (ViewController?.View is UIView view)
		{
			var overlayView = GetOverlayView(view);
			overlayView.Bounds = view.Bounds;
			overlayView.Layer.RemoveAllAnimations();
			overlayView.Frame = new CGRect(0, 0, view.Frame.Width, view.Frame.Height);
			overlayView.BackgroundColor = UIColor.Black.ColorWithAlpha(0.4f);
			view.AddSubview(overlayView);
		}
	}

	/// <summary>
	/// Method to CleanUp the resources of the <see cref="MauiPopup"/>.
	/// </summary>
	public void CleanUp()
	{
		if (VirtualView is null)
		{
			return;
		}

		VirtualView = null;

		if (PresentationController is UIPopoverPresentationController presentationController)
		{
			presentationController.Delegate = null;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="func"></param>
	/// <param name="virtualView"></param>
	/// <returns></returns>
	[MemberNotNull(nameof(Control), nameof(ViewController))]
	public void CreateControl(Func<IPopup, PageHandler> func, in IPopup virtualView)
	{
		Control = func(virtualView);

		SetPresentationController();

		_ = View ?? throw new InvalidOperationException($"{nameof(View)} cannot be null.");
		SetView(View, Control);

		_ = ViewController ?? throw new InvalidOperationException($"{nameof(ViewController)} cannot be null.");
		AddToCurrentPageViewController(ViewController);

		this.SetSize(virtualView);
		this.SetLayout(virtualView);
	}

	static UIView GetOverlayView(UIView view)
	{
		const int overlayViewTagNumber = 38483;

		var overlayViewTag = new IntPtr(overlayViewTagNumber);
		var overlayView = view.Subviews
								.AsEnumerable()
								.FirstOrDefault(x => x.Tag == overlayViewTag);

		if (overlayView is null)
		{
			overlayView = new UIView();
			overlayView.Tag = overlayViewTag;
		}

		return overlayView;
	}

	void SetView(UIView view, PageHandler control)
	{
		view.AddSubview(control.ViewController?.View ?? throw new InvalidOperationException($"{nameof(control.ViewController.View)} cannot be null."));
		view.Bounds = new(0, 0, PreferredContentSize.Width, PreferredContentSize.Height);
		AddChildViewController(control.ViewController);

		if (VirtualView is not null)
		{
			this.SetBackgroundColor(VirtualView);
		}
	}

	void SetPresentationController()
	{
		var popOverDelegate = new PopoverDelegate();
		popOverDelegate.PopoverDismissedEvent += HandlePopoverDelegateDismissed;

		UIPopoverPresentationController presentationController = (UIPopoverPresentationController)(PresentationController ?? throw new InvalidOperationException($"{nameof(PresentationController)} cannot be null."));
		presentationController.SourceView = ViewController?.View ?? throw new InvalidOperationException($"{nameof(ViewController.View)} cannot be null.");

		presentationController.Delegate = popOverDelegate;
	}

	[MemberNotNull(nameof(VirtualView))]
	void HandlePopoverDelegateDismissed(object? sender, UIPresentationController e)
	{
		_ = VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} cannot be null.");
		VirtualView.Handler?.Invoke(nameof(IPopup.OnDismissedByTappingOutsideOfPopup));
	}

	void AddToCurrentPageViewController(UIViewController viewController)
	{
		viewController.PresentViewController(this, true, null);
	}

	sealed class PopoverDelegate : UIPopoverPresentationControllerDelegate
	{
		readonly WeakEventManager popoverDismissedEventManager = new();

		public event EventHandler<UIPresentationController> PopoverDismissedEvent
		{
			add => popoverDismissedEventManager.AddEventHandler(value);
			remove => popoverDismissedEventManager.RemoveEventHandler(value);
		}

		public override UIModalPresentationStyle GetAdaptivePresentationStyle(UIPresentationController forPresentationController) =>
			UIModalPresentationStyle.None;

		public override void DidDismiss(UIPresentationController presentationController) =>
			popoverDismissedEventManager.HandleEvent(this, presentationController, nameof(PopoverDismissedEvent));
	}
}