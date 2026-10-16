using System.Collections;
using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class DrawingViewServiceTests
{
	// Catches: a token cancelled while the lines are drawn is not observed after drawing
	[Fact]
	public async Task TokenCancelledWhileLinesAreDrawnThrowsOperationCanceledException()
	{
		using var cancellation = new CancellationTokenSource();
		var lines = new List<IDrawingLine> { new CancellingLine(cancellation) };
		var options = ImageLineOptions.FullCanvas(lines, new Size(50, 50), null, new Size(50, 50));

		await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await DrawingViewService.GetImageStream(options, cancellation.Token));
	}

	// Catches: a token cancelled while the points are drawn is not observed after drawing
	[Fact]
	public async Task TokenCancelledWhilePointsAreDrawnThrowsOperationCanceledException()
	{
		using var cancellation = new CancellationTokenSource();
		var points = new CancellingPointList(cancellation, ToPoints((5, 5), (30, 30)));
		var options = new ImagePointOptions(points, new Size(50, 50), 4, red, null, new Size(50, 50));

		await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await DrawingViewService.GetImageStream(options, cancellation.Token));
	}

	// Cancels the token when the service first reads the line's color, i.e. while it draws, after the first token check.
	sealed class CancellingLine(CancellationTokenSource cancellation) : IDrawingLine
	{
		public int Granularity { get; set; } = 5;

		public Color LineColor
		{
			get
			{
				cancellation.Cancel();
				return Colors.Red;
			}
			set { }
		}

		public float LineWidth { get; set; } = 4;

		public ObservableCollection<PointF> Points { get; set; } = [new(5, 5), new(30, 30)];

		public bool ShouldSmoothPathWhenDrawn { get; set; }

		public ValueTask<Stream> GetImageStream(double desiredSizeWidth, double desiredSizeHeight, Paint background, Size? canvasSize = null, CancellationToken token = default) =>
			throw new NotSupportedException();
	}

	// Cancels the token when the service first enumerates the points.
	sealed class CancellingPointList(CancellationTokenSource cancellation, IEnumerable<PointF> points) : IList<PointF>
	{
		readonly List<PointF> items = [.. points];

		public int Count => items.Count;

		public bool IsReadOnly => false;

		public PointF this[int index]
		{
			get => items[index];
			set => items[index] = value;
		}

		public IEnumerator<PointF> GetEnumerator()
		{
			cancellation.Cancel();
			return items.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public void Add(PointF item) => items.Add(item);

		public void Clear() => items.Clear();

		public bool Contains(PointF item) => items.Contains(item);

		public void CopyTo(PointF[] array, int arrayIndex) => items.CopyTo(array, arrayIndex);

		public int IndexOf(PointF item) => items.IndexOf(item);

		public void Insert(int index, PointF item) => items.Insert(index, item);

		public bool Remove(PointF item) => items.Remove(item);

		public void RemoveAt(int index) => items.RemoveAt(index);
	}
}