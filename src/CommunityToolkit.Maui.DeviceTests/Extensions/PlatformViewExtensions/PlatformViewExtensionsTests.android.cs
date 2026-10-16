using Android.Content;
using Android.Views;
using Android.Widget;
using AScrollView = Android.Widget.ScrollView;
using AView = Android.Views.View;
using JavaObject = Java.Lang.Object;
using ToolkitPlatformViewExtensions = CommunityToolkit.Maui.Extensions.PlatformViewExtensions;

namespace CommunityToolkit.Maui.DeviceTests;

// PlatformViewExtensions (Android): the toolkit's own FindParentOfType, IsPeerDisposed and IsPeerAlive helpers, tested on real
// Android views and Java peers.
[Category(TestCategory.PlatformViewExtensions)]
[Collection(TestCollections.DisableParallelization)]
public class PlatformViewExtensionsTests : TestBase
{
	static Context Context => MauiProgramDefaults.DefaultContext
		?? throw new InvalidOperationException("The runner's activity is not available.");

	[Fact]
	public Task FindParentOfTypeReturnsTheViewItselfWhenItHasTheRequestedType() =>
		InvokeOnMainThreadAsync(() =>
		{
			using var tree = new ViewTree(Context);

			Assert.Same(tree.Text, ToolkitPlatformViewExtensions.FindParentOfType<TextView>(tree.Text));
			Assert.Same(tree.Text, ToolkitPlatformViewExtensions.FindParentOfType<AView>(tree.Text));
			// The view itself wins even though a parent of the same type exists.
			Assert.Same(tree.Scroll, ToolkitPlatformViewExtensions.FindParentOfType<FrameLayout>(tree.Scroll));
			Assert.Same(tree.Linear, ToolkitPlatformViewExtensions.FindParentOfType<ViewGroup>(tree.Linear));
		});

	[Fact]
	public Task FindParentOfTypeReturnsNullForANullView() =>
		InvokeOnMainThreadAsync(() =>
		{
			Assert.Null(ToolkitPlatformViewExtensions.FindParentOfType<ViewGroup>(null));
			Assert.Null(ToolkitPlatformViewExtensions.FindParentOfType<AView>(null));
		});

	[Fact]
	public Task FindParentOfTypeReturnsTheNearestParentOfTheRequestedType() =>
		InvokeOnMainThreadAsync(() =>
		{
			using var tree = new ViewTree(Context);

			// text -> linear -> relative -> scroll (a FrameLayout) -> outer (a FrameLayout)
			Assert.Same(tree.Linear, ToolkitPlatformViewExtensions.FindParentOfType<LinearLayout>(tree.Text));
			Assert.Same(tree.Linear, ToolkitPlatformViewExtensions.FindParentOfType<ViewGroup>(tree.Text));
			Assert.Same(tree.Relative, ToolkitPlatformViewExtensions.FindParentOfType<RelativeLayout>(tree.Text));
			Assert.Same(tree.Scroll, ToolkitPlatformViewExtensions.FindParentOfType<AScrollView>(tree.Text));
			Assert.Same(tree.Scroll, ToolkitPlatformViewExtensions.FindParentOfType<FrameLayout>(tree.Text));
			Assert.Same(tree.Outer, ToolkitPlatformViewExtensions.FindParentOfType<FrameLayout>(tree.Scroll.Parent as AView));
		});

	[Fact]
	public Task FindParentOfTypeFindsAParentThatIsAnInterface() =>
		InvokeOnMainThreadAsync(() =>
		{
			using var tree = new ViewTree(Context);

			// Every ViewGroup is an IViewParent: the nearest one is the direct parent.
			var parent = ToolkitPlatformViewExtensions.FindParentOfType<IViewParent>(tree.Text);

			Assert.NotNull(parent);
			Assert.Equal(tree.Linear.Handle, ((JavaObject)parent).Handle);
		});

	[Fact]
	public Task FindParentOfTypeReturnsNullWhenNoParentHasTheRequestedType() =>
		InvokeOnMainThreadAsync(() =>
		{
			using var tree = new ViewTree(Context);

			Assert.Null(ToolkitPlatformViewExtensions.FindParentOfType<HorizontalScrollView>(tree.Text));
			Assert.Null(ToolkitPlatformViewExtensions.FindParentOfType<TextView>(tree.Linear));
			// The root of a detached tree has no parent at all.
			Assert.Null(tree.Outer.Parent);
			Assert.Null(ToolkitPlatformViewExtensions.FindParentOfType<HorizontalScrollView>(tree.Outer));
			Assert.Null(ToolkitPlatformViewExtensions.FindParentOfType<LinearLayout>(new TextView(Context)));
		});

