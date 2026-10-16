using CommunityToolkit.Maui.Core.Views;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using WLayoutInformation = Microsoft.UI.Xaml.Controls.Primitives.LayoutInformation;
using WPanel = Microsoft.UI.Xaml.Controls.Panel;
using WRect = Windows.Foundation.Rect;
using WSize = Windows.Foundation.Size;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;

namespace CommunityToolkit.Maui.DeviceTests;

// Device tests of WrapperControl (Windows): the WinUI Panel that hosts the platform view of a MAUI view. Its layout code is driven
// directly (Measure and Arrange on the panel) against a spy platform view that records what the panel asks of it and returns a
// size the test chooses, so every number the panel computes is observable. The two behaviors that need a live visual tree (the
// Loaded handler and the MeasureInvalidated subscription) run in a real window.
//
// The MAUI view of those tests has its own handler (ProbeView, ProbeViewHandler) that ignores InvalidateMeasure: the panel then
// only learns about an invalidation through the event it subscribed to, and not through the platform view of the handler.
[Category(TestCategory.WrapperControl)]
[Collection(TestCollections.DisableParallelization)]
public partial class WrapperControlTests : CommunityToolkitHandlerTestBase
{
	static readonly WSize spyDesiredSize = new(80, 20);

	static void RegisterProbe(MauiAppBuilder builder) => builder.ConfigureMauiHandlers(handlers => handlers.AddHandler<ProbeView, ProbeViewHandler>());

	// ---- constructor

	[Fact]
	public async Task ConstructorThrowsForANullView()
	{
		SetupBuilder(RegisterProbe);

		var exception = await InvokeOnMainThreadAsync(() => Assert.Throws<ArgumentNullException>(() => new WrapperControl(null!, MauiContext)));

		Assert.Equal("view", exception.ParamName);
	}

	[Fact]
	public async Task ConstructorThrowsForANullMauiContext()
	{
		SetupBuilder(RegisterProbe);

		var exception = await InvokeOnMainThreadAsync(() => Assert.Throws<ArgumentNullException>(() => new WrapperControl(new ProbeView(), null!)));

		Assert.Equal("mauiContext", exception.ParamName);
	}

