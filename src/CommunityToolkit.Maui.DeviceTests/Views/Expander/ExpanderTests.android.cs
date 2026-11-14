#pragma warning disable CS0618 // ListView and ViewCell are obsolete, but Expander still supports them in a ListView
using Android.Views;
using AndroidX.Core.View.Accessibility;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Platform;
using AccessibilityAction = Android.Views.Accessibility.Action;
using AListView = Android.Widget.ListView;
using AView = Android.Views.View;

namespace CommunityToolkit.Maui.DeviceTests;

// Expander needs no Android code to resize a row of a ListView: the platform list measures its rows again by itself.
public partial class ExpanderTests
{
	static bool IsPlatformViewVisible(IView view) => ((Android.Views.View)view.Handler!.PlatformView!).Visibility == ViewStates.Visible;

	static double GetRowHeight(ListView listView, int row) => ToDips(listView, GetRow(listView, row)?.Height ?? 0);

	static double GetRowTop(ListView listView, int row) => ToDips(listView, GetRow(listView, row)?.Top ?? 0);

	// MAUI's ListView adds a header (and footer) container to the platform list, so the first row sits after HeaderViewsCount children.
	static Android.Views.View? GetRow(ListView listView, int row)
	{
		var native = Assert.IsType<AListView>(listView.Handler!.PlatformView);
		return native.GetChildAt(row + native.HeaderViewsCount - native.FirstVisiblePosition);
	}

	static double ToDips(ListView listView, int pixels)
	{
		var native = Assert.IsType<AListView>(listView.Handler!.PlatformView);
		return pixels / native.Context!.Resources!.DisplayMetrics!.Density;
	}

	static AView GetPlatformView(IView view) => Assert.IsType<AView>(view.Handler?.PlatformView, exactMatch: false);

	// The node that the view creates is what TalkBack reads.
	static int[] GetAccessibilityActionIds(AView view)
	{
		using var nodeInfo = view.CreateAccessibilityNodeInfo() ?? throw new InvalidOperationException("The view created no AccessibilityNodeInfo.");
		return [.. (nodeInfo.ActionList ?? []).Select(static action => action.Id)];
	}

	[Fact]
	public async Task TalkBackFindsTheHeaderOfferingExpandWhileCollapsedAndCollapseWhileExpanded()
	{
		SetupBuilder();
		var expander = CreateExpander();

		await AttachAndRun(expander, async _ =>
		{
			await expander.OnLoadedAsync();

			var collapsedActions = GetAccessibilityActionIds(GetPlatformView(expander.Header));
			AotSafeAssert.Contains(AccessibilityNodeInfoCompat.ActionExpand, collapsedActions);
			AotSafeAssert.DoesNotContain(AccessibilityNodeInfoCompat.ActionCollapse, collapsedActions);
			AotSafeAssert.Contains(AccessibilityNodeInfoCompat.ActionClick, collapsedActions);

			expander.IsExpanded = true;

			var expandedActions = GetAccessibilityActionIds(GetPlatformView(expander.Header));
			AotSafeAssert.Contains(AccessibilityNodeInfoCompat.ActionCollapse, expandedActions);
			AotSafeAssert.DoesNotContain(AccessibilityNodeInfoCompat.ActionExpand, expandedActions);
			AotSafeAssert.Contains(AccessibilityNodeInfoCompat.ActionClick, expandedActions);
		});
	}

	[Fact]
	public async Task TheExpandAndCollapseActionsOfTheHeaderToggleTheExpander()
	{
		SetupBuilder();
		var expander = CreateExpander();
		List<bool> expandedChanges = [];
		List<object?> commandParameters = [];
		expander.ExpandedChanged += (_, e) => expandedChanges.Add(e.IsExpanded);
		expander.Command = new Command(parameter => commandParameters.Add(parameter));
		expander.CommandParameter = nameof(Expander.CommandParameter);

		await AttachAndRun(expander, async _ =>
		{
			await expander.OnLoadedAsync();
			var header = GetPlatformView(expander.Header);

			Assert.False(header.PerformAccessibilityAction(AccessibilityAction.Collapse, null));
			Assert.False(expander.IsExpanded);

			Assert.True(header.PerformAccessibilityAction(AccessibilityAction.Expand, null));
			Assert.True(expander.IsExpanded);
			var expandedActions = GetAccessibilityActionIds(header);
			AotSafeAssert.Contains(AccessibilityNodeInfoCompat.ActionCollapse, expandedActions);
			AotSafeAssert.DoesNotContain(AccessibilityNodeInfoCompat.ActionExpand, expandedActions);
			await AssertionExtensions.AssertEventually(() => IsPlatformViewVisible(expander.Content), message: "The expand action did not show the content.");

			Assert.False(header.PerformAccessibilityAction(AccessibilityAction.Expand, null));
			Assert.True(expander.IsExpanded);

			Assert.True(header.PerformAccessibilityAction(AccessibilityAction.Collapse, null));
			Assert.False(expander.IsExpanded);
			var collapsedActions = GetAccessibilityActionIds(header);
			AotSafeAssert.Contains(AccessibilityNodeInfoCompat.ActionExpand, collapsedActions);
			AotSafeAssert.DoesNotContain(AccessibilityNodeInfoCompat.ActionCollapse, collapsedActions);

			AotSafeAssert.SequenceEqual<bool>([true, false], expandedChanges);
			Assert.Equal<object?>([nameof(Expander.CommandParameter), nameof(Expander.CommandParameter)], commandParameters);
		});
	}