	[Fact]
	public Task FindParentOfTypeWalksPastTheTreeUpToTheWindowRoot() =>
		InvokeOnMainThreadAsync(async () =>
		{
			using var tree = new ViewTree(Context);

			await tree.Outer.AttachAndRun(() =>
			{
				Assert.True(tree.Text.IsAttachedToWindow);

				// The tree's own parents are found first, then the walk goes on above it: the layout that AttachAndRun adds is the
				// first parent of the outermost view.
				Assert.Same(tree.Scroll, ToolkitPlatformViewExtensions.FindParentOfType<AScrollView>(tree.Text));
				var host = ToolkitPlatformViewExtensions.FindParentOfType<ViewGroup>(tree.Outer.Parent as AView);
				Assert.NotNull(host);
				Assert.Equal(((JavaObject)tree.Outer.Parent!).Handle, host.Handle);

				// A type that no parent has: the walk ends at the window's root, whose parent is null.
				Assert.Null(ToolkitPlatformViewExtensions.FindParentOfType<HorizontalScrollView>(tree.Text));
				Assert.Null(ToolkitPlatformViewExtensions.FindParentOfType<TextView>(tree.Outer));
			});
		});

	[Fact]
	public void IsPeerDisposedThrowsForANullPeer()
	{
		var exception = Assert.Throws<ArgumentNullException>(() => ToolkitPlatformViewExtensions.IsPeerDisposed(null!));

		Assert.Equal("javaObject", exception.ParamName);
	}

	[Fact]
	public Task IsPeerDisposedIsFalseForALivePeerAndTrueAfterDispose() =>
		InvokeOnMainThreadAsync(() =>
		{
			JavaObject javaObject = new Java.Lang.String(nameof(javaObject));
			var view = new TextView(Context);

			Assert.False(ToolkitPlatformViewExtensions.IsPeerDisposed(javaObject));
			Assert.False(ToolkitPlatformViewExtensions.IsPeerDisposed(view));

			javaObject.Dispose();
			view.Dispose();

			Assert.Equal(IntPtr.Zero, javaObject.Handle);
			Assert.Equal(IntPtr.Zero, view.Handle);
			Assert.True(ToolkitPlatformViewExtensions.IsPeerDisposed(javaObject));
			Assert.True(ToolkitPlatformViewExtensions.IsPeerDisposed(view));
		});

	[Fact]
	public void IsPeerAliveIsFalseForANullPeer() =>
		Assert.False(ToolkitPlatformViewExtensions.IsPeerAlive(null));

	[Fact]
	public Task IsPeerAliveIsTrueForALivePeerAndFalseAfterDispose() =>
		InvokeOnMainThreadAsync(() =>
		{
			JavaObject javaObject = new Java.Lang.String(nameof(javaObject));
			var view = new TextView(Context);

			Assert.True(ToolkitPlatformViewExtensions.IsPeerAlive(javaObject));
			Assert.True(ToolkitPlatformViewExtensions.IsPeerAlive(view));

			javaObject.Dispose();
			view.Dispose();

			Assert.False(ToolkitPlatformViewExtensions.IsPeerAlive(javaObject));
			Assert.False(ToolkitPlatformViewExtensions.IsPeerAlive(view));
		});

	// outer (FrameLayout) > scroll (AScrollView) > relative (RelativeLayout) > linear (LinearLayout) > text (TextView)
	sealed class ViewTree : IDisposable
	{
		public ViewTree(Context context)
		{
			Outer = new FrameLayout(context);
			Scroll = new AScrollView(context);
			Relative = new RelativeLayout(context);
			Linear = new LinearLayout(context);
			Text = new TextView(context) { Text = nameof(ViewTree) };

			Outer.AddView(Scroll);
			Scroll.AddView(Relative);
			Relative.AddView(Linear);
			Linear.AddView(Text);
		}

		public FrameLayout Outer { get; }

		public AScrollView Scroll { get; }

		public RelativeLayout Relative { get; }

		public LinearLayout Linear { get; }

		public TextView Text { get; }

		public void Dispose()
		{
			Outer.RemoveAllViews();
			Scroll.RemoveAllViews();
			Relative.RemoveAllViews();
			Linear.RemoveAllViews();
		}
	}
}