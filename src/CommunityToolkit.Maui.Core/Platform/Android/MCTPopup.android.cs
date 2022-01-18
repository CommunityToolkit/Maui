using System;
using Android.App;
using Android.Content;
using CommunityToolkit.Maui.Core;
//using CommunityToolkit.Maui.Extensions.Workarounds;
using Microsoft.Maui.Platform;
using AView = Android.Views.View;

namespace CommunityToolkit.Core.Platform;

public class MCTPopup : Dialog, IDialogInterfaceOnCancelListener
{
	AView? container;
	bool isDisposed;

	readonly IMauiContext mauiContext;

	public IBasePopup? VirtualView { get; private set; }

	public MCTPopup(Context context, IMauiContext mauiContext)
		: base(context)
	{
		this.mauiContext = mauiContext ?? throw new ArgumentNullException(nameof(mauiContext));
	}

	public AView? SetElement(IBasePopup? element)
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

	public override void Show()
	{
		base.Show();
		VirtualView?.OnOpened();
	}

	public void CreateControl(in IBasePopup basePopup)
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

	public void OnCancel(IDialogInterface? dialog)
	{
		VirtualView?.Handler?.Invoke(nameof(IBasePopup.LightDismiss));
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
			VirtualView = null;
		}

		base.Dispose(disposing);
	}
}
