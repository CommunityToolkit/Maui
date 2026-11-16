using Android.OS;
using Android.Views;
using AndroidX.Fragment.App;
using Microsoft.Maui.Platform;
using AView = Android.Views.View;
using AWindow = Android.Views.Window;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// <c>AndroidWindowExtensions.GetCurrentWindow</c> only returns the window of a dialog fragment that is added, showing and visible, and a
/// fragment without a view is never visible, so this one has a content view, like the toolkit's popups. Every member must be called on the
/// UI thread.
/// </summary>
public sealed class TestDialogFragment : DialogFragment
{
	AView? embeddedContainer;

	public WindowManagerFlags FlagsWhenShown { get; private set; }

	public override AView? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState) =>
		new Android.Widget.TextView(RequireContext()) { Text = nameof(TestDialogFragment) };

	public static TestDialogFragment Show(string tag)
	{
		var fragment = new TestDialogFragment();
		fragment.Show(ActivityFragmentManager(), tag);
		ActivityFragmentManager().ExecutePendingTransactions();
		fragment.FlagsWhenShown = fragment.Dialog?.Window is AWindow window ? AndroidSystemBars.Flags(window) : 0;
		return fragment;
	}

	// A dialog fragment that is added to a container does not create a dialog: the fragment is visible, its Dialog is null.
	public static TestDialogFragment Embed(string tag)
	{
		var activity = AndroidSystemBars.Activity;
		var content = activity.FindViewById<Android.Widget.FrameLayout>(Android.Resource.Id.Content) ?? throw new InvalidOperationException("The activity has no content view.");
		var container = new Android.Widget.FrameLayout(activity) { Id = AView.GenerateViewId() };
		content.AddView(container);

		var fragment = new TestDialogFragment { embeddedContainer = container };
		ActivityFragmentManager().BeginTransaction().Add(container.Id, fragment, tag)?.Commit();
		ActivityFragmentManager().ExecutePendingTransactions();
		return fragment;
	}

	public AWindow DialogWindow => Dialog?.Window ?? throw new InvalidOperationException("The dialog fragment has no dialog window yet.");

	// The test that GetCurrentWindow applies to a dialog fragment.
	public bool IsShowingAndVisible => Dialog is { IsShowing: true, Window: not null } && IsVisible;

	public void HideDialog() => Dialog?.Hide();

	public void HideFragment()
	{
		ActivityFragmentManager().BeginTransaction().Hide(this)?.Commit();
		ActivityFragmentManager().ExecutePendingTransactions();
	}

	public void Remove()
	{
		if (IsAdded)
		{
			DismissAllowingStateLoss();
			ActivityFragmentManager().ExecutePendingTransactions();
		}

		(embeddedContainer?.Parent as ViewGroup)?.RemoveView(embeddedContainer);
		embeddedContainer = null;
	}

	static AndroidX.Fragment.App.FragmentManager ActivityFragmentManager() =>
		AndroidSystemBars.Activity.GetFragmentManager() ?? throw new InvalidOperationException("The current activity has no fragment manager.");
}

public sealed class TestPlainFragment : AndroidX.Fragment.App.Fragment
{
	public static TestPlainFragment Add(string tag)
	{
		var fragmentManager = AndroidSystemBars.Activity.GetFragmentManager() ?? throw new InvalidOperationException("The current activity has no fragment manager.");
		var fragment = new TestPlainFragment();
		fragmentManager.BeginTransaction().Add(fragment, tag)?.Commit();
		fragmentManager.ExecutePendingTransactions();
		return fragment;
	}

	public void Remove()
	{
		var fragmentManager = AndroidSystemBars.Activity.GetFragmentManager() ?? throw new InvalidOperationException("The current activity has no fragment manager.");
		fragmentManager.BeginTransaction().Remove(this)?.Commit();
		fragmentManager.ExecutePendingTransactions();
	}
}