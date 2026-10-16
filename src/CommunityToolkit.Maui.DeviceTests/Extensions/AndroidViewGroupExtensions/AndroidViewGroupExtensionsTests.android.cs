using Android.Widget;
using CommunityToolkit.Maui.Core.Extensions;
using AView = Android.Views.View;

namespace CommunityToolkit.Maui.DeviceTests;

// AndroidViewGroupExtensions.FindChildWithTag (Android): StatusBar and NavigationBar find their own overlay view among the direct children
// of the decor view with it. An app view deeper in the hierarchy that carries the same tag must not be taken for it (ViewGroup.FindViewWithTag
// would return it), which StatusBarTests and NavigationBarTests only check on Android 15 and later.
[Category(TestCategory.AndroidViewGroupExtensions)]
public class AndroidViewGroupExtensionsTests : CommunityToolkitHandlerTestBase
{
	const string overlayTag = "overlay";

	[Fact]
	public async Task FindChildWithTagReturnsTheDirectChildWithTheTag()
	{
		await InvokeOnMainThreadAsync(() =>
		{
			var context = MauiProgramDefaults.DefaultContext;
			var root = new FrameLayout(context);
			root.AddView(new AView(context) { Tag = new Java.Lang.String("other") });
			var overlay = new AView(context) { Tag = new Java.Lang.String(overlayTag) };
			root.AddView(overlay);

			Assert.Same(overlay, root.FindChildWithTag(overlayTag));
		});
	}

	[Fact]
	public async Task FindChildWithTagIgnoresAViewWithTheTagDeeperInTheHierarchy()
	{
		await InvokeOnMainThreadAsync(() =>
		{
			var context = MauiProgramDefaults.DefaultContext;
			var root = new FrameLayout(context);
			var appLayout = new FrameLayout(context);
			appLayout.AddView(new AView(context) { Tag = new Java.Lang.String(overlayTag) });
			root.AddView(appLayout);

			Assert.NotNull(root.FindViewWithTag(new Java.Lang.String(overlayTag)));
			Assert.Null(root.FindChildWithTag(overlayTag));
		});
	}

	[Fact]
	public async Task FindChildWithTagOfAViewGroupWithoutChildrenOrWithUntaggedChildrenIsNull()
	{
		await InvokeOnMainThreadAsync(() =>
		{
			var context = MauiProgramDefaults.DefaultContext;
			var empty = new FrameLayout(context);
			var untagged = new FrameLayout(context);
			untagged.AddView(new AView(context));

			Assert.Null(empty.FindChildWithTag(overlayTag));
			Assert.Null(untagged.FindChildWithTag(overlayTag));
		});
	}
}