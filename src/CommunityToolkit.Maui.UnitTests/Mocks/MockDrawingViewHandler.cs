using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Core.Views;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

public class MockDrawingViewHandler : ViewHandler<IDrawingView, object>, IDrawingViewHandler
{
	DrawingLineAdapter adapter;

	public static readonly PropertyMapper<IDrawingView, MockDrawingViewHandler> DrawingViewPropertyMapper = new(ViewMapper)
	{
		[nameof(IDrawingView.LineWidth)] = MapLineWidth,
		[nameof(IDrawingView.LineColor)] = MapLineColor,
		[nameof(IDrawingView.ClearOnFinish)] = MapClearOnFinish,
		[nameof(IDrawingView.MultiLineMode)] = MapMultiLineMode,
		[nameof(IDrawingView.DrawAction)] = MapDrawAction
	};

	public MockDrawingViewHandler() : this(DrawingViewPropertyMapper)
	{
	}

	public MockDrawingViewHandler(IPropertyMapper mapper) : base(mapper)
	{
		SetDrawingLineAdapter();
	}

	/// <inheritdoc />
	protected override void ConnectHandler(object nativeView)
	{
		base.ConnectHandler(nativeView);
		VirtualView.Lines.CollectionChanged += Lines_CollectionChanged;
	}

	/// <inheritdoc />
	protected override void DisconnectHandler(object nativeView)
	{
		base.DisconnectHandler(nativeView);
		VirtualView.Lines.CollectionChanged -= Lines_CollectionChanged;
	}

	public int MapLineWidthCount { get; private set; }
	public int MapLineColorCount { get; private set; }
	public int MapClearOnFinishCount { get; private set; }
	public int MapMultiLineModeCount { get; private set; }
	public int MapDrawCount { get; private set; }
	public List<MauiDrawingLine> Lines { get; } = new();

	static void MapLineWidth(MockDrawingViewHandler arg1, IDrawingView arg2)
	{
		arg1.MapLineWidthCount++;
	}

	static void MapLineColor(MockDrawingViewHandler arg1, IDrawingView arg2)
	{
		arg1.MapLineColorCount++;
	}

	static void MapClearOnFinish(MockDrawingViewHandler arg1, IDrawingView arg2)
	{
		arg1.MapClearOnFinishCount++;
	}

	static void MapMultiLineMode(MockDrawingViewHandler arg1, IDrawingView arg2)
	{
		arg1.MapMultiLineModeCount++;
	}

	static void MapDrawAction(MockDrawingViewHandler arg1, IDrawingView arg2)
	{
		arg1.MapDrawCount++;
	}

	protected override object CreatePlatformView()
	{
		return new object();
	}

	void Lines_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
	{
		Lines.Clear();
		foreach (var line in VirtualView.Lines)
		{
			Lines.Add(new MauiDrawingLine()
			{
				EnableSmoothedPath = line.EnableSmoothedPath,
				Granularity = line.Granularity,
				LineWidth = line.LineWidth
			});
		}
		var drawingLine = adapter.GetDrawingLine(Lines.Last());
		VirtualView.DrawingLineCompleted(drawingLine);
	}

	[MemberNotNull(nameof(adapter))]
	public void SetDrawingLineAdapter(DrawingLineAdapter? drawingLineAdapter = null)
	{
		adapter = drawingLineAdapter ?? new DrawingLineAdapter();
	}
}

public class MockDrawingLineAdapter : DrawingLineAdapter
{
	public override IDrawingLine GetDrawingLine(MauiDrawingLine mauiDrawingLine)
	{
		return new MockDrawingLine
		{
			EnableSmoothedPath = mauiDrawingLine.EnableSmoothedPath,
			LineWidth = mauiDrawingLine.LineWidth,
			Granularity = mauiDrawingLine.Granularity,
			LineColor = mauiDrawingLine.LineColor,
			Points = mauiDrawingLine.Points.ToObservableCollection()
		};
	}
}

public class MockDrawingLine : IDrawingLine
{
	public int Granularity { get; set; }
	public Color LineColor { get; set; } = Colors.Blue;
	public float LineWidth { get; set; }
	public ObservableCollection<PointF> Points { get; set; } = new();
	public bool EnableSmoothedPath { get; set; }
	public ValueTask<Stream> GetImageStream(double imageSizeWidth, double imageSizeHeight, Color backgroundColor)
	{
		return ValueTask.FromResult(Stream.Null);
	}
}