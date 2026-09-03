using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Layouts;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using Xunit;

namespace CommunityToolkit.Maui.DeviceTests.Tests.Core;

public class DockLayoutManagerTests
{
	[Fact]
	public void Measure_EmptyLayout_ReturnsPaddingSize()
	{
		var layout = new TestDockLayout();
		var manager = new DockLayoutManager(layout);

		var result = manager.Measure(500, 500);

		Assert.Equal(0, result.Width);
		Assert.Equal(0, result.Height);
	}

	[Fact]
	public void Measure_WithPadding_ReturnsPaddingSize()
	{
		var layout = new TestDockLayout
		{
			Padding = new Thickness(10, 20)
		};
		var manager = new DockLayoutManager(layout);

		var result = manager.Measure(500, 500);

		Assert.Equal(20, result.Width);
		Assert.Equal(40, result.Height);
	}

	[Fact]
	public void Measure_SingleTopChild_AddsHeight()
	{
		var child = new TestView(100, 50);
		var layout = new TestDockLayout();
		layout.AddChild(child, DockPosition.Top);
		var manager = new DockLayoutManager(layout);

		var result = manager.Measure(500, 500);

		// A Top-docked child accumulates only height; width stays at padding (0)
		Assert.Equal(0, result.Width);
		Assert.Equal(50, result.Height);
	}

	[Fact]
	public void Measure_SingleLeftChild_AddsWidth()
	{
		var child = new TestView(80, 200);
		var layout = new TestDockLayout();
		layout.AddChild(child, DockPosition.Left);
		var manager = new DockLayoutManager(layout);

		var result = manager.Measure(500, 500);

		// A Left-docked child accumulates only width; height stays at padding (0)
		Assert.Equal(80, result.Width);
		Assert.Equal(0, result.Height);
	}

	[Fact]
	public void Measure_MultipleChildren_AccumulatesSize()
	{
		var topChild = new TestView(100, 30);
		var leftChild = new TestView(50, 100);
		var layout = new TestDockLayout();
		layout.AddChild(topChild, DockPosition.Top);
		layout.AddChild(leftChild, DockPosition.Left);
		var manager = new DockLayoutManager(layout);

		var result = manager.Measure(500, 500);

		Assert.True(result.Width >= 50);
		Assert.True(result.Height >= 30);
	}

	[Fact]
	public void Measure_RespectsWidthConstraint()
	{
		var child = new TestView(1000, 50);
		var layout = new TestDockLayout();
		layout.AddChild(child, DockPosition.Top);
		var manager = new DockLayoutManager(layout);

		var result = manager.Measure(200, 500);

		Assert.True(result.Width <= 200);
	}

	[Fact]
	public void Measure_RespectsHeightConstraint()
	{
		var child = new TestView(100, 1000);
		var layout = new TestDockLayout();
		layout.AddChild(child, DockPosition.Left);
		var manager = new DockLayoutManager(layout);

		var result = manager.Measure(500, 200);

		Assert.True(result.Height <= 200);
	}

	[Fact]
	public void Measure_InvisibleChild_IsSkipped()
	{
		var visibleChild = new TestView(100, 50);
		var invisibleChild = new TestView(100, 50) { IsVisible = false };
		var layout = new TestDockLayout();
		layout.AddChild(visibleChild, DockPosition.Top);
		layout.AddChild(invisibleChild, DockPosition.Top);
		var manager = new DockLayoutManager(layout);

		var result = manager.Measure(500, 500);

		// Only the visible Top child contributes height; width stays at padding (0)
		Assert.Equal(0, result.Width);
		Assert.Equal(50, result.Height);
	}

	[Fact]
	public void ArrangeChildren_EmptyLayout_ReturnsBoundsSize()
	{
		var layout = new TestDockLayout();
		var manager = new DockLayoutManager(layout);

		var result = manager.ArrangeChildren(new Rect(0, 0, 300, 300));

		Assert.Equal(300, result.Width);
		Assert.Equal(300, result.Height);
	}

