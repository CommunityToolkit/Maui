#pragma warning disable CS0618 // ListView and ViewCell are obsolete, but Expander still supports them in a ListView
using System.Runtime.InteropServices;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Foundation;
using Microsoft.Maui.Handlers;
using ObjCRuntime;
using UIKit;
using LayoutView = Microsoft.Maui.Platform.LayoutView;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class ExpanderTests
{
	const string objectiveCLibrary = "/usr/lib/libobjc.dylib";
	const string accessibilityExpandedStatusNeedsVersion18 = "UIKit has accessibilityExpandedStatus, which VoiceOver reads as expanded or collapsed, since iOS and Mac Catalyst 18; the simulators of the pool run iOS 27.";

	static bool HasAccessibilityExpandedStatus => OperatingSystem.IsIOSVersionAtLeast(18) || OperatingSystem.IsMacCatalystVersionAtLeast(18);

	static ExpanderLayoutView GetLayoutView(Expander expander) => Assert.IsType<ExpanderLayoutView>(((ContentView)expander).Content?.Handler?.PlatformView);

	static NSObject[]? GetAccessibilityElements(Expander expander) =>
		((IUIAccessibilityContainer)GetLayoutView(expander)).GetAccessibilityElements() is NSArray array ? NSArray.FromArray<NSObject>(array) : null;

	static UIAccessibilityElement GetHeaderAccessibilityElement(Expander expander) =>
		Assert.IsType<UIAccessibilityElement>(Assert.Single(GetAccessibilityElements(expander) ?? [], static element => element is UIAccessibilityElement), exactMatch: false);

	static UIView GetUIView(IView view) => Assert.IsType<UIView>(view.Handler?.PlatformView, exactMatch: false);

	// VoiceOver sends these messages to the element it focuses, so the tests send them through the Objective-C runtime too.
	static bool AccessibilityActivate(NSObject element) => SendBoolMessage(element.Handle, Selector.GetHandle("accessibilityActivate")) is not 0;

	static UIAccessibilityExpandedStatus GetAccessibilityExpandedStatus(NSObject element) =>
		(UIAccessibilityExpandedStatus)SendNIntMessage(element.Handle, Selector.GetHandle("accessibilityExpandedStatus"));

	[DllImport(objectiveCLibrary, EntryPoint = "objc_msgSend")]
	static extern byte SendBoolMessage(IntPtr receiver, IntPtr selector);

	[DllImport(objectiveCLibrary, EntryPoint = "objc_msgSend")]
	static extern nint SendNIntMessage(IntPtr receiver, IntPtr selector);

	[Fact]
	public async Task VoiceOverReadsTheWholeHeaderAsOneButton()
	{
		SetupBuilder();
		var header = new Label { Text = "Header", AutomationId = "HeaderAutomationId", HeightRequest = headerHeight };
		SemanticProperties.SetHint(header, "Shows the content");
		var expander = new Expander { Header = header, Content = new BoxView { Color = Colors.Red, HeightRequest = contentHeight } };

		await AttachAndRun(expander, async _ =>
		{
			await expander.OnLoadedAsync();
			var headerElement = Assert.IsType<UIAccessibilityElement>(Assert.Single(GetAccessibilityElements(expander) ?? []), exactMatch: false);
			var headerView = GetUIView(header);

			Assert.True(headerElement.IsAccessibilityElement);
			Assert.Equal("Header", headerElement.AccessibilityLabel);
			Assert.Equal("Shows the content", headerElement.AccessibilityHint);
			Assert.Equal("HeaderAutomationId", headerElement.AccessibilityIdentifier);
			Assert.Equal(UIAccessibilityTrait.Button, (UIAccessibilityTrait)headerElement.AccessibilityTraits);
			Assert.Equal(UIAccessibility.ConvertFrameToScreenCoordinates(headerView.Bounds, headerView), headerElement.AccessibilityFrame);
		});
	}

	[Fact]
	public async Task AccessibilityActivateTogglesTheExpanderAndVoiceOverReadsItsState()
	{
		Skip.IfNot(HasAccessibilityExpandedStatus, accessibilityExpandedStatusNeedsVersion18);
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
			var headerElement = GetHeaderAccessibilityElement(expander);
			Assert.Equal(UIAccessibilityExpandedStatus.Collapsed, GetAccessibilityExpandedStatus(headerElement));

			Assert.True(AccessibilityActivate(headerElement));

			Assert.True(expander.IsExpanded);
			Assert.Equal(UIAccessibilityExpandedStatus.Expanded, GetAccessibilityExpandedStatus(headerElement));
			await AssertionExtensions.AssertEventually(() => IsPlatformViewVisible(expander.Content), message: "accessibilityActivate did not show the content.");

			Assert.True(AccessibilityActivate(headerElement));

			Assert.False(expander.IsExpanded);
			Assert.Equal(UIAccessibilityExpandedStatus.Collapsed, GetAccessibilityExpandedStatus(headerElement));
			Assert.Equal<object?>([nameof(Expander.CommandParameter), nameof(Expander.CommandParameter)], commandParameters);

			expander.IsExpanded = true;
			Assert.Equal(UIAccessibilityExpandedStatus.Expanded, GetAccessibilityExpandedStatus(headerElement));

			expander.IsExpanded = false;
			Assert.Equal(UIAccessibilityExpandedStatus.Collapsed, GetAccessibilityExpandedStatus(headerElement));

			Assert.Equal<bool>([true, false, true, false], expandedChanges);
		});
	}

	[Theory]
	[InlineData(ExpandDirection.Down)]
	[InlineData(ExpandDirection.Up)]
	public async Task VoiceOverReachesTheContentOfAnExpandedExpanderInTheOrderOfItsDirection(ExpandDirection direction)
	{
		SetupBuilder();
		var expander = CreateExpander(direction);

		await AttachAndRun(expander, async _ =>
		{
			await expander.OnLoadedAsync();
			var headerElement = GetHeaderAccessibilityElement(expander);

			expander.IsExpanded = true;
			await AssertionExtensions.AssertEventually(() => IsPlatformViewVisible(expander.Content), message: "Expanding did not show the content.");

			var contentView = GetUIView(expander.Content);
			NativeHandle[] expectedOrder = direction is ExpandDirection.Down ? [headerElement.Handle, contentView.Handle] : [contentView.Handle, headerElement.Handle];
			Assert.Equal(expectedOrder, (GetAccessibilityElements(expander) ?? []).Select(static element => element.Handle));

			expander.IsExpanded = false;
			await AssertionExtensions.AssertEventually(() => !IsPlatformViewVisible(expander.Content), message: "Collapsing did not hide the content.");

			Assert.Equal<NativeHandle>([headerElement.Handle], (GetAccessibilityElements(expander) ?? []).Select(static element => element.Handle));
		});
	}

	[Theory]
	[InlineData(nameof(Expander.IsEnabled))]
	[InlineData(nameof(Expander.Header) + nameof(VisualElement.IsEnabled))]
	[InlineData(nameof(Expander.Header) + nameof(VisualElement.InputTransparent))]
	public async Task VoiceOverReadsAHeaderThatATapCannotToggleAsDimmedAndCannotActivateIt(string blockingProperty)
	{
		SetupBuilder();
		var expander = CreateExpander();

		await AttachAndRun(expander, async _ =>
		{
			await expander.OnLoadedAsync();
			var headerElement = GetHeaderAccessibilityElement(expander);

			SetHeaderActivationBlocked(expander, blockingProperty, isBlocked: true);

			Assert.Equal(UIAccessibilityTrait.Button | UIAccessibilityTrait.NotEnabled, (UIAccessibilityTrait)headerElement.AccessibilityTraits);
			Assert.False(AccessibilityActivate(headerElement));
			Assert.False(expander.IsExpanded);

			SetHeaderActivationBlocked(expander, blockingProperty, isBlocked: false);

			Assert.Equal(UIAccessibilityTrait.Button, (UIAccessibilityTrait)headerElement.AccessibilityTraits);
			Assert.True(AccessibilityActivate(headerElement));
			Assert.True(expander.IsExpanded);
		});
	}

	[Fact]
	public async Task WithoutAVisibleHeaderTheLayoutKeepsTheDefaultAccessibilityElementsOfUIKit()
	{
		SetupBuilder();
		var expander = CreateExpander();

		await AttachAndRun(expander, async _ =>
		{
			await expander.OnLoadedAsync();
			Assert.NotNull(GetAccessibilityElements(expander));

			SetHeaderActivationBlocked(expander, nameof(Expander.Header) + nameof(VisualElement.IsVisible), isBlocked: true);

			Assert.Null(GetAccessibilityElements(expander));
		});
	}

	// Without elements of its own the layout leaves UIKit's default, its subviews, through which VoiceOver reaches the control in the header.
	[Theory]
	[InlineData(nameof(Button))]
	[InlineData(nameof(Editor))]
	public async Task AHeaderThatHoldsAControlKeepsTheDefaultAccessibilityElementsOfUIKit(string controlType)
	{
		SetupBuilder();
		View control = controlType switch
		{
			nameof(Button) => new Button { Text = "Delete" },
			nameof(Editor) => new Editor { Text = "Notes" },
			_ => throw new ArgumentOutOfRangeException(nameof(controlType), controlType, "Not a control that the test puts in the header.")
		};
		var expander = new Expander
		{
			Header = new HorizontalStackLayout { new Label { Text = "Header" }, control },
			Content = new Label { Text = "Content" }
		};

		await AttachAndRun(expander, async _ =>
		{
			await expander.OnLoadedAsync();

			Assert.Null(GetAccessibilityElements(expander));

			control.IsVisible = false;

			Assert.NotNull(GetAccessibilityElements(expander));
		});
	}

	// VoiceOver taps an element whose accessibilityActivate returns false, and that tap runs every TapGestureRecognizer of the header.
	[Fact]
	public async Task WithAnotherTapGestureRecognizerOnTheHeaderAccessibilityActivateLeavesTheTapToVoiceOver()
	{
		SetupBuilder();
		var header = new Label { Text = "Header", HeightRequest = headerHeight };
		header.GestureRecognizers.Add(new TapGestureRecognizer());
		var expander = new Expander { Header = header, Content = new BoxView { Color = Colors.Red, HeightRequest = contentHeight } };

		await AttachAndRun(expander, async _ =>
		{
			await expander.OnLoadedAsync();

			Assert.False(AccessibilityActivate(GetHeaderAccessibilityElement(expander)));
			Assert.False(expander.IsExpanded);
		});
	}

	[Fact]
	public async Task VoiceOverReadsALayoutHeaderAsTheLabelsItHolds()
	{
		SetupBuilder();
		var expander = new Expander
		{
			Header = new HorizontalStackLayout
			{
				new BoxView { Color = Colors.Blue, WidthRequest = 20, HeightRequest = 20 },
				new Label { Text = "Brandon" },
				new Label { Text = "Minnick" }
			},
			Content = new Label { Text = "Content" }
		};

		await AttachAndRun(expander, async _ =>
		{
			await expander.OnLoadedAsync();

			Assert.Equal("Brandon, Minnick", GetHeaderAccessibilityElement(expander).AccessibilityLabel);
		});
	}

	// MAUI's LayoutHandler.PlatformViewFactory creates the platform view of every layout handler, and ExpanderLayoutHandler is one, but only an
	// ExpanderLayoutView makes VoiceOver read the header as one button
	[Fact]
	public async Task VoiceOverReadsTheHeaderAsOneButtonWhenTheAppSetsTheLayoutPlatformViewFactory()
	{
		SetupBuilder();
		var headerLayout = new HorizontalStackLayout { new Label { Text = "Brandon" }, new Label { Text = "Minnick" } };
		var contentLayout = new VerticalStackLayout { new Label { Text = "Content" } };
		var expander = new Expander { Header = headerLayout, Content = contentLayout };
		var gridBeside = new Grid { new Label { Text = "Beside" } };
		var page = new VerticalStackLayout { expander, gridBeside };
		var handlersOfTheFactory = new List<IViewHandler>();
		var platformViewsOfTheFactory = new List<object?>();
		var factoriesThatTheAppRead = new List<object?>();
		object? platformViewFactoryOfTheApp = null;
		object? platformViewFactoryAfterwards = null;

		// HandlerChanging is code of the app that runs while the layout of the Expander sets its own handler up, so it must find the factory of the app
		headerLayout.HandlerChanging += (_, _) => factoriesThatTheAppRead.Add(LayoutHandler.PlatformViewFactory);

		await InvokeOnMainThreadAsync(() =>
		{
			var previousFactory = LayoutHandler.PlatformViewFactory;
			LayoutHandler.PlatformViewFactory = handler =>
			{
				handlersOfTheFactory.Add(handler);
				var platformView = new LayoutView();
				platformViewsOfTheFactory.Add(platformView);
				return platformView;
			};
			platformViewFactoryOfTheApp = LayoutHandler.PlatformViewFactory;
			try
			{
				CreateHandler(page);
				platformViewFactoryAfterwards = LayoutHandler.PlatformViewFactory;
			}
			finally
			{
				LayoutHandler.PlatformViewFactory = previousFactory;
			}
		});

		Assert.Same(platformViewFactoryOfTheApp, platformViewFactoryAfterwards);
		Assert.Same(platformViewFactoryOfTheApp, Assert.Single(factoriesThatTheAppRead));

		await AttachAndRun(page, async _ =>
		{
			await page.OnLoadedAsync();
			var layoutHandler = Assert.IsType<ExpanderLayoutHandler>(((ContentView)expander).Content?.Handler);
			Assert.IsType<ExpanderLayoutView>(layoutHandler.PlatformView);
			Assert.DoesNotContain(layoutHandler, handlersOfTheFactory);

			// The layouts in the Header and the Content get their handlers while the layout of the Expander sets its own handler up
			Assert.Contains(headerLayout.Handler?.PlatformView, platformViewsOfTheFactory);
			Assert.Contains(contentLayout.Handler?.PlatformView, platformViewsOfTheFactory);
			Assert.Contains(gridBeside.Handler?.PlatformView, platformViewsOfTheFactory);
			Assert.Contains(page.Handler?.PlatformView, platformViewsOfTheFactory);

			var headerElement = Assert.IsType<UIAccessibilityElement>(Assert.Single(GetAccessibilityElements(expander) ?? []), exactMatch: false);
			Assert.Equal("Brandon, Minnick", headerElement.AccessibilityLabel);
			Assert.Equal(UIAccessibilityTrait.Button, (UIAccessibilityTrait)headerElement.AccessibilityTraits);
			Assert.True(AccessibilityActivate(headerElement));
			Assert.True(expander.IsExpanded);
		});
	}

	[Fact]
	public async Task TheLayoutAndItsHeaderElementAreCollectedWithTheExpander()
	{
		SetupBuilder();
		var expanderReference = new WeakReference(null);
		var layoutViewReference = new WeakReference(null);
		var headerElementReference = new WeakReference(null);

		await InvokeOnMainThreadAsync(() =>
		{
			var expander = CreateExpander();
			var handler = CreateHandler<ContentViewHandler>(expander);
			var headerElement = GetHeaderAccessibilityElement(expander);
			Assert.True(AccessibilityActivate(headerElement));
			Assert.True(expander.IsExpanded);

			expanderReference.Target = expander;
			layoutViewReference.Target = GetLayoutView(expander);
			headerElementReference.Target = headerElement;

			((IElementHandler)handler).DisconnectHandler();
			expander.Handler = null;
		});

		await AssertionExtensions.WaitForGC(expanderReference, layoutViewReference, headerElementReference);
	}

	[Fact]
	public async Task HeaderTapInsideAListViewWhosePlatformViewIsNotATableViewTogglesAndDoesNotThrow()
	{
		SetupBuilder(builder => builder.ConfigureMauiHandlers(handlers => handlers.AddHandler<ListView, NonTableListViewHandler>()));
		var listView = CreateListView(itemCount: 1);
		var expander = GetExpander(listView, 0);

		var exception = await Record.ExceptionAsync(() => InvokeOnMainThreadAsync(() =>
		{
			var handler = CreateHandler<NonTableListViewHandler>(listView);
			Assert.IsNotType<UITableView>(handler.PlatformView);

			expander.HandleHeaderTap(new TappedEventArgs(null));
		}));

		Assert.Null(exception);
		Assert.True(expander.IsExpanded);
	}

	[Fact]
	public async Task HeaderTapReloadsTheTableViewOfEveryListViewTheExpanderIsIn()
	{
		SetupBuilder(builder => builder.ConfigureMauiHandlers(handlers => handlers.AddHandler<ListView, ReloadCountingListViewHandler>()));
		var outer = new ListView();
		var inner = new ListView();
		var expander = new Expander { Header = new Label { Text = "Header" }, Content = new Label { Text = "Content" } };

		inner.Parent = outer;
		expander.Parent = inner;

		await InvokeOnMainThreadAsync(() =>
		{
			var outerTable = CreateHandler<ReloadCountingListViewHandler>(outer).PlatformView;
			var innerTable = CreateHandler<ReloadCountingListViewHandler>(inner).PlatformView;

			expander.HandleHeaderTap(new TappedEventArgs(null));

			Assert.True(expander.IsExpanded);
			Assert.Equal(1, innerTable.ReloadCount);
			Assert.Equal(1, outerTable.ReloadCount);

			expander.HandleHeaderTap(new TappedEventArgs(null));

			Assert.False(expander.IsExpanded);
			Assert.Equal(2, innerTable.ReloadCount);
			Assert.Equal(2, outerTable.ReloadCount);
		});
	}

	[Fact]
	public async Task HeaderTapWithoutAHeaderDoesNotReloadTheTableViewOfTheListView()
	{
		SetupBuilder(builder => builder.ConfigureMauiHandlers(handlers => handlers.AddHandler<ListView, ReloadCountingListViewHandler>()));
		var listView = new ListView();
		var expander = new Expander { Content = new Label { Text = "Content" } };
		expander.Parent = listView;

		await InvokeOnMainThreadAsync(() =>
		{
			var table = CreateHandler<ReloadCountingListViewHandler>(listView).PlatformView;

			// Without a header, ResizeExpanderInItemsView returns before it walks the parents.
			expander.HandleHeaderTap(new TappedEventArgs(null));

			Assert.True(expander.IsExpanded);
			Assert.Equal(0, table.ReloadCount);
		});
	}

	static bool IsPlatformViewVisible(IView view) => !((UIView)view.Handler!.PlatformView!).Hidden;

	static double GetRowHeight(ListView listView, int row) => GetRowFrame(listView, row).Height;

	static double GetRowTop(ListView listView, int row) => GetRowFrame(listView, row).Y;

	static CoreGraphics.CGRect GetRowFrame(ListView listView, int row)
	{
		var table = Assert.IsType<UITableView>(listView.Handler!.PlatformView);
		table.LayoutIfNeeded();
		return table.RectForRowAtIndexPath(NSIndexPath.FromRowSection(row, 0));
	}

	// A plain UIView as platform view reaches the `as UITableView` null branch.
	sealed class NonTableListViewHandler : ViewHandler<ListView, UIView>
	{
		public NonTableListViewHandler() : base(ViewMapper)
		{
		}

		protected override UIView CreatePlatformView() => new();
	}

	sealed class ReloadCountingListViewHandler : ViewHandler<ListView, ReloadCountingTableView>
	{
		public ReloadCountingListViewHandler() : base(ViewMapper)
		{
		}

		protected override ReloadCountingTableView CreatePlatformView() => new();
	}

	public sealed class ReloadCountingTableView : UITableView
	{
		public int ReloadCount { get; private set; }

		public override void ReloadData()
		{
			ReloadCount++;
			base.ReloadData();
		}
	}
}