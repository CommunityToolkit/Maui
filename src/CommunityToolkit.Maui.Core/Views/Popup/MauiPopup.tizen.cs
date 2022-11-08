using Microsoft.Maui.Platform;
using Microsoft.Maui.Primitives;
using Tizen.NUI;
using Tizen.UIExtensions.NUI;
using NHorizontalAlignment = Tizen.NUI.HorizontalAlignment;
using NVerticalAlignment = Tizen.NUI.VerticalAlignment;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The native implementation of Popup control.
/// </summary>
public class MauiPopup : Popup
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
		OutsideClicked += OnOutsideClicked;
	}

	/// <summary>
	/// An instance of the <see cref="IPopup"/>.
	/// </summary>
	public IPopup? VirtualView { get; private set; }

	/// <inheritdoc/>
	protected override void Dispose(bool isDisposing)
	{
		if (isDisposing)
		{
			OutsideClicked -= OnOutsideClicked;
		}

		base.Dispose(isDisposing);
	}

	/// <summary>
	/// Method to initialize the native implementation.
	/// </summary>
	/// <param name="element">An instance of <see cref="IPopup"/>.</param>
	public void SetElement(IPopup? element)
	{
		VirtualView = element;
	}

	/// <summary>
	/// Method to show the Popup
	/// </summary>
	public void ShowPopup()
	{
		_ = VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} cannot be null");
		Content = VirtualView.Content?.ToPlatform(mauiContext) ?? throw new InvalidOperationException($"{nameof(VirtualView.Content)} cannot be null");

		BackgroundColor = new Tizen.NUI.Color(0.1f, 0.1f, 0.1f, 0.5f);
		Content.BackgroundColor = (VirtualView.Color ?? Colors.Transparent).ToNUIColor();

		if (VirtualView.Anchor is not null)
		{
			var anchorView = VirtualView.Anchor.ToPlatform();
			var anchorPosition = anchorView.ScreenPosition;
			Layout = new AbsoluteLayout();
			Content.UpdatePosition(new Tizen.UIExtensions.Common.Point(anchorPosition.X, anchorPosition.Y));
		}
		else
		{
			Layout = new LinearLayout
			{
				LinearOrientation = LinearLayout.Orientation.Vertical,
				VerticalAlignment = ToVerticalAlignment(VirtualView.VerticalOptions),
				HorizontalAlignment = ToHorizontalAlignment(VirtualView.HorizontalOptions),
			};
			Content.UpdatePosition(new Tizen.UIExtensions.Common.Point(0, 0));
		}

		UpdateContentSize();

		Open();
		VirtualView.OnOpened();
	}

	/// <summary>
	/// Method to update size of Content
	/// </summary>
	/// <exception cref="InvalidOperationException"></exception>
	public void UpdateContentSize()
	{
		_ = VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} cannot be null");
		if (Content is null)
		{
			return;
		}

		if (VirtualView.Size.Width > 0 && VirtualView.Size.Height > 0)
		{
			Content.UpdateSize(VirtualView.Size.ToPixel());
		}
		else
		{
			var measured = VirtualView.Content?.Measure(double.PositiveInfinity, double.PositiveInfinity).ToPixel() ?? new Tizen.UIExtensions.Common.Size(0, 0);
			Content.UpdateSize(measured);
		}
	}

	static NVerticalAlignment ToVerticalAlignment(LayoutAlignment align) => align switch
	{
		LayoutAlignment.Start => NVerticalAlignment.Top,
		LayoutAlignment.End => NVerticalAlignment.Bottom,
		_ => NVerticalAlignment.Center
	};

	static NHorizontalAlignment ToHorizontalAlignment(LayoutAlignment align) => align switch
	{
		LayoutAlignment.Start => NHorizontalAlignment.Begin,
		LayoutAlignment.End => NHorizontalAlignment.End,
		_ => NHorizontalAlignment.Center
	};

	void OnOutsideClicked(object? sender, EventArgs e)
	{
		if (VirtualView?.Handler is null)
		{
			return;
		}

		if (VirtualView.CanBeDismissedByTappingOutsideOfPopup)
		{
			Close();
			VirtualView.Handler.Invoke(nameof(IPopup.OnDismissedByTappingOutsideOfPopup));
		}
	}
}