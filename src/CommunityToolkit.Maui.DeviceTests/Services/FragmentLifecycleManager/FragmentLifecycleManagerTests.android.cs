#pragma warning disable CA1416, CA1422, CS0618
using Android.OS;
using AndroidX.AppCompat.App;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Services;
using AFragment = AndroidX.Fragment.App.Fragment;
using AFragmentManager = AndroidX.Fragment.App.FragmentManager;
using AView = Android.Views.View;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.FragmentLifecycleManager)]
[Collection(TestCollections.DisableParallelization)]
public class FragmentLifecycleManagerTests : FragmentTestBase
{
	static readonly string[] lifecycleCallbackNames =
	[
		nameof(FragmentLifecycleManager.OnFragmentAttached),
		nameof(FragmentLifecycleManager.OnFragmentCreated),
		nameof(FragmentLifecycleManager.OnFragmentDestroyed),
		nameof(FragmentLifecycleManager.OnFragmentDetached),
		nameof(FragmentLifecycleManager.OnFragmentPaused),
		nameof(FragmentLifecycleManager.OnFragmentPreAttached),
		nameof(FragmentLifecycleManager.OnFragmentPreCreated),
		nameof(FragmentLifecycleManager.OnFragmentResumed),
		nameof(FragmentLifecycleManager.OnFragmentSaveInstanceState),
		nameof(FragmentLifecycleManager.OnFragmentStarted),
		nameof(FragmentLifecycleManager.OnFragmentStopped),
		nameof(FragmentLifecycleManager.OnFragmentViewCreated),
		nameof(FragmentLifecycleManager.OnFragmentViewDestroyed),
	];

	public static TheoryData<string> LifecycleCallbacks => [.. lifecycleCallbackNames];

	[Theory]
	[MemberData(nameof(LifecycleCallbacks))]
	public async Task CallbackForwardsEveryArgumentToTheService(string callback)
	{
		var service = new RecordingDialogFragmentService();
		var manager = new FragmentLifecycleManager(service);

		await InvokeOnMainThreadAsync(() =>
		{
			var activity = HostActivity;
			var fragmentManager = activity.SupportFragmentManager;
			var fragment = new PlainFragment();
			var view = new AView(activity);
			var bundle = new Bundle();

			var expectedArguments = InvokeCallbackAndGetExpectedArguments(manager, callback, fragmentManager, fragment, activity, view, bundle);

			var call = Assert.Single(service.Calls);
			Assert.Equal(callback, call.Name);
			Assert.Equal(expectedArguments.Length, call.Arguments.Length);
			for (var i = 0; i < expectedArguments.Length; i++)
			{
				Assert.Same(expectedArguments[i], call.Arguments[i]);
			}
		});
	}

	[Theory]
	[InlineData(nameof(FragmentLifecycleManager.OnFragmentCreated))]
	[InlineData(nameof(FragmentLifecycleManager.OnFragmentPreCreated))]
	[InlineData(nameof(FragmentLifecycleManager.OnFragmentViewCreated))]
	public async Task CallbackForwardsANullSavedInstanceState(string callback)
	{
		var service = new RecordingDialogFragmentService();
		var manager = new FragmentLifecycleManager(service);

		await InvokeOnMainThreadAsync(() =>
		{
			var activity = HostActivity;
			var fragmentManager = activity.SupportFragmentManager;
			var fragment = new PlainFragment();
			var view = new AView(activity);

			switch (callback)
			{
				case nameof(FragmentLifecycleManager.OnFragmentCreated):
					manager.OnFragmentCreated(fragmentManager, fragment, null);
					break;
				case nameof(FragmentLifecycleManager.OnFragmentPreCreated):
					manager.OnFragmentPreCreated(fragmentManager, fragment, null);
					break;
				default:
					manager.OnFragmentViewCreated(fragmentManager, fragment, view, null);
					break;
			}

			var call = Assert.Single(service.Calls);
			Assert.Equal(callback, call.Name);
			Assert.Null(call.Arguments[^1]);
		});
	}

	[Theory]
	[MemberData(nameof(LifecycleCallbacks))]
	public async Task CallbackDoesNotSwallowExceptionsOfTheService(string callback)
	{
		var manager = new FragmentLifecycleManager(new ThrowingDialogFragmentService());

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => InvokeOnMainThreadAsync(() =>
		{
			var activity = HostActivity;
			InvokeCallbackAndGetExpectedArguments(manager, callback, activity.SupportFragmentManager, new PlainFragment(), activity, new AView(activity), new Bundle());
		}));

