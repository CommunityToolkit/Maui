using Android.OS;
using Android.Views;
using AndroidX.Fragment.App;
using Microsoft.Maui.Platform;
using AView = Android.Views.View;
using AWindow = Android.Views.Window;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// A dialog fragment with a content view, like the toolkit's popups: <c>AndroidWindowExtensions.GetCurrentWindow</c> only returns the
/// window of a dialog fragment that is added, showing and visible, and a fragment without a view is never visible.
/// Every member must be called on the UI thread.
/// </summary>
public sealed class TestDialogFragment : DialogFragment
{
	AView? embeddedContainer;

	/// <summary>The flags of the dialog's window when <see cref="Show"/> returned, before a test changed them (none if the dialog had no window yet).</summary>
	public WindowManagerFlags FlagsWhenShown { get; private set; }

	public override AView? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState) =>
		new Android.Widget.TextView(RequireContext()) { Text = nameof(TestDialogFragment) };

	/// <summary>Adds the fragment to the current activity as a dialog. The dialog is created when the transaction runs.</summary>
	public static TestDialogFragment Show(string tag)
	{
		var fragment = new TestDialogFragment();
		fragment.Show(ActivityFragmentManager(), tag);
		ActivityFragmentManager().ExecutePendingTransactions();
		fragment.FlagsWhenShown = fragment.Dialog?.Window is AWindow window ? AndroidSystemBars.Flags(window) : 0;
		return fragment;
	}

	/// <summary>
	/// Adds the fragment to the activity's content view as an ordinary fragment (a dialog fragment that is added to a container
	/// does not create a dialog): the fragment is visible, its <c>Dialog</c> is <see langword="null"/>.
	/// </summary>
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

	/// <summary>The window of the fragment's dialog.</summary>
	public AWindow DialogWindow => Dialog?.Window ?? throw new InvalidOperationException("The dialog fragment has no dialog window yet.");

	/// <summary>True once the fragment passes the test that GetCurrentWindow applies: its dialog is showing and its view is attached to a window.</summary>
	public bool IsShowingAndVisible => Dialog is { IsShowing: true, Window: not null } && IsVisible;

	/// <summary>Hides the dialog window itself (Dialog.Hide): <c>IsShowing</c> turns false while the fragment stays visible.</summary>
	public void HideDialog() => Dialog?.Hide();

	/// <summary>Hides the fragment with a transaction: <c>IsVisible</c> turns false while the dialog keeps showing.</summary>
	public void HideFragment()
	{
		ActivityFragmentManager().BeginTransaction().Hide(this)?.Commit();
		ActivityFragmentManager().ExecutePendingTransactions();
	}

	/// <summary>Removes the fragment (and the container of an embedded one) again.</summary>
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

/// <summary>A fragment without a view: it is in the fragment manager's list but is not a dialog fragment.</summary>
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