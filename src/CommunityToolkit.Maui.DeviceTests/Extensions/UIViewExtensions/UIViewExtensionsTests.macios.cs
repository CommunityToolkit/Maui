using CommunityToolkit.Maui.Core.Extensions;
using CoreGraphics;
using Foundation;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

// UIViewExtensions (iOS + Mac Catalyst): each Safe*Anchor returns the matching anchor of the view's SafeAreaLayoutGuide, not the
// anchor of the view itself. The tests pin a probe view to the returned anchor with Auto Layout and read where UIKit puts it. The host
// view controller adds safe-area insets that differ on every edge, so the safe area and the plain view edges give different frames.
[Category(TestCategory.UIViewExtensions)]
[Collection(TestCollections.DisableParallelization)]
public class UIViewExtensionsTests : TestBase
{
	// top, left, bottom, right: different on every edge so that a swapped anchor is detected.
	static readonly UIEdgeInsets additionalInsets = new(11, 13, 17, 19);

	const double probeSize = 10;
	const double tolerance = 0.51;

	public enum SafeAnchor
	{
		Top,
		Bottom,
		CenterX,
		CenterY,
		Height,
		Width,
		Leading,
		Trailing,
		Left,
		Right,
	}

	[Theory]
	[InlineData(SafeAnchor.Top, false)]
	[InlineData(SafeAnchor.Bottom, false)]
	[InlineData(SafeAnchor.CenterX, false)]
	[InlineData(SafeAnchor.CenterY, false)]
	[InlineData(SafeAnchor.Height, false)]
	[InlineData(SafeAnchor.Width, false)]
	[InlineData(SafeAnchor.Leading, false)]
	[InlineData(SafeAnchor.Trailing, false)]
	[InlineData(SafeAnchor.Left, false)]
	[InlineData(SafeAnchor.Right, false)]
	// In a right-to-left view Leading/Trailing swap sides while Left/Right stay physical.
	[InlineData(SafeAnchor.Leading, true)]
	[InlineData(SafeAnchor.Trailing, true)]
	[InlineData(SafeAnchor.Left, true)]
	[InlineData(SafeAnchor.Right, true)]
	[InlineData(SafeAnchor.CenterX, true)]
	public async Task SafeAnchorFollowsTheSafeAreaOfTheView(SafeAnchor anchor, bool rightToLeft)
	{
		var direction = rightToLeft ? UISemanticContentAttribute.ForceRightToLeft : UISemanticContentAttribute.ForceLeftToRight;

		await RunInSafeAreaHost(direction, host =>
		{
			var insets = host.SafeAreaInsets;
			AssertSafeAreaDiffersFromTheViewEdges(host);

			var probe = AddProbe(host, direction);
			NSLayoutConstraint.ActivateConstraints(CreateConstraints(host, probe, anchor));
			host.SetNeedsLayout();
			host.LayoutIfNeeded();

			var bounds = host.Bounds;
			var frame = probe.Frame;
			var safeWidth = (double)(bounds.Width - insets.Left - insets.Right);
			var safeHeight = (double)(bounds.Height - insets.Top - insets.Bottom);
			var safeLeft = (double)insets.Left;
			var safeRight = (double)(bounds.Width - insets.Right);
			var safeTop = (double)insets.Top;
			var safeBottom = (double)(bounds.Height - insets.Bottom);

			switch (anchor)
			{
				case SafeAnchor.Top:
					AssertionExtensions.CloseEnough(safeTop, frame.Top, tolerance, "SafeTopAnchor");
					break;
				case SafeAnchor.Bottom:
					AssertionExtensions.CloseEnough(safeBottom, frame.Bottom, tolerance, "SafeBottomAnchor");
					break;
				case SafeAnchor.CenterY:
					AssertionExtensions.CloseEnough(safeTop + (safeHeight / 2), frame.GetMidY(), tolerance, "SafeCenterYAnchor");
					break;
				case SafeAnchor.Height:
					AssertionExtensions.CloseEnough(safeHeight, frame.Height, tolerance, "SafeHeightAnchor");
					break;
				case SafeAnchor.CenterX:
					AssertionExtensions.CloseEnough(safeLeft + (safeWidth / 2), frame.GetMidX(), tolerance, "SafeCenterXAnchor");
					break;
				case SafeAnchor.Width:
					AssertionExtensions.CloseEnough(safeWidth, frame.Width, tolerance, "SafeWidthAnchor");
					break;
				case SafeAnchor.Left:
					AssertionExtensions.CloseEnough(safeLeft, frame.Left, tolerance, "SafeLeftAnchor");
					break;
				case SafeAnchor.Right:
					AssertionExtensions.CloseEnough(safeRight, frame.Right, tolerance, "SafeRightAnchor");
					break;
				case SafeAnchor.Leading:
					if (rightToLeft)
					{
						AssertionExtensions.CloseEnough(safeRight, frame.Right, tolerance, "SafeLeadingAnchor in a right-to-left view");
					}
					else
					{
						AssertionExtensions.CloseEnough(safeLeft, frame.Left, tolerance, "SafeLeadingAnchor");
					}

					break;
				case SafeAnchor.Trailing:
					if (rightToLeft)
					{
						AssertionExtensions.CloseEnough(safeLeft, frame.Left, tolerance, "SafeTrailingAnchor in a right-to-left view");
					}
					else
					{
						AssertionExtensions.CloseEnough(safeRight, frame.Right, tolerance, "SafeTrailingAnchor");
					}

					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(anchor), anchor, null);
			}
		});
	}

	[Theory]
	[InlineData(SafeAnchor.Top)]
	[InlineData(SafeAnchor.Bottom)]
	[InlineData(SafeAnchor.CenterX)]
	[InlineData(SafeAnchor.CenterY)]
	[InlineData(SafeAnchor.Height)]
	[InlineData(SafeAnchor.Width)]
	[InlineData(SafeAnchor.Leading)]
	[InlineData(SafeAnchor.Trailing)]
	[InlineData(SafeAnchor.Left)]
	[InlineData(SafeAnchor.Right)]
	public async Task SafeAnchorIsTheAnchorOfTheSafeAreaLayoutGuide(SafeAnchor anchor)
	{
		await RunInSafeAreaHost(UISemanticContentAttribute.ForceLeftToRight, host =>
		{
			var guide = host.SafeAreaLayoutGuide;

			// UIKit hands out one native anchor object per native anchor, so the managed wrappers compare by reference.
			NSObject actual = anchor switch
			{
				SafeAnchor.Top => host.SafeTopAnchor(),
				SafeAnchor.Bottom => host.SafeBottomAnchor(),
				SafeAnchor.CenterX => host.SafeCenterXAnchor(),
				SafeAnchor.CenterY => host.SafeCenterYAnchor(),
				SafeAnchor.Height => host.SafeHeightAnchor(),
				SafeAnchor.Width => host.SafeWidthAnchor(),
				SafeAnchor.Leading => host.SafeLeadingAnchor(),
				SafeAnchor.Trailing => host.SafeTrailingAnchor(),
				SafeAnchor.Left => host.SafeLeftAnchor(),
				SafeAnchor.Right => host.SafeRightAnchor(),
				_ => throw new ArgumentOutOfRangeException(nameof(anchor), anchor, null),
			};

			NSObject expected = anchor switch
			{
				SafeAnchor.Top => guide.TopAnchor,
				SafeAnchor.Bottom => guide.BottomAnchor,
				SafeAnchor.CenterX => guide.CenterXAnchor,
				SafeAnchor.CenterY => guide.CenterYAnchor,
				SafeAnchor.Height => guide.HeightAnchor,
				SafeAnchor.Width => guide.WidthAnchor,
				SafeAnchor.Leading => guide.LeadingAnchor,
				SafeAnchor.Trailing => guide.TrailingAnchor,
				SafeAnchor.Left => guide.LeftAnchor,
				SafeAnchor.Right => guide.RightAnchor,
				_ => throw new ArgumentOutOfRangeException(nameof(anchor), anchor, null),
			};

			Assert.Equal(expected.Handle, actual.Handle);
		});
	}

	static void AssertSafeAreaDiffersFromTheViewEdges(UIView host)
	{
		var insets = host.SafeAreaInsets;

		// The additional insets of the host view controller are part of the safe area, so every edge is inset.
		Assert.True(insets.Top >= additionalInsets.Top, $"top inset {insets.Top}");
		Assert.True(insets.Left >= additionalInsets.Left, $"left inset {insets.Left}");
		Assert.True(insets.Bottom >= additionalInsets.Bottom, $"bottom inset {insets.Bottom}");
		Assert.True(insets.Right >= additionalInsets.Right, $"right inset {insets.Right}");
	}

	static UIView AddProbe(UIView host, UISemanticContentAttribute direction)
	{
		var probe = new UIView
		{
			TranslatesAutoresizingMaskIntoConstraints = false,
			SemanticContentAttribute = direction,
		};
		host.AddSubview(probe);
		return probe;
	}

	// Pins the probe to the anchor under test and fully determines the rest of its frame with plain anchors.
	static NSLayoutConstraint[] CreateConstraints(UIView host, UIView probe, SafeAnchor anchor) => anchor switch
	{
		SafeAnchor.Top =>
		[
			probe.TopAnchor.ConstraintEqualTo(host.SafeTopAnchor()),
			probe.LeftAnchor.ConstraintEqualTo(host.LeftAnchor),
			probe.WidthAnchor.ConstraintEqualTo((nfloat)probeSize),
			probe.HeightAnchor.ConstraintEqualTo((nfloat)probeSize),
		],
		SafeAnchor.Bottom =>
		[
			probe.BottomAnchor.ConstraintEqualTo(host.SafeBottomAnchor()),
			probe.LeftAnchor.ConstraintEqualTo(host.LeftAnchor),
			probe.WidthAnchor.ConstraintEqualTo((nfloat)probeSize),
			probe.HeightAnchor.ConstraintEqualTo((nfloat)probeSize),
		],
		SafeAnchor.CenterY =>
		[
			probe.CenterYAnchor.ConstraintEqualTo(host.SafeCenterYAnchor()),
			probe.LeftAnchor.ConstraintEqualTo(host.LeftAnchor),
			probe.WidthAnchor.ConstraintEqualTo((nfloat)probeSize),
			probe.HeightAnchor.ConstraintEqualTo((nfloat)probeSize),
		],
		SafeAnchor.Height =>
		[
			probe.HeightAnchor.ConstraintEqualTo(host.SafeHeightAnchor()),
			probe.TopAnchor.ConstraintEqualTo(host.TopAnchor),
			probe.LeftAnchor.ConstraintEqualTo(host.LeftAnchor),
			probe.WidthAnchor.ConstraintEqualTo((nfloat)probeSize),
		],
		SafeAnchor.CenterX =>
		[
			probe.CenterXAnchor.ConstraintEqualTo(host.SafeCenterXAnchor()),
			probe.TopAnchor.ConstraintEqualTo(host.TopAnchor),
			probe.WidthAnchor.ConstraintEqualTo((nfloat)probeSize),
			probe.HeightAnchor.ConstraintEqualTo((nfloat)probeSize),
		],
		SafeAnchor.Width =>
		[
			probe.WidthAnchor.ConstraintEqualTo(host.SafeWidthAnchor()),
			probe.TopAnchor.ConstraintEqualTo(host.TopAnchor),
			probe.LeftAnchor.ConstraintEqualTo(host.LeftAnchor),
			probe.HeightAnchor.ConstraintEqualTo((nfloat)probeSize),
		],
		SafeAnchor.Leading =>
		[
			probe.LeadingAnchor.ConstraintEqualTo(host.SafeLeadingAnchor()),
			probe.TopAnchor.ConstraintEqualTo(host.TopAnchor),
			probe.WidthAnchor.ConstraintEqualTo((nfloat)probeSize),
			probe.HeightAnchor.ConstraintEqualTo((nfloat)probeSize),
		],
		SafeAnchor.Trailing =>
		[
			probe.TrailingAnchor.ConstraintEqualTo(host.SafeTrailingAnchor()),
			probe.TopAnchor.ConstraintEqualTo(host.TopAnchor),
			probe.WidthAnchor.ConstraintEqualTo((nfloat)probeSize),
			probe.HeightAnchor.ConstraintEqualTo((nfloat)probeSize),
		],
		SafeAnchor.Left =>
		[
			probe.LeftAnchor.ConstraintEqualTo(host.SafeLeftAnchor()),
			probe.TopAnchor.ConstraintEqualTo(host.TopAnchor),
			probe.WidthAnchor.ConstraintEqualTo((nfloat)probeSize),
			probe.HeightAnchor.ConstraintEqualTo((nfloat)probeSize),
		],
		SafeAnchor.Right =>
		[
			probe.RightAnchor.ConstraintEqualTo(host.SafeRightAnchor()),
			probe.TopAnchor.ConstraintEqualTo(host.TopAnchor),
			probe.WidthAnchor.ConstraintEqualTo((nfloat)probeSize),
			probe.HeightAnchor.ConstraintEqualTo((nfloat)probeSize),
		],
		_ => throw new ArgumentOutOfRangeException(nameof(anchor), anchor, null),
	};

	// Hosts a view controller whose view has extra safe-area insets in the runner's window, so UIKit lays the view out with real
	// safe-area insets, and runs the action on the UI thread.
	static Task RunInSafeAreaHost(UISemanticContentAttribute direction, Action<UIView> action) =>
		InvokeOnMainThreadAsync(async () =>
		{
			using var viewController = new UIViewController
			{
				AdditionalSafeAreaInsets = additionalInsets,
			};
			var host = viewController.View ?? throw new InvalidOperationException("The host view controller has no view.");
			host.SemanticContentAttribute = direction;
			host.BackgroundColor = UIColor.White;

			await host.AttachAndRun(() => action(host));
		});
}