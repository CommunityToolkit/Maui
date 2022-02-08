using System;
using Android.App;
using Android.Content;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Platform;
using AView = Android.Views.View;

namespace CommunityToolkit.Core.Platform;

/// <summary>
/// The navite implementation of Popup control.
/// </summary>
public class MCTPopup : Dialog, IDialogInterfaceOnCancelListener
{
	AView? container;

	readonly IMauiContext mauiContext;

	/// <summary>
	/// An instace of the <see cref="IPopup"/>.
	/// </summary>
	public IPopup? VirtualView { get; private set; }

	/// <summary>
	/// Constructor of <see cref="MCTPopup"/>.
	/// </summary>
	/// <param name="context">An instance of <see cref="Context"/>.</param>
	/// <param name="mauiContext">An instace of <see cref="IMauiContext"/>.</param>
	/// <exception cref="ArgumentNullException">If <paramref name="mauiContext"/> is null an exception will be thrown. </exception>
	public MCTPopup(Context context, IMauiContext mauiContext)
		: base(context)
	{
		this.mauiContext = mauiContext ?? throw new ArgumentNullException(nameof(mauiContext));
	}

	/// <summary>
	/// Method to initialize the native implementation.
	/// </summary>
	/// <param name="element">An instance of <see cref="IPopup"/>.</param>
	public AView? SetElement(IPopup? element)
	{
		if (element is null)
		{
			throw new ArgumentNullException(nameof(element));
		}

		VirtualView = element;
		CreateControl(VirtualView);
		SetEvents();
		return container;
	}

	/// <summary>
	/// Method to show the Popup.
	/// </summary>
	public override void Show()
	{
		base.Show();
		VirtualView?.OnOpened();
	}

	void CreateControl(in IPopup basePopup)
	{
		if (basePopup.Content != null)
		{
			container = basePopup.Content.ToNative(mauiContext);
			SetContentView(container);
		}
	}

	void SetEvents()
	{
		SetOnCancelListener(this);
	}

	/// <summary>
	/// Method triggered when the Popup is LightDismissed.
	/// </summary>
	/// <param name="dialog">An instance of the <see cref="IDialogInterface"/>.</param>
	public void OnCancel(IDialogInterface? dialog)
	{
		VirtualView?.Handler?.Invoke(nameof(IPopup.LightDismiss));
	}

	/// <summary>
	/// Method to CleanUp the resources of the <see cref="MCTPopup"/>.
	/// </summary>
	public void ClenaUp()
	{
		VirtualView = null;
	}
}