	[Fact]
	public void ArrangeChildren_SingleChild_ExpandsLastChild()
	{
		var child = new TestView(100, 50);
		var layout = new TestDockLayout
		{
			ShouldExpandLastChild = true
		};
		layout.AddChild(child, DockPosition.Top);
		var manager = new DockLayoutManager(layout);

		manager.ArrangeChildren(new Rect(0, 0, 300, 300));

		Assert.Equal(new Rect(0, 0, 300, 300), child.ArrangedRect);
	}

	[Fact]
	public void ArrangeChildren_TopChild_PositionsAtTop()
	{
		var topChild = new TestView(100, 50);
		var fillChild = new TestView(100, 100);
		var layout = new TestDockLayout
		{
			ShouldExpandLastChild = true
		};
		layout.AddChild(topChild, DockPosition.Top);
		layout.AddChild(fillChild, DockPosition.None);
		var manager = new DockLayoutManager(layout);

		manager.ArrangeChildren(new Rect(0, 0, 300, 300));

		Assert.Equal(0, topChild.ArrangedRect.Top);
	}

	[Fact]
	public void ArrangeChildren_LeftChild_PositionsAtLeft()
	{
		var leftChild = new TestView(50, 100);
		var fillChild = new TestView(100, 100);
		var layout = new TestDockLayout
		{
			ShouldExpandLastChild = true
		};
		layout.AddChild(leftChild, DockPosition.Left);
		layout.AddChild(fillChild, DockPosition.None);
		var manager = new DockLayoutManager(layout);

		manager.ArrangeChildren(new Rect(0, 0, 300, 300));

		Assert.Equal(0, leftChild.ArrangedRect.Left);
	}

	[Fact]
	public void ArrangeChildren_RightChild_PositionsAtRight()
	{
		var rightChild = new TestView(50, 100);
		var fillChild = new TestView(100, 100);
		var layout = new TestDockLayout
		{
			ShouldExpandLastChild = true
		};
		layout.AddChild(rightChild, DockPosition.Right);
		layout.AddChild(fillChild, DockPosition.None);
		var manager = new DockLayoutManager(layout);

		// Measure first so each child's DesiredSize is populated before arranging
		manager.Measure(300, 300);
		manager.ArrangeChildren(new Rect(0, 0, 300, 300));

		Assert.Equal(250, rightChild.ArrangedRect.Left);
	}

	[Fact]
	public void ArrangeChildren_BottomChild_PositionsAtBottom()
	{
		var bottomChild = new TestView(100, 50);
		var fillChild = new TestView(100, 100);
		var layout = new TestDockLayout
		{
			ShouldExpandLastChild = true
		};
		layout.AddChild(bottomChild, DockPosition.Bottom);
		layout.AddChild(fillChild, DockPosition.None);
		var manager = new DockLayoutManager(layout);

		// Measure first so each child's DesiredSize is populated before arranging
		manager.Measure(300, 300);
		manager.ArrangeChildren(new Rect(0, 0, 300, 300));

		Assert.Equal(250, bottomChild.ArrangedRect.Top);
	}

	[Fact]
	public void ArrangeChildren_WithPadding_OffsetsChildren()
	{
		var child = new TestView(100, 50);
		var layout = new TestDockLayout
		{
			Padding = new Thickness(10),
			ShouldExpandLastChild = true
		};
		layout.AddChild(child, DockPosition.None);
		var manager = new DockLayoutManager(layout);

		manager.ArrangeChildren(new Rect(0, 0, 300, 300));

		Assert.Equal(10, child.ArrangedRect.Left);
		Assert.Equal(10, child.ArrangedRect.Top);
	}

	[Fact]
	public void ArrangeChildren_InvisibleChild_IsSkipped()
	{
		var visibleChild = new TestView(100, 50);
		var invisibleChild = new TestView(100, 50) { IsVisible = false };
		var layout = new TestDockLayout
		{
			ShouldExpandLastChild = true
		};
		layout.AddChild(invisibleChild, DockPosition.Top);
		layout.AddChild(visibleChild, DockPosition.None);
		var manager = new DockLayoutManager(layout);

		manager.ArrangeChildren(new Rect(0, 0, 300, 300));

		Assert.Equal(Rect.Zero, invisibleChild.ArrangedRect);
	}

