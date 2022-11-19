using Microsoft.Maui.Platform;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using NLinearLayout = Tizen.NUI.LinearLayout;
using NVerticalAlignment = Tizen.NUI.VerticalAlignment;
using NView = Tizen.NUI.BaseComponents.View;
using TButton = Tizen.UIExtensions.NUI.Button;
using TLabel = Tizen.UIExtensions.NUI.Label;
using TPopup = Tizen.UIExtensions.NUI.Popup;

namespace CommunityToolkit.Maui.Alerts;

public partial class Snackbar
{
	static TPopup? PopupStatic { get; set; }

	TPopup? PopupInstance { get; set; }

	/// <summary>
	/// Dispose Snackbar
	/// </summary>
	protected virtual void Dispose(bool isDisposing)
	{
		if (isDisposed)
		{
			return;
		}

		if (isDisposing)
		{
			PopupInstance?.Close();
			PopupInstance?.Dispose();
			PopupInstance = null;
		}
		isDisposed = true;
	}

	/// <inheritdoc/>
	Task ShowPlatform(CancellationToken token)
	{
		var dispatcher = Dispatcher.GetForCurrentThread() ?? Application.Current?.Dispatcher ?? throw new InvalidOperationException($"There is no IDispatcher object, application is not initalized");

		// close and cleanup the previously opened snackbar
		if (PopupStatic is not null && PopupStatic.IsOpen)
		{
			PopupStatic.Close();
			PopupStatic.Dispose();
			PopupStatic = null;
		}

		var popup = new OutsidePassThroughPopup
		{
			Layout = new NLinearLayout
			{
				LinearOrientation = NLinearLayout.Orientation.Vertical,
				VerticalAlignment = NVerticalAlignment.Bottom,
			}
		};

		var content = new NView
		{
			Margin = new Extents((ushort)10d.ToScaledPixel()),
			WidthSpecification = LayoutParamPolicies.MatchParent,
			HeightSpecification = LayoutParamPolicies.WrapContent,

			BackgroundColor = VisualOptions.BackgroundColor.ToNUIColor(),
			CornerRadius = new Vector4(10, 10, 10, 10),
			Layout = new NLinearLayout
			{
				LinearOrientation = NLinearLayout.Orientation.Horizontal,
				VerticalAlignment = NVerticalAlignment.Center,
			}
		};

		var margin = (ushort)10d.ToScaledPixel();

		var message = new TLabel()
		{
			MultiLine = true,
			Margin = margin,
			WidthSpecification = LayoutParamPolicies.MatchParent,
			Text = Text,
			TextColor = VisualOptions.TextColor.ToPlatform(),
			PixelSize = VisualOptions.Font.Size.ToPixel(),
			FontAttributes = LabelExtensions.GetFontAttributes(VisualOptions.Font),
		};
		content.Add(message);

		var actionButton = new TButton()
		{
			Margin = margin,
			WidthSpecification = LayoutParamPolicies.WrapContent,
			BackgroundColor = Tizen.NUI.Color.Transparent,
			TextColor = VisualOptions.ActionButtonTextColor.ToPlatform(),
			Text = ActionButtonText,
		};
		actionButton.TextLabel.PixelSize = 15d.ToPixel();
		actionButton.SizeWidth = actionButton.TextLabel.NaturalSize.Width + 15d.ToPixel() * 2;
		actionButton.Clicked += (s, e) =>
		{
			if (Action is not null)
			{
				Action.Invoke();
			}
			else
			{
				popup.Close();
			}
		};

		content.Add(actionButton);
		popup.Content = content;

		if (Anchor is not null)
		{
			var anchorPlatformView = Anchor.ToPlatform();

			// can't measure height of content on NUI
			var maximumHeight = 100d.ToScaledPixel();
			popup.SizeHeight = maximumHeight;

			if (maximumHeight < anchorPlatformView.ScreenPosition.Y)
			{
				// display top outside of anchor
				popup.Position = new Position(0, anchorPlatformView.ScreenPosition.Y - popup.SizeHeight);
			}
			else
			{
				// display bottom innter of anchor
				popup.Position = new Position(0, anchorPlatformView.ScreenPosition.Y - (maximumHeight - anchorPlatformView.SizeHeight));
			}
		}
		else
		{
			popup.Position = new Position(0, 0);
			popup.WidthSpecification = LayoutParamPolicies.MatchParent;
			popup.HeightSpecification = LayoutParamPolicies.MatchParent;
		}

		popup.Closed += (s, e) => OnDismissed();
		popup.Open();

		var timer = dispatcher.CreateTimer();
		timer.IsRepeating = false;
		timer.Interval = Duration;
		timer.Tick += (s, e) => popup.Close();
		timer.Start();

		PopupStatic = popup;
		PopupInstance = popup;

		OnShown();

		return Task.CompletedTask;
	}

	/// <inheritdoc/>
	Task DismissPlatform(CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		if (PopupInstance is null)
		{
			return Task.CompletedTask;
		}

		PopupInstance.Close();
		PopupInstance.Dispose();
		PopupInstance = null;

		return Task.CompletedTask;
	}

	class OutsidePassThroughPopup : TPopup
	{
		protected override bool HitTest(Touch touch)
		{
			return false;
		}
	}
}