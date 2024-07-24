namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// Popup for iOS + MacCatalyst
/// </summary>
public class Alert
{
	NSTimer? timer;

	/// <summary>
	/// Initialize Alert
	/// </summary>
	public Alert()
	{
		AlertView = [];

		AlertView.ParentView.AddSubview(AlertView);
		AlertView.ParentView.BringSubviewToFront(AlertView);
	}

	/// <summary>
	/// Duration of time before <see cref="Alert"/> disappears
	/// </summary>
	public TimeSpan Duration { get; set; }

	/// <summary>
	/// <see cref="UIView"/> on which Alert will appear. When null, <see cref="Alert"/> will appear at bottom of screen.
	/// </summary>
	public UIView? Anchor { get; set; }

	/// <summary>
	/// Action to execute on popup dismissed
	/// </summary>
	public Action? OnDismissed { get; set; }

	/// <summary>
	/// Action to execute on popup shown
	/// </summary>
	public Action? OnShown { get; set; }

	/// <summary>
	/// <see cref="UIView"/> for <see cref="Alert"/>
	/// </summary>
	protected AlertView AlertView { get; }

	/// <summary>
	/// Dismiss the <see cref="Alert"/> from the screen
	/// </summary>
	public void Dismiss()
	{
		if (timer != null)
		{
			timer.Invalidate();
			timer.Dispose();
			timer = null;
		}

		AlertView.Dismiss();
		OnDismissed?.Invoke();
	}

	/// <summary>
	/// Show the <see cref="Alert"/> on the screen
	/// </summary>
	public void Show()
	{
		AlertView.AnchorView = Anchor;

		AlertView.Setup();

		timer = NSTimer.CreateScheduledTimer(Duration, t =>
		{
			Dismiss();
		});

		OnShown?.Invoke();
	}
}