	[Fact]
	public void ArrangeChildren_ShouldNotExpandLastChild()
	{
		var child = new TestView(100, 50);
		var layout = new TestDockLayout
		{
			ShouldExpandLastChild = false
		};
		layout.AddChild(child, DockPosition.None);
		var manager = new DockLayoutManager(layout);

		manager.Measure(300, 300);
		manager.ArrangeChildren(new Rect(0, 0, 300, 300));

		// With ShouldExpandLastChild=false the None child keeps its natural height (50)
		// rather than being expanded to the full bounds height (300).
		Assert.Equal(300, child.ArrangedRect.Width);
		Assert.Equal(50, child.ArrangedRect.Height);
	}
}

public class UniformItemsLayoutManagerTests
{
	[Fact]
	public void Measure_EmptyLayout_ReturnsZero()
	{
		var layout = new TestUniformItemsLayout();
		var manager = new UniformItemsLayoutManager(layout);

		var result = manager.Measure(500, 500);

		Assert.Equal(0, result.Width);
		Assert.Equal(0, result.Height);
	}

	[Fact]
	public void Measure_SingleChild_ReturnsChildSize()
	{
		var child = new TestView(100, 50);
		var layout = new TestUniformItemsLayout();
		layout.AddChild(child);
		var manager = new UniformItemsLayoutManager(layout);

		var result = manager.Measure(500, 500);

		Assert.Equal(100, result.Width);
		Assert.Equal(50, result.Height);
	}

	[Fact]
	public void Measure_MultipleChildren_CalculatesGrid()
	{
		var layout = new TestUniformItemsLayout();
		layout.AddChild(new TestView(100, 50));
		layout.AddChild(new TestView(100, 50));
		layout.AddChild(new TestView(100, 50));
		var manager = new UniformItemsLayoutManager(layout);

		var result = manager.Measure(300, 500);

		Assert.Equal(300, result.Width);
		Assert.Equal(50, result.Height);
	}

	[Fact]
	public void Measure_WrapsToNextRow()
	{
		var layout = new TestUniformItemsLayout();
		layout.AddChild(new TestView(100, 50));
		layout.AddChild(new TestView(100, 50));
		layout.AddChild(new TestView(100, 50));
		var manager = new UniformItemsLayoutManager(layout);

		var result = manager.Measure(200, 500);

		Assert.Equal(200, result.Width);
		Assert.Equal(100, result.Height);
	}

	[Fact]
	public void Measure_RespectsMaxColumns()
	{
		var layout = new TestUniformItemsLayout
		{
			MaxColumns = 2
		};
		layout.AddChild(new TestView(100, 50));
		layout.AddChild(new TestView(100, 50));
		layout.AddChild(new TestView(100, 50));
		var manager = new UniformItemsLayoutManager(layout);

		var result = manager.Measure(500, 500);

		Assert.Equal(200, result.Width);
		Assert.Equal(100, result.Height);
	}

	[Fact]
	public void Measure_RespectsMaxRows()
	{
		var layout = new TestUniformItemsLayout
		{
			MaxRows = 1
		};
		layout.AddChild(new TestView(100, 50));
		layout.AddChild(new TestView(100, 50));
		layout.AddChild(new TestView(100, 50));
		var manager = new UniformItemsLayoutManager(layout);

		var result = manager.Measure(200, 500);

		Assert.Equal(200, result.Width);
		Assert.Equal(50, result.Height);
	}

	[Fact]
	public void Measure_WithPadding_IncludesPadding()
	{
		var layout = new TestUniformItemsLayout
		{
			Padding = new Thickness(10)
		};
		layout.AddChild(new TestView(100, 50));
		var manager = new UniformItemsLayoutManager(layout);

		var result = manager.Measure(500, 500);

		Assert.Equal(120, result.Width);
		Assert.Equal(70, result.Height);
	}

