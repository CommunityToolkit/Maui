using CommunityToolkit.Maui.Core.Views;
using CoreGraphics;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.AlertView)]
[Collection(TestCollections.DisableParallelization)]
public class PaddedLabelTests : AlertsTestBase
{
	[Fact]
	public async Task PaddingsAreKeptPerSide()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var label = new PaddedLabel(3, 4, 5, 6);

			Assert.Equal(3, (double)label.LeftPadding);
			Assert.Equal(4, (double)label.TopPadding);
			Assert.Equal(5, (double)label.RightPadding);
			Assert.Equal(6, (double)label.BottomPadding);
		});
	}

	[Fact]
	public async Task IntrinsicContentSizeGrowsByThePaddings()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var padded = new PaddedLabel(3, 4, 5, 6) { Text = "Padded label", Font = UIFont.SystemFontOfSize(16)! };
			var plain = new UILabel { Text = "Padded label", Font = UIFont.SystemFontOfSize(16)! };

			Assert.Equal(plain.IntrinsicContentSize.Width + 3 + 5, padded.IntrinsicContentSize.Width, 2);
			Assert.Equal(plain.IntrinsicContentSize.Height + 4 + 6, padded.IntrinsicContentSize.Height, 2);
		});
	}

	[Fact]
	public async Task TextRectForBoundsStartsInsideTheLeftAndTopPaddings()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var padded = new PaddedLabel(3, 4, 5, 6) { Text = "Padded label", Font = UIFont.SystemFontOfSize(16)! };
			var plain = new UILabel { Text = "Padded label", Font = UIFont.SystemFontOfSize(16)! };
			var bounds = new CGRect(0, 0, 200, 50);

			var paddedRect = padded.TextRectForBounds(bounds, 1);
			var plainRect = plain.TextRectForBounds(bounds, 1);

			Assert.Equal(plainRect.X + 3, paddedRect.X, 2);
			Assert.Equal(plainRect.Y + 4, paddedRect.Y, 2);
			Assert.Equal(plainRect.Width, paddedRect.Width, 2);
			Assert.Equal(plainRect.Height, paddedRect.Height, 2);
		});
	}

	[Fact]
	public async Task TextRectForBoundsShrinksTheAvailableWidthByTheHorizontalPaddings()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var padded = new PaddedLabel(30, 4, 50, 6) { Text = "A message that is much too long to fit in the narrow bounds", Font = UIFont.SystemFontOfSize(16)!, Lines = 1 };

			var rect = padded.TextRectForBounds(new CGRect(0, 0, 200, 50), 1);

			Assert.True(rect.Width <= 200 - 30 - 50, $"The text rect is {rect.Width} wide but only {200 - 30 - 50} are available inside the paddings.");
			Assert.Equal(30, (double)rect.X, 2);
		});
	}

	[Fact]
	public async Task DrawTextKeepsTheLeftPaddingFree()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var padded = Render(new PaddedLabel(30, 0, 0, 0));
			var plain = Render(new UILabel());

			Assert.True(HasInk(plain, 0, 30), "The plain label draws text inside the first 30 columns, so the comparison would prove nothing.");
			Assert.False(HasInk(padded, 0, 30), "The padded label drew text inside its 30 point left padding.");
			Assert.True(HasInk(padded, 30, 100), "The padded label drew no text at all.");
		});
	}

	// The label is as tall as its text (plus the padding of the side under test), so UIKit, which centers the text vertically in the rect
	// it draws into, would draw into the padding if DrawText left it in the rect. A label with a long text truncates it at the end of the
	// rect, so the text reaches the right edge unless the right padding is taken out.
	[Theory]
	[InlineData("Top")]
	[InlineData("Bottom")]
	[InlineData("Right")]
	public async Task DrawTextKeepsThePaddingOfEachSideFree(string side)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			const int padding = 30;
			const int width = 160;
			var textHeight = (int)Math.Ceiling(UIFont.BoldSystemFontOfSize(24)!.LineHeight);
			var height = side is "Right" ? textHeight : textHeight + padding;
			var label = side switch
			{
				"Top" => new PaddedLabel(0, padding, 0, 0),
				"Bottom" => new PaddedLabel(0, 0, 0, padding),
				"Right" => new PaddedLabel(0, 0, padding, 0),
				_ => throw new ArgumentOutOfRangeException(nameof(side), side, null)
			};

			var padded = Render(label, new CGSize(width, height));
			var plain = Render(new UILabel(), new CGSize(width, height));

			// The padding and the rest of the label as (left, top, right, bottom) in points
			var (paddingArea, textArea) = side switch
			{
				"Top" => ((0, 0, width, padding), (0, padding, width, height)),
				"Bottom" => ((0, height - padding, width, height), (0, 0, width, height - padding)),
				_ => ((width - padding, 0, width, height), (0, 0, width - padding, height))
			};

			Assert.True(HasInk(plain, paddingArea), $"The plain label draws no text where the {side} padding is, so the comparison would prove nothing.");
			Assert.False(HasInk(padded, paddingArea), $"The padded label drew text inside its {padding} point {side} padding.");
			Assert.True(HasInk(padded, textArea), "The padded label drew no text outside the padding.");
		});
	}

	[Fact]
	public async Task PaddedLabelDoesNotLeakAfterItIsRemovedFromTheWindow()
	{
		SetupBuilder();
		WeakReference labelReference = null!;

		await InvokeOnMainThreadAsync(() =>
		{
			var label = new PaddedLabel(3, 4, 5, 6) { Text = "Leak", Frame = new CGRect(0, 0, 100, 30) };
			labelReference = new WeakReference(label);

			AlertsApple.KeyWindow.AddSubview(label);
			label.LayoutIfNeeded();
			label.RemoveFromSuperview();
		});

		await AssertionExtensions.WaitForGC(labelReference);
	}

	static UIImage Render(UILabel label) => Render(label, new CGSize(160, 40));

	static UIImage Render(UILabel label, CGSize size)
	{
		label.Text = "MMMMMMMM";
		label.Font = UIFont.BoldSystemFontOfSize(24)!;
		label.TextColor = UIColor.Black;
		label.BackgroundColor = UIColor.White;
		label.Frame = new CGRect(CGPoint.Empty, size);

		return new UIGraphicsImageRenderer(label.Frame.Size).CreateImage(context => label.Layer.RenderInContext(context.CGContext));
	}

	// True when a pixel in the column range [from, to) (points) is not white.
	static bool HasInk(UIImage image, int from, int to) => HasInk(image, (from, 0, to, int.MaxValue));

	// True when a pixel in the area [left, right) x [top, bottom) (points) is not white.
	static bool HasInk(UIImage image, (int Left, int Top, int Right, int Bottom) area)
	{
		var (pixels, width, height) = AlertsApple.ReadPixels(image);
		var scale = (int)image.CurrentScale;
		var right = (int)Math.Min((long)area.Right * scale, width);
		var bottom = (int)Math.Min((long)area.Bottom * scale, height);

		for (var x = area.Left * scale; x < right; x++)
		{
			for (var y = area.Top * scale; y < bottom; y++)
			{
				if (pixels[(((y * width) + x) * 4)] < 230)
				{
					return true;
				}
			}
		}

		return false;
	}
}