		Assert.Equal(callback, exception.Message);
	}

	[Fact]
	public async Task RegisteredManagerReceivesTheWholeFragmentLifecycleInOrder()
	{
		var service = new RecordingDialogFragmentService();
		var manager = new FragmentLifecycleManager(service);
		var host = new PlainFragment();
		var fragment = new PlainFragment();
		AFragmentManager? childManager = null;

		try
		{
			await InvokeOnMainThreadAsync(() =>
			{
				// A child FragmentManager of a throwaway fragment, so nothing is registered on the runner activity's own FragmentManager.
				HostActivity.SupportFragmentManager.BeginTransaction().Add(host, $"test-host-{Guid.NewGuid():N}").CommitNow();
				childManager = host.ChildFragmentManager;
				childManager.RegisterFragmentLifecycleCallbacks(manager, false);

				childManager.BeginTransaction().Add(fragment, "test-fragment").CommitNow();
				childManager.SaveFragmentInstanceState(fragment);
				childManager.BeginTransaction().Remove(fragment).CommitNow();
			});
		}
		finally
		{
			await InvokeOnMainThreadAsync(() =>
			{
				childManager?.UnregisterFragmentLifecycleCallbacks(manager);
				HostActivity.SupportFragmentManager.BeginTransaction().Remove(host).CommitNow();
			});
		}

		string[] expected =
		[
			nameof(IDialogFragmentService.OnFragmentPreAttached),
			nameof(IDialogFragmentService.OnFragmentAttached),
			nameof(IDialogFragmentService.OnFragmentPreCreated),
			nameof(IDialogFragmentService.OnFragmentCreated),
			nameof(IDialogFragmentService.OnFragmentViewCreated),
			nameof(IDialogFragmentService.OnFragmentStarted),
			nameof(IDialogFragmentService.OnFragmentResumed),
			nameof(IDialogFragmentService.OnFragmentSaveInstanceState),
			nameof(IDialogFragmentService.OnFragmentPaused),
			nameof(IDialogFragmentService.OnFragmentStopped),
			nameof(IDialogFragmentService.OnFragmentViewDestroyed),
			nameof(IDialogFragmentService.OnFragmentDestroyed),
			nameof(IDialogFragmentService.OnFragmentDetached),
		];
		Assert.Equal(expected, service.CallsFor(fragment));
		Assert.All(service.Calls.Where(call => ReferenceEquals(call.Arguments[1], fragment)), call => Assert.Same(childManager, call.Arguments[0]));
	}

	[Fact]
	public async Task RegisteredManagerForwardsTheViewAndSavedStateOfTheFragment()
	{
		var service = new RecordingDialogFragmentService();
		var manager = new FragmentLifecycleManager(service);
		var host = new PlainFragment();
		var fragment = new PlainFragment();
		AFragmentManager? childManager = null;

		try
		{
			await InvokeOnMainThreadAsync(() =>
			{
				HostActivity.SupportFragmentManager.BeginTransaction().Add(host, $"test-host-{Guid.NewGuid():N}").CommitNow();
				childManager = host.ChildFragmentManager;
				childManager.RegisterFragmentLifecycleCallbacks(manager, false);

				childManager.BeginTransaction().Add(fragment, "test-fragment").CommitNow();

				var attached = Assert.Single(service.Calls, call => call.Name == nameof(IDialogFragmentService.OnFragmentAttached) && ReferenceEquals(call.Arguments[1], fragment));
				Assert.Same(HostActivity, Assert.IsType<AppCompatActivity>(attached.Arguments[2], exactMatch: false));
				var viewCreated = Assert.Single(service.Calls, call => call.Name == nameof(IDialogFragmentService.OnFragmentViewCreated) && ReferenceEquals(call.Arguments[1], fragment));
				Assert.Same(fragment.View, viewCreated.Arguments[2]);
				Assert.NotNull(fragment.View);
			});
		}
		finally
		{
			await InvokeOnMainThreadAsync(() =>
			{
				childManager?.UnregisterFragmentLifecycleCallbacks(manager);
				childManager?.BeginTransaction().Remove(fragment).CommitNow();
				HostActivity.SupportFragmentManager.BeginTransaction().Remove(host).CommitNow();
			});
		}
	}

	[Fact]
	public async Task ManagerAndFragmentsAreCollectedAfterTheFragmentIsRemoved()
	{
		WeakReference managerReference = null!, serviceReference = null!, fragmentReference = null!, dialogFragmentReference = null!;
		var tag = $"test-leak-{Guid.NewGuid():N}";

		await InvokeOnMainThreadAsync(() =>
		{
			var service = new RecordingDialogFragmentService();
			var manager = new FragmentLifecycleManager(service);
			var fragment = new PlainFragment();
			var dialogFragment = new ProbeDialogFragment();
			var fragmentManager = HostActivity.SupportFragmentManager;

			fragmentManager.RegisterFragmentLifecycleCallbacks(manager, false);
			try
			{
				fragmentManager.BeginTransaction().Add(fragment, tag).CommitNow();
				dialogFragment.ShowNow(fragmentManager, tag + "-dialog");
				Assert.Contains(service.Calls, call => call.Name == nameof(IDialogFragmentService.OnFragmentResumed));

				fragmentManager.BeginTransaction().Remove(fragment).CommitNow();
				dialogFragment.DismissNow();
			}
			finally
			{
				fragmentManager.UnregisterFragmentLifecycleCallbacks(manager);
			}

			managerReference = new WeakReference(manager);
			serviceReference = new WeakReference(service);
			fragmentReference = new WeakReference(fragment);
			dialogFragmentReference = new WeakReference(dialogFragment);
		});

		await AssertionExtensions.WaitForGC(managerReference, serviceReference, fragmentReference, dialogFragmentReference);
	}

	static object?[] InvokeCallbackAndGetExpectedArguments(FragmentLifecycleManager manager, string callback, AFragmentManager fragmentManager, AFragment fragment, AppCompatActivity activity, AView view, Bundle bundle)
	{
		switch (callback)
		{
			case nameof(FragmentLifecycleManager.OnFragmentAttached):
				manager.OnFragmentAttached(fragmentManager, fragment, activity);
				return [fragmentManager, fragment, activity];
			case nameof(FragmentLifecycleManager.OnFragmentCreated):
				manager.OnFragmentCreated(fragmentManager, fragment, bundle);
				return [fragmentManager, fragment, bundle];
			case nameof(FragmentLifecycleManager.OnFragmentDestroyed):
				manager.OnFragmentDestroyed(fragmentManager, fragment);
				return [fragmentManager, fragment];
			case nameof(FragmentLifecycleManager.OnFragmentDetached):
				manager.OnFragmentDetached(fragmentManager, fragment);
				return [fragmentManager, fragment];
			case nameof(FragmentLifecycleManager.OnFragmentPaused):
				manager.OnFragmentPaused(fragmentManager, fragment);
				return [fragmentManager, fragment];
			case nameof(FragmentLifecycleManager.OnFragmentPreAttached):
				manager.OnFragmentPreAttached(fragmentManager, fragment, activity);
				return [fragmentManager, fragment, activity];
			case nameof(FragmentLifecycleManager.OnFragmentPreCreated):
				manager.OnFragmentPreCreated(fragmentManager, fragment, bundle);
				return [fragmentManager, fragment, bundle];
			case nameof(FragmentLifecycleManager.OnFragmentResumed):
				manager.OnFragmentResumed(fragmentManager, fragment);
				return [fragmentManager, fragment];
			case nameof(FragmentLifecycleManager.OnFragmentSaveInstanceState):
				manager.OnFragmentSaveInstanceState(fragmentManager, fragment, bundle);
				return [fragmentManager, fragment, bundle];
			case nameof(FragmentLifecycleManager.OnFragmentStarted):
				manager.OnFragmentStarted(fragmentManager, fragment);
				return [fragmentManager, fragment];
			case nameof(FragmentLifecycleManager.OnFragmentStopped):
				manager.OnFragmentStopped(fragmentManager, fragment);
				return [fragmentManager, fragment];
			case nameof(FragmentLifecycleManager.OnFragmentViewCreated):
				manager.OnFragmentViewCreated(fragmentManager, fragment, view, bundle);
				return [fragmentManager, fragment, view, bundle];
			case nameof(FragmentLifecycleManager.OnFragmentViewDestroyed):
				manager.OnFragmentViewDestroyed(fragmentManager, fragment);
				return [fragmentManager, fragment];
			default:
				throw new ArgumentOutOfRangeException(nameof(callback), callback, "Not a callback of FragmentLifecycleManager.");
		}
	}
}