	[Fact]
	public void Measure_InvisibleChildren_AreSkipped()
	{
		var layout = new TestUniformItemsLayout();
		layout.AddChild(new TestView(100, 50));
		layout.AddChild(new TestView(100, 50) { IsVisible = false });
		var manager = new UniformItemsLayoutManager(layout);

		var result = manager.Measure(500, 500);

		Assert.Equal(100, result.Width);
		Assert.Equal(50, result.Height);
	}

	[Fact]
	public void ArrangeChildren_EmptyLayout_ReturnsBoundsSize()
	{
		var layout = new TestUniformItemsLayout();
		var manager = new UniformItemsLayoutManager(layout);

		var result = manager.ArrangeChildren(new Rect(0, 0, 300, 300));

		Assert.Equal(300, result.Width);
		Assert.Equal(300, result.Height);
	}

	[Fact]
	public void ArrangeChildren_SingleChild_ArrangesAtOrigin()
	{
		var child = new TestView(100, 50);
		var layout = new TestUniformItemsLayout();
		layout.AddChild(child);
		var manager = new UniformItemsLayoutManager(layout);

		manager.Measure(500, 500);
		manager.ArrangeChildren(new Rect(0, 0, 300, 300));

		Assert.Equal(0, child.ArrangedRect.Left);
		Assert.Equal(0, child.ArrangedRect.Top);
	}

	[Fact]
	public void ArrangeChildren_MultipleChildren_GridLayout()
	{
		var child1 = new TestView(100, 50);
		var child2 = new TestView(100, 50);
		var layout = new TestUniformItemsLayout();
		layout.AddChild(child1);
		layout.AddChild(child2);
		var manager = new UniformItemsLayoutManager(layout);

		manager.Measure(200, 500);
		manager.ArrangeChildren(new Rect(0, 0, 200, 100));

		Assert.Equal(0, child1.ArrangedRect.Left);
		Assert.Equal(100, child2.ArrangedRect.Left);
	}

	[Fact]
	public void ArrangeChildren_WithPadding_OffsetsChildren()
	{
		var child = new TestView(100, 50);
		var layout = new TestUniformItemsLayout
		{
			Padding = new Thickness(15)
		};
		layout.AddChild(child);
		var manager = new UniformItemsLayoutManager(layout);

		manager.Measure(500, 500);
		manager.ArrangeChildren(new Rect(0, 0, 300, 300));

		Assert.Equal(15, child.ArrangedRect.Left);
		Assert.Equal(15, child.ArrangedRect.Top);
	}
}

sealed partial class TestView : View
{
	readonly double measureWidth;
	readonly double measureHeight;

	public TestView(double measureWidth, double measureHeight)
	{
		this.measureWidth = measureWidth;
		this.measureHeight = measureHeight;
	}

	public Rect ArrangedRect { get; private set; }

	protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
	{
		return new Size(
			Math.Min(measureWidth, widthConstraint),
			Math.Min(measureHeight, heightConstraint));
	}

	protected override Size ArrangeOverride(Rect bounds)
	{
		ArrangedRect = bounds;
		return bounds.Size;
	}
}

sealed partial class TestDockLayout : Layout, IDockLayout
{
	readonly Dictionary<IView, DockPosition> dockPositions = [];

	public bool ShouldExpandLastChild { get; set; } = true;

	public double HorizontalSpacing { get; set; }

	public double VerticalSpacing { get; set; }

	public void AddChild(IView child, DockPosition position)
	{
		Add(child);
		dockPositions[child] = position;
	}

	public void Add(IView view, DockPosition position)
	{
		Add(view);
		dockPositions[view] = position;
	}

	public DockPosition GetDockPosition(IView view)
	{
		return dockPositions.GetValueOrDefault(view, DockPosition.None);
	}

	protected override ILayoutManager CreateLayoutManager() => new DockLayoutManager(this);
}

sealed partial class TestUniformItemsLayout : Layout, IUniformItemsLayout
{
	public int MaxColumns { get; set; } = int.MaxValue;

	public int MaxRows { get; set; } = int.MaxValue;

	public void AddChild(IView child)
	{
		Add(child);
	}

	protected override ILayoutManager CreateLayoutManager() => new UniformItemsLayoutManager(this);
}