using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

public sealed class MockDrawingViewHandler(IPropertyMapper mapper) : ViewHandler<IDrawingView, DrawingView>(mapper), IDrawingViewHandler
{
	IDrawingLineAdapter adapter = new DrawingLineAdapter();

	public static readonly PropertyMapper<IDrawingView, MockDrawingViewHandler> DrawingViewPropertyMapper = new()
	{
		[nameof(IDrawingView.LineWidth)] = MapLineWidth,
		[nameof(IDrawingView.LineColor)] = MapLineColor,
		[nameof(IDrawingView.ShouldClearOnFinish)] = MapShouldSmoothPathWhenDrawn,
		[nameof(IDrawingView.IsMultiLineModeEnabled)] = MapIsMultiLineModeEnabled,
		[nameof(IDrawingView.DrawAction)] = MapDrawAction
	};

	public MockDrawingViewHandler() : this(DrawingViewPropertyMapper)
	{
	}

	public List<MauiDrawingLine> Lines { get; } = [];
	public int MapLineWidthCount { get; private set; }
	public int MapLineColorCount { get; private set; }
	public int MapShouldSmoothPathWhenDrawnCount { get; private set; }
	public int MapIsMultiLineModeEnabledCount { get; private set; }
	public int MapDrawCount { get; private set; }

	/// <inheritdoc />
	public void SetDrawingLineAdapter(IDrawingLineAdapter drawingLineAdapter)
	{
		adapter = drawingLineAdapter;
	}

	/// <inheritdoc />
	protected override DrawingView CreatePlatformView() => new();

	/// <inheritdoc />
	protected override void ConnectHandler(DrawingView platformView)
	{
		base.ConnectHandler(platformView);
		VirtualView.Lines.CollectionChanged += Lines_CollectionChanged;
	}

	/// <inheritdoc />
	protected override void DisconnectHandler(DrawingView platformView)
	{
		base.DisconnectHandler(platformView);
		VirtualView.Lines.CollectionChanged -= Lines_CollectionChanged;
	}

	static void MapLineWidth(MockDrawingViewHandler arg1, IDrawingView arg2)
	{
		arg1.MapLineWidthCount++;
	}

	static void MapLineColor(MockDrawingViewHandler arg1, IDrawingView arg2)
	{
		arg1.MapLineColorCount++;
	}

	static void MapShouldSmoothPathWhenDrawn(MockDrawingViewHandler arg1, IDrawingView arg2)
	{
		arg1.MapShouldSmoothPathWhenDrawnCount++;
	}

	static void MapIsMultiLineModeEnabled(MockDrawingViewHandler arg1, IDrawingView arg2)
	{
		arg1.MapIsMultiLineModeEnabledCount++;
	}

	static void MapDrawAction(MockDrawingViewHandler arg1, IDrawingView arg2)
	{
		arg1.MapDrawCount++;
	}

	void Lines_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
	{
		Lines.Clear();
		foreach (var line in VirtualView.Lines)
		{
			Lines.Add(new MauiDrawingLine
			{
				ShouldSmoothPathWhenDrawn = line.ShouldSmoothPathWhenDrawn,
				Granularity = line.Granularity,
				LineWidth = line.LineWidth,
				LineColor = line.LineColor,
				Points = line.Points
			});
		}

		if (Lines.Count > 0)
		{
			var drawingLine = adapter.ConvertMauiDrawingLine(Lines.Last());
			VirtualView.OnDrawingLineCompleted(drawingLine);
		}
	}
}

sealed class MockDrawingLineAdapter : IDrawingLineAdapter
{
	public IDrawingLine ConvertMauiDrawingLine(MauiDrawingLine mauiDrawingLine)
	{
		return new MockDrawingLine
		{
			ShouldSmoothPathWhenDrawn = mauiDrawingLine.ShouldSmoothPathWhenDrawn,
			LineWidth = mauiDrawingLine.LineWidth,
			Granularity = mauiDrawingLine.Granularity,
			LineColor = mauiDrawingLine.LineColor,
			Points = mauiDrawingLine.Points.ToObservableCollection()
		};
	}
}

sealed class MockDrawingLine : IDrawingLine
{
	public int Granularity { get; set; }
	public Color LineColor { get; set; } = Colors.Blue;
	public float LineWidth { get; set; }
	public ObservableCollection<PointF> Points { get; set; } = [];
	public bool ShouldSmoothPathWhenDrawn { get; set; }
	public ValueTask<Stream> GetImageStream(double imageSizeWidth, double imageSizeHeight, Paint background, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		return ValueTask.FromResult(Stream.Null);
	}
}