	[Fact]
	public async Task ConstructorAddsThePlatformViewOfTheMauiViewAsTheOnlyChild()
	{
		SetupBuilder(RegisterProbe);

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new ProbeView();
			var wrapper = new WrapperControl(view, MauiContext);

			var child = Assert.Single(wrapper.Children);
			Assert.Same(view.Handler!.PlatformView, child);
			Assert.IsType<SpyPanel>(child);
			wrapper.CleanUp();
		});
	}

	[Fact]
	public async Task ConstructorWrapsTheTextBlockOfALabel()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var label = new Label { Text = "Wrapped" };
			var wrapper = new WrapperControl(label, MauiContext);

			var child = Assert.IsType<WTextBlock>(Assert.Single(wrapper.Children));
			Assert.Same(label.Handler!.PlatformView, child);
			Assert.Equal("Wrapped", child.Text);

			wrapper.Measure(new WSize(double.PositiveInfinity, double.PositiveInfinity));
			Assert.True(child.DesiredSize.Width > 0 && child.DesiredSize.Height > 0, $"The label should have measured a text size but measured {child.DesiredSize}.");
			Assert.Equal(child.DesiredSize.Width, wrapper.DesiredSize.Width);
			wrapper.CleanUp();
		});
	}

	// ---- MeasureOverride

	[Fact]
	public async Task MeasureOfAFillViewWithAFiniteWidthTakesTheAvailableWidthAndTheHeightOfTheChild()
	{
		SetupBuilder(RegisterProbe);

		await InvokeOnMainThreadAsync(() =>
		{
			var (wrapper, _, spy) = CreateWrapper(LayoutOptions.Fill);

			wrapper.Measure(new WSize(300, 100));

			// The child is measured with exactly the space the panel got...
			Assert.Equal(new WSize(300, 100), spy.MeasuredWith.Last());
			// ...and the panel is as wide as that space, but only as high as the child wants to be.
			Assert.Equal(new WSize(300, spyDesiredSize.Height), wrapper.DesiredSize);
			wrapper.CleanUp();
		});
	}

	[Theory]
	[InlineData(LayoutAlignment.Start)]
	[InlineData(LayoutAlignment.Center)]
	[InlineData(LayoutAlignment.End)]
	public async Task MeasureOfAViewThatDoesNotFillTakesTheSizeTheChildWants(LayoutAlignment alignment)
	{
		SetupBuilder(RegisterProbe);

		await InvokeOnMainThreadAsync(() =>
		{
			var (wrapper, _, spy) = CreateWrapper(new LayoutOptions(alignment, false));

			wrapper.Measure(new WSize(300, 100));

			Assert.Equal(new WSize(300, 100), spy.MeasuredWith.Last());
			Assert.Equal(spyDesiredSize, wrapper.DesiredSize);
			wrapper.CleanUp();
		});
	}

	[Fact]
	public async Task MeasureOfAFillViewWithAnInfiniteWidthTakesTheSizeTheChildWants()
	{
		SetupBuilder(RegisterProbe);

		await InvokeOnMainThreadAsync(() =>
		{
			var (wrapper, _, spy) = CreateWrapper(LayoutOptions.Fill);
			var unbounded = new WSize(double.PositiveInfinity, double.PositiveInfinity);

			// An infinite width would make the panel infinitely wide, which WinUI rejects.
			var exception = Record.Exception(() => wrapper.Measure(unbounded));

			Assert.Null(exception);
			Assert.Equal(unbounded, spy.MeasuredWith.Last());
			Assert.Equal(spyDesiredSize, wrapper.DesiredSize);
			wrapper.CleanUp();
		});
	}

	[Fact]
	public async Task MeasureOfAFillViewWithAFiniteHeightKeepsTheHeightOfTheChild()
	{
		SetupBuilder(RegisterProbe);

		await InvokeOnMainThreadAsync(() =>
		{
			var (wrapper, _, spy) = CreateWrapper(LayoutOptions.Fill);
			spy.DesiredToReturn = new WSize(80, 70);

			wrapper.Measure(new WSize(300, 100));

			Assert.Equal(new WSize(300, 70), wrapper.DesiredSize);
			wrapper.CleanUp();
		});
	}

	// An available width of 0 is its own case in the source, but its result is the same as the Fill case: WinUI clamps the desired
	// size of the child to the space it was given, so the child's request is as wide as the available width, 0.
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task MeasureWithNoAvailableWidthIsAsWideAsTheAvailableWidth(bool fill)
	{
		SetupBuilder(RegisterProbe);

		await InvokeOnMainThreadAsync(() =>
		{
			var (wrapper, _, spy) = CreateWrapper(fill ? LayoutOptions.Fill : LayoutOptions.Start);

			wrapper.Measure(new WSize(0, 100));

			Assert.Equal(new WSize(0, 100), spy.MeasuredWith.Last());
			Assert.Equal(new WSize(0, spyDesiredSize.Height), wrapper.DesiredSize);
			wrapper.CleanUp();
		});
	}

	// ---- ArrangeOverride

	[Fact]
	public async Task ArrangeSetsTheFrameOfTheMauiViewAndArrangesTheChildAndKeepsThePanelVisible()
	{
		SetupBuilder(RegisterProbe);

		await InvokeOnMainThreadAsync(() =>
		{
			var (wrapper, view, spy) = CreateWrapper(LayoutOptions.Fill);

			MeasureAndArrange(wrapper, 200, 50);

			Assert.Equal(new Rect(0, 0, 200, 50), view.Frame);
			Assert.Equal(200, view.Width);
			Assert.Equal(50, view.Height);
			Assert.Equal(new WRect(0, 0, 200, 50), WLayoutInformation.GetLayoutSlot(spy));
			// The slot is larger than the 80 x 20 the child wants, so WinUI hands it to the child's ArrangeOverride unchanged.
			Assert.Equal(new WSize(200, 50), spy.ArrangedWith.Last());
			Assert.Equal(1.0, wrapper.Opacity);
			wrapper.CleanUp();
		});
	}

	[Theory]
	[InlineData(0.0, 50.0)]
	[InlineData(200.0, 0.0)]
	[InlineData(0.0, 0.0)]
	public async Task ArrangeHidesThePanelWhenTheMauiViewHasNoWidthOrNoHeight(double width, double height)
	{
		SetupBuilder(RegisterProbe);

		await InvokeOnMainThreadAsync(() =>
		{
			var (wrapper, view, spy) = CreateWrapper(LayoutOptions.Fill);

			MeasureAndArrange(wrapper, width, height);

			// The panel must not overlap its siblings when it has nothing to show.
			Assert.Equal(0.0, wrapper.Opacity);
			// The panel gives its child the whole slot it got. The slot is what the panel decides, not the size the child's
			// ArrangeOverride receives: WinUI raises that size to the child's own desired size (80 x 20 here) when the slot is smaller
			// (FrameworkElement::ArrangeCore in microsoft-ui-xaml), and it does not arrange a child that is not dirty in the slot that
			// child already has (a child never measured, as in the 0 x 0 row, has the slot 0, 0, 0, 0).
			Assert.Equal(new WRect(0, 0, width, height), WLayoutInformation.GetLayoutSlot(spy));
			Assert.Equal(width, view.Width);
			Assert.Equal(height, view.Height);
			wrapper.CleanUp();
		});
	}

	[Fact]
	public async Task ArrangeShowsThePanelAgainWhenTheSizeBecomesPositive()
	{
		SetupBuilder(RegisterProbe);

		await InvokeOnMainThreadAsync(() =>
		{
			var (wrapper, _, _) = CreateWrapper(LayoutOptions.Fill);
			MeasureAndArrange(wrapper, 0, 0);
			Assert.Equal(0.0, wrapper.Opacity);

			MeasureAndArrange(wrapper, 120, 40);

			Assert.Equal(1.0, wrapper.Opacity);
			wrapper.CleanUp();
		});
	}

	// ---- Loaded handler and MeasureInvalidated subscription (a live visual tree)

	[Fact]
	public async Task LoadingThePlatformViewInvalidatesTheMeasureOfThePanelAgain()
	{
		SetupBuilder(RegisterProbe);

		await InvokeOnMainThreadAsync(() =>
		{
			var wrapper = new SpyWrapperControl(new ProbeView(), MauiContext) { Width = 200, Height = 50 };

			return wrapper.AttachAndRun(async () =>
			{
				// The first measure happens before the platform view is loaded. Its Loaded handler invalidates the panel, and the second
				// measure then runs with every control in a consistent, loaded state.
				await AssertionExtensions.AssertEventually(() => wrapper.MeasureCount >= 2, timeout: 5000, message: $"The panel was measured {wrapper.MeasureCount} time(s); it should be measured again once its platform view is loaded.");
				wrapper.CleanUp();
			}, MauiContext);
		});
	}

	[Fact]
	public async Task MauiViewInvalidationInvalidatesTheMeasureOfThePanelUntilCleanUp()
	{
		SetupBuilder(RegisterProbe);

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new ProbeView();
			var wrapper = new SpyWrapperControl(view, MauiContext) { Width = 200, Height = 50 };

			return wrapper.AttachAndRun(async () =>
			{
				await AssertionExtensions.AssertEventually(() => wrapper.MeasureCount >= 2, timeout: 5000, message: "The panel was never measured again after it was loaded.");
				wrapper.UpdateLayout();
				var measuredBeforeInvalidation = wrapper.MeasureCount;

				// The handler of ProbeView ignores InvalidateMeasure, so only the subscription of the panel can reach it.
				view.RaiseMeasureInvalidated();
				wrapper.UpdateLayout();

				await AssertionExtensions.AssertEventually(() => wrapper.MeasureCount > measuredBeforeInvalidation, timeout: 5000, message: "Invalidating the measure of the MAUI view should invalidate the measure of its panel.");

				wrapper.CleanUp();
				wrapper.UpdateLayout();
				var measuredAfterCleanUp = wrapper.MeasureCount;

				view.RaiseMeasureInvalidated();
				// UpdateLayout runs a pending measure synchronously: a panel that still listened would have been measured again by now.
				wrapper.UpdateLayout();

				Assert.Equal(measuredAfterCleanUp, wrapper.MeasureCount);
			}, MauiContext);
		});
	}

	[Fact]
	public async Task CleanUpCanBeCalledMoreThanOnce()
	{
		SetupBuilder(RegisterProbe);

		await InvokeOnMainThreadAsync(() =>
		{
			var wrapper = new WrapperControl(new ProbeView(), MauiContext);

			var exception = Record.Exception(() =>
			{
				wrapper.CleanUp();
				wrapper.CleanUp();
			});

			Assert.Null(exception);
		});
	}

	// ---- leak

	[Fact]
	public async Task PanelMauiViewAndPlatformViewAreCollectedAfterCleanUp()
	{
		SetupBuilder(RegisterProbe);
		WeakReference wrapperReference = null!, viewReference = null!, platformViewReference = null!;

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new ProbeView();
			var wrapper = new WrapperControl(view, MauiContext);
			wrapperReference = new WeakReference(wrapper);
			viewReference = new WeakReference(view);
			platformViewReference = new WeakReference(view.Handler!.PlatformView);

			wrapper.CleanUp();
			wrapper.Children.Clear();
			view.Handler.DisconnectHandler();
			view.Handler = null;
		});

		await AssertionExtensions.WaitForGC(wrapperReference, viewReference, platformViewReference);
	}

	// ---- helpers

	// A panel around a ProbeView whose platform view returns spyDesiredSize and records what the panel passes to it.
	(WrapperControl Wrapper, ProbeView View, SpyPanel Spy) CreateWrapper(LayoutOptions horizontalOptions)
	{
		var view = new ProbeView { HorizontalOptions = horizontalOptions };
		var wrapper = new WrapperControl(view, MauiContext);
		return (wrapper, view, Assert.IsType<SpyPanel>(Assert.Single(wrapper.Children)));
	}

	// Lays the panel out as its parent would: Measure, then Arrange in (0, 0, width, height). The panel is in no window, so WinUI lays
	// it out with the layout manager of the app's main visual tree, whose root also holds every window of the app, and an Arrange does
	// nothing while that tree waits for a measure, e.g. after an earlier test closed its window (CUIElement::Arrange in
	// microsoft-ui-xaml: the loop stops on CLayoutManager::GetRequiresMeasure). UpdateLayout of an element outside the tree runs that
	// pending layout of the app's windows first and leaves the panel alone; Measure has no such check.
	static void MeasureAndArrange(WrapperControl wrapper, double width, double height)
	{
		wrapper.Measure(new WSize(width, height));
		wrapper.UpdateLayout();
		wrapper.Arrange(new WRect(0, 0, width, height));
	}

	// Counts the measure passes of the panel, which is what its Loaded handler and its MeasureInvalidated subscription cause.
	sealed partial class SpyWrapperControl(View view, IMauiContext mauiContext) : WrapperControl(view, mauiContext)
	{
		public int MeasureCount { get; private set; }

		protected override WSize MeasureOverride(WSize availableSize)
		{
			MeasureCount++;
			return base.MeasureOverride(availableSize);
		}
	}

	// A platform view that records the sizes the panel measures and arranges it with, and returns a size the test chooses.
	sealed partial class SpyPanel : WPanel
	{
		public WSize DesiredToReturn { get; set; } = spyDesiredSize;

		public List<WSize> MeasuredWith { get; } = [];

		public List<WSize> ArrangedWith { get; } = [];

		protected override WSize MeasureOverride(WSize availableSize)
		{
			MeasuredWith.Add(availableSize);
			return DesiredToReturn;
		}

		protected override WSize ArrangeOverride(WSize finalSize)
		{
			ArrangedWith.Add(finalSize);
			return finalSize;
		}
	}

	sealed class ProbeView : View
	{
		public void RaiseMeasureInvalidated() => InvalidateMeasure();
	}

	// A handler without property mappings (nothing the panel does to the MAUI view reaches the platform view) whose InvalidateMeasure
	// command does nothing, unlike the handler of every MAUI control.
	sealed class ProbeViewHandler : ViewHandler<ProbeView, SpyPanel>
	{
		static readonly PropertyMapper<ProbeView, ProbeViewHandler> probeMapper = new();

		static readonly CommandMapper<ProbeView, ProbeViewHandler> probeCommandMapper = new()
		{
			[nameof(IView.InvalidateMeasure)] = static (_, _, _) => { }
		};

		public ProbeViewHandler() : base(probeMapper, probeCommandMapper)
		{
		}

		protected override SpyPanel CreatePlatformView() => new();
	}
}