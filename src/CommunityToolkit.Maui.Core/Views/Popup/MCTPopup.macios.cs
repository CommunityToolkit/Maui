using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using UIKit;

namespace CommunityToolkit.Core.Platform;

/// <summary>
/// The navite implementation of Popup control.
/// </summary>
public class MCTPopup : UIViewController
{
	readonly IMauiContext mauiContext;

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
	/// Constructor of <see cref="MCTPopup"/>.
	/// </summary>
	/// <param name="mauiContext">An instace of <see cref="IMauiContext"/>.</param>
	/// <exception cref="ArgumentNullException">If <paramref name="mauiContext"/> is null an exception will be thrown. </exception>
	public MCTPopup(IMauiContext mauiContext)
	{
		this.mauiContext = mauiContext ?? throw new ArgumentNullException(nameof(mauiContext));
	}

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
		contentPage.SetBinding(VisualElement.BindingContextProperty, new Binding { Source = VirtualView, Path = VisualElement.BindingContextProperty.PropertyName });
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

		ViewController ??= currentPageRenderer?.ToUIViewController(mauiContext);
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

		VirtualView.Handler?.Invoke(nameof(IPopup.LightDismiss));
	}

	void AddToCurrentPageViewController()
	{
		_ = ViewController ?? throw new NullReferenceException($"{nameof(ViewController)} cannot be null.");
		_ = VirtualView ?? throw new NullReferenceException($"{nameof(VirtualView)} cannot be null.");

		ViewController.PresentViewController(this, true, () => VirtualView.Handler?.Invoke(nameof(IPopup.OnOpened)));
	}

	/// <summary>
	/// Method to CleanUp the resources of the <see cref="MCTPopup"/>.
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
