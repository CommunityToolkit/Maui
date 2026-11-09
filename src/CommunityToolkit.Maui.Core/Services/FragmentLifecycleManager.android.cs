using System.ComponentModel;
using Android.Content;
using FragmentManager = AndroidX.Fragment.App.FragmentManager;

namespace CommunityToolkit.Maui.Core.Services;

public sealed class FragmentLifecycleManager : FragmentManager.FragmentLifecycleCallbacks
{
	readonly IDialogFragmentService dialogFragmentService;

	/// <summary>
	/// Initializes a new instance of <see cref="FragmentLifecycleManager"/>.
	/// </summary>
	/// <param name="dialogFragmentService">The <see cref="IDialogFragmentService"/> that receives the fragment lifecycle callbacks.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="dialogFragmentService"/> is null.</exception>
	public FragmentLifecycleManager(IDialogFragmentService dialogFragmentService)
	{
		ArgumentNullException.ThrowIfNull(dialogFragmentService);
		this.dialogFragmentService = dialogFragmentService;
	}

	public override void OnFragmentAttached(FragmentManager fm, AndroidX.Fragment.App.Fragment f, Context context)
	{
		base.OnFragmentAttached(fm, f, context);
		dialogFragmentService.OnFragmentAttached(fm, f, context);
	}

	public override void OnFragmentCreated(FragmentManager fm, AndroidX.Fragment.App.Fragment f, Bundle? savedInstanceState)
	{
		base.OnFragmentCreated(fm, f, savedInstanceState);
		dialogFragmentService.OnFragmentCreated(fm, f, savedInstanceState);
	}

	public override void OnFragmentDestroyed(FragmentManager fm, AndroidX.Fragment.App.Fragment f)
	{
		base.OnFragmentDestroyed(fm, f);
		dialogFragmentService.OnFragmentDestroyed(fm, f);
	}

	public override void OnFragmentDetached(FragmentManager fm, AndroidX.Fragment.App.Fragment f)
	{
		base.OnFragmentDetached(fm, f);
		dialogFragmentService.OnFragmentDetached(fm, f);
	}

	public override void OnFragmentPaused(FragmentManager fm, AndroidX.Fragment.App.Fragment f)
	{
		base.OnFragmentPaused(fm, f);
		dialogFragmentService.OnFragmentPaused(fm, f);
	}

	public override void OnFragmentPreAttached(FragmentManager fm, AndroidX.Fragment.App.Fragment f, Context context)
	{
		base.OnFragmentPreAttached(fm, f, context);
		dialogFragmentService.OnFragmentPreAttached(fm, f, context);
	}

	public override void OnFragmentPreCreated(FragmentManager fm, AndroidX.Fragment.App.Fragment f, Bundle? savedInstanceState)
	{
		base.OnFragmentPreCreated(fm, f, savedInstanceState);
		dialogFragmentService.OnFragmentPreCreated(fm, f, savedInstanceState);
	}

	public override void OnFragmentResumed(FragmentManager fm, AndroidX.Fragment.App.Fragment f)
	{
		base.OnFragmentResumed(fm, f);
		dialogFragmentService.OnFragmentResumed(fm, f);
	}

	public override void OnFragmentSaveInstanceState(FragmentManager fm, AndroidX.Fragment.App.Fragment f, Bundle outState)
	{
		base.OnFragmentSaveInstanceState(fm, f, outState);
		dialogFragmentService.OnFragmentSaveInstanceState(fm, f, outState);
	}

	public override void OnFragmentStarted(FragmentManager fm, AndroidX.Fragment.App.Fragment f)
	{
		base.OnFragmentStarted(fm, f);
		dialogFragmentService.OnFragmentStarted(fm, f);
	}

	public override void OnFragmentStopped(FragmentManager fm, AndroidX.Fragment.App.Fragment f)
	{
		base.OnFragmentStopped(fm, f);
		dialogFragmentService.OnFragmentStopped(fm, f);
	}

	public override void OnFragmentViewCreated(FragmentManager fm, AndroidX.Fragment.App.Fragment f, Android.Views.View v, Bundle? savedInstanceState)
	{
		base.OnFragmentViewCreated(fm, f, v, savedInstanceState);
		dialogFragmentService.OnFragmentViewCreated(fm, f, v, savedInstanceState);
	}

	public override void OnFragmentViewDestroyed(FragmentManager fm, AndroidX.Fragment.App.Fragment f)
	{
		base.OnFragmentViewDestroyed(fm, f);
		dialogFragmentService.OnFragmentViewDestroyed(fm, f);
	}
}