	[Theory]
	[InlineData(nameof(Expander.IsEnabled))]
	[InlineData(nameof(Expander.Header) + nameof(VisualElement.IsEnabled))]
	[InlineData(nameof(Expander.Header) + nameof(VisualElement.IsVisible))]
	[InlineData(nameof(Expander.Header) + nameof(VisualElement.InputTransparent))]
	public async Task AHeaderThatATapCannotToggleOffersNeitherExpandNorCollapse(string blockingProperty)
	{
		SetupBuilder();
		var expander = CreateExpander();

		await AttachAndRun(expander, async _ =>
		{
			await expander.OnLoadedAsync();
			var header = GetPlatformView(expander.Header);
			AotSafeAssert.Contains(AccessibilityNodeInfoCompat.ActionExpand, GetAccessibilityActionIds(header));

			SetHeaderActivationBlocked(expander, blockingProperty, isBlocked: true);

			var blockedActions = GetAccessibilityActionIds(header);
			AotSafeAssert.DoesNotContain(AccessibilityNodeInfoCompat.ActionExpand, blockedActions);
			AotSafeAssert.DoesNotContain(AccessibilityNodeInfoCompat.ActionCollapse, blockedActions);
			Assert.False(header.PerformAccessibilityAction(AccessibilityAction.Expand, null));
			Assert.False(expander.IsExpanded);

			SetHeaderActivationBlocked(expander, blockingProperty, isBlocked: false);

			AotSafeAssert.Contains(AccessibilityNodeInfoCompat.ActionExpand, GetAccessibilityActionIds(header));
		});
	}

	[Fact]
	public async Task TheExpandActionMovesFromAReplacedHeaderToTheNewHeader()
	{
		SetupBuilder();
		var expander = CreateExpander();
		var newHeader = new Label { Text = "New header" };

		await AttachAndRun(expander, async _ =>
		{
			await expander.OnLoadedAsync();
			var replacedHeader = GetPlatformView(expander.Header);

			expander.Header = newHeader;

			AotSafeAssert.DoesNotContain(AccessibilityNodeInfoCompat.ActionExpand, GetAccessibilityActionIds(replacedHeader));
			Assert.False(replacedHeader.PerformAccessibilityAction(AccessibilityAction.Expand, null));
			Assert.False(expander.IsExpanded);

			AotSafeAssert.Contains(AccessibilityNodeInfoCompat.ActionExpand, GetAccessibilityActionIds(GetPlatformView(newHeader)));
			Assert.True(GetPlatformView(newHeader).PerformAccessibilityAction(AccessibilityAction.Expand, null));
			Assert.True(expander.IsExpanded);
		});
	}

	// MAUI adds the click of the header's TapGestureRecognizer to the node only when the view has no accessibility delegate yet or one of MAUI's
	// own, and it adds it after the HandlerChanged event in which the Expander adds the expand action.
	[Fact]
	public async Task ANewPlatformViewOfTheHeaderOffersTheExpandActionAndTheClickOfItsTapGestureRecognizer()
	{
		SetupBuilder();
		var expander = CreateExpander();

		await AttachAndRun(expander, async _ =>
		{
			await expander.OnLoadedAsync();
			var header = Assert.IsType<Microsoft.Maui.Controls.View>(expander.Header, exactMatch: false);
			var firstPlatformView = GetPlatformView(header);

			header.Handler?.DisconnectHandler();
			header.Handler = null;

			AotSafeAssert.DoesNotContain(AccessibilityNodeInfoCompat.ActionExpand, GetAccessibilityActionIds(firstPlatformView));

			var secondPlatformView = Assert.IsType<AView>(header.ToPlatform(MauiContext), exactMatch: false);

			Assert.NotSame(firstPlatformView, secondPlatformView);
			var actions = GetAccessibilityActionIds(secondPlatformView);
			AotSafeAssert.Contains(AccessibilityNodeInfoCompat.ActionExpand, actions);
			AotSafeAssert.Contains(AccessibilityNodeInfoCompat.ActionClick, actions);
		});
	}
}