using Android.Content;
using Android.Views;

namespace CommunityToolkit.Maui.Core;

public interface IDialogFragmentService
{
	void OnFragmentAttached(AndroidX.Fragment.App.FragmentManager fm, AndroidX.Fragment.App.Fragment f, Context context);
	void OnFragmentCreated(AndroidX.Fragment.App.FragmentManager fm, AndroidX.Fragment.App.Fragment f, Bundle? savedInstanceState);
	void OnFragmentDestroyed(AndroidX.Fragment.App.FragmentManager fm, AndroidX.Fragment.App.Fragment f);
	void OnFragmentDetached(AndroidX.Fragment.App.FragmentManager fm, AndroidX.Fragment.App.Fragment f);
	void OnFragmentPaused(AndroidX.Fragment.App.FragmentManager fm, AndroidX.Fragment.App.Fragment f);
	void OnFragmentPreAttached(AndroidX.Fragment.App.FragmentManager fm, AndroidX.Fragment.App.Fragment f, Context context);
	void OnFragmentPreCreated(AndroidX.Fragment.App.FragmentManager fm, AndroidX.Fragment.App.Fragment f, Bundle? savedInstanceState);
	void OnFragmentResumed(AndroidX.Fragment.App.FragmentManager fm, AndroidX.Fragment.App.Fragment f);
	void OnFragmentSaveInstanceState(AndroidX.Fragment.App.FragmentManager fm, AndroidX.Fragment.App.Fragment f, Bundle outState);
	void OnFragmentStarted(AndroidX.Fragment.App.FragmentManager fm, AndroidX.Fragment.App.Fragment f);
	void OnFragmentStopped(AndroidX.Fragment.App.FragmentManager fm, AndroidX.Fragment.App.Fragment f);
	void OnFragmentViewCreated(AndroidX.Fragment.App.FragmentManager fm, AndroidX.Fragment.App.Fragment f, View v, Bundle? savedInstanceState);
	void OnFragmentViewDestroyed(AndroidX.Fragment.App.FragmentManager fm, AndroidX.Fragment.App.Fragment f);
}