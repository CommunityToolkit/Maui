using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using UIKit;

namespace CommunityToolkit.Core.Views;

/// <summary>
/// The navite implementation of Popup control.
/// </summary>
public class MauiPopup : UIViewController
{
	readonly IMauiContext mauiContext;

	/// <summary>
	/// Constructor of <see cref="MauiPopup"/>.
	/// </summary>
	/// <param name="mauiContext">An instace of <see cref="IMauiContext"/>.</param>
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
	/// An instace of the <see cref="IPopup"/>.
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

		_ = View ?? throw new InvalidOperationException($"{nameof(View)} cannot be null");
		SetElementSize(new Size(View.Bounds.Width, View.Bounds.Height));
	}

	/// <summary>
	/// Method to initialize the native implementation.
	/// </summary>
	/// <param name="element">An instance of <see cref="IPopup"/>.</param>
	public void SetElement(IPopup element)
	{
		var mainPage = Application.Current?.MainPage ?? throw new InvalidOperationException($"{nameof(Application.Current.MainPage)} cannot be null");

		VirtualView = element;
		ModalPresentationStyle = UIModalPresentationStyle.Popover;

		_ = View ?? throw new InvalidOperationException($"{nameof(View)} cannot be null");
		_ = VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} cannot be null.");

		Control = CreateControl(VirtualView);
		ViewController ??= CreateViewController(mainPage);

		SetPresentationController();
		SetView(View, Control);
		AddToCurrentPageViewController(ViewController, VirtualView);
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

		var presentationController = (UIPopoverPresentationController)PresentationController;
		if (presentationController is not null)
		{
			presentationController.Delegate = null;
		}
	}

	PageHandler CreateControl(in IPopup virtualView)
	{
		var view = (View?)virtualView.Content;
		var contentPage = new ContentPage { Content = view, BackgroundColor = Colors.Orange };

		contentPage.Parent = Application.Current?.MainPage;
		contentPage.SetBinding(VisualElement.BindingContextProperty, new Binding { Source = virtualView, Path = VisualElement.BindingContextProperty.PropertyName });

		return (PageHandler)contentPage.ToHandler(mauiContext);
	}

	UIViewController CreateViewController(Page mainPage)
	{
		Page currentPageRenderer;

		var modalStackCount = mainPage.Navigation.ModalStack.Count;
		var stackCount = mainPage.Navigation.NavigationStack.Count;

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

		return currentPageRenderer.ToUIViewController(mauiContext);
	}

	void SetView(UIView view, PageHandler control)
	{
		view.AddSubview(control.ViewController?.View ?? throw new InvalidOperationException($"{nameof(control.ViewController.View)} cannot be null"));
		view.Bounds = new(0, 0, PreferredContentSize.Width, PreferredContentSize.Height);
		AddChildViewController(control.ViewController);
	}

	void SetPresentationController()
	{
		var popOverDelegate = new PopoverDelegate();
		popOverDelegate.PopoverDismissed += HandlePopoverDelegateDismissed;

		((UIPopoverPresentationController)PresentationController).SourceView = ViewController?.View ?? throw new InvalidOperationException($"{nameof(ViewController.View)} cannot be null");

		((UIPopoverPresentationController)PresentationController).Delegate = popOverDelegate;
	}

	void HandlePopoverDelegateDismissed(object? sender, UIPresentationController e)
	{
		_ = VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} cannot be null.");

		VirtualView.Handler?.Invoke(nameof(IPopup.LightDismiss));
	}

	void AddToCurrentPageViewController(UIViewController viewController, IPopup virtualView)
	{
		viewController.PresentViewController(this, true, () => virtualView.Handler?.Invoke(nameof(IPopup.OnOpened)));
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