[Category(TestCategory.AlertView)]
[Collection(TestCollections.DisableParallelization)]
public class PaddedButtonTests : AlertsTestBase
{
	[Fact]
	public async Task PaddingsAreKeptPerSideAndAppliedAsContentEdgeInsets()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var button = new PaddedButton(3, 4, 5, 6);

			Assert.Equal(3, (double)button.LeftPadding);
			Assert.Equal(4, (double)button.TopPadding);
			Assert.Equal(5, (double)button.RightPadding);
			Assert.Equal(6, (double)button.BottomPadding);
			Assert.Equal(new UIEdgeInsets(4, 3, 6, 5), button.GetContentEdgeInsets());
		});
	}

	// SetPadding uses UIButton.Configuration on iOS 15 and later when the button has one. The guard is true on both Apple platforms
	// (Mac Catalyst reports its iOS version), but a button created with a constructor has no configuration, so PaddedButton always takes
	// the ContentEdgeInsets arm: the Configuration arm cannot be reached.
	[Fact]
	public async Task ButtonWithoutAConfigurationUsesContentEdgeInsetsEvenWhereConfigurationsAreSupported()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			Assert.True(OperatingSystem.IsIOSVersionAtLeast(15));

			var button = new PaddedButton(7, 8, 9, 10);

			Assert.Null(button.Configuration);
			Assert.Equal(new UIEdgeInsets(8, 7, 10, 9), button.GetContentEdgeInsets());
		});
	}

	[Fact]
	public async Task PaddedButtonDoesNotLeakAfterItIsRemovedFromTheWindow()
	{
		SetupBuilder();
		WeakReference buttonReference = null!;

		await InvokeOnMainThreadAsync(() =>
		{
			var button = new PaddedButton(3, 4, 5, 6) { Frame = new CGRect(0, 0, 100, 30) };
			button.SetTitle("Leak", UIControlState.Normal);
			buttonReference = new WeakReference(button);

			AlertsApple.KeyWindow.AddSubview(button);
			button.LayoutIfNeeded();
			button.RemoveFromSuperview();
		});

		await AssertionExtensions.WaitForGC(buttonReference);
	}

	[Fact]
	public async Task PaddingsEnlargeTheSizeThatFitsTheTitle()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var button = new PaddedButton(10, 12, 14, 16);
			button.SetTitle("Undo", UIControlState.Normal);
			button.TitleLabel.Font = UIFont.SystemFontOfSize(16)!;
			var title = button.TitleLabel.IntrinsicContentSize;

			var size = button.SizeThatFits(new CGSize(500, 500));

			// UIKit rounds the fitted size up to the pixel grid.
			Assert.InRange(size.Width, title.Width + 10 + 14 - 0.1, title.Width + 10 + 14 + 1);
			Assert.InRange(size.Height, title.Height + 12 + 16 - 0.1, title.Height + 12 + 16 + 1);
		});
	}
}