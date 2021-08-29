using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Graphics;

namespace CommunityToolkit.Maui.UI.Views
{
    /// <summary>
    /// A Layout that arranges the elements in a honeycomb pattern.
    /// Based on https://github.com/AlexanderSharykin/HexGrid. Created by Alexander Sharykin.
    /// </summary>
    public class HexLayout : Layout<View>
	{
		public static readonly BindableProperty OrientationProperty =
		 BindableProperty.Create(nameof(Orientation), typeof(HexOrientation), typeof(HexLayout), HexOrientation.Vertical,
			 BindingMode.TwoWay, propertyChanged: (bindable, oldvalue, newvalue) => ((HexLayout)bindable).InvalidateMeasure());

		public HexOrientation Orientation
		{
			get => (HexOrientation)GetValue(OrientationProperty);
			set => SetValue(OrientationProperty, value);
		}

		public static readonly BindableProperty ColumnCountProperty =
			BindableProperty.Create(nameof(ColumnCount), typeof(int), typeof(HexLayout), 1,
				BindingMode.TwoWay, propertyChanged: (bindable, oldvalue, newvalue) => ((HexLayout)bindable).InvalidateMeasure());

		public int ColumnCount
		{
			get => (int)GetValue(ColumnCountProperty);
			set => SetValue(ColumnCountProperty, value);
		}

		public static readonly BindableProperty RowCountProperty =
			BindableProperty.Create(nameof(RowCount), typeof(int), typeof(HexLayout), 1,
				BindingMode.TwoWay, propertyChanged: (bindable, oldvalue, newvalue) => ((HexLayout)bindable).InvalidateMeasure());

		public int RowCount
		{
			get => (int)GetValue(RowCountProperty);
			set => SetValue(RowCountProperty, value);
		}

		public static readonly BindableProperty ColumnProperty =
			BindableProperty.Create(nameof(Column), typeof(int), typeof(HexLayout), 1,
				BindingMode.TwoWay, null);

		public int Column
		{
			get => (int)GetValue(ColumnProperty);
			set => SetValue(ColumnProperty, value);
		}

		int GetColumn(VisualElement e) => Math.Min((int)e.GetValue(ColumnProperty), ColumnCount - 1);

		public static readonly BindableProperty RowProperty =
			BindableProperty.Create(nameof(Row), typeof(int), typeof(HexLayout), 1,
				BindingMode.TwoWay);

		public int Row
		{
			get => (int)GetValue(RowProperty);
			set => SetValue(RowProperty, value);
		}

		int GetRow(VisualElement e) => Math.Min((int)e.GetValue(RowProperty), RowCount - 1);

		protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
		{
			var w = widthConstraint;
			var h = heightConstraint;

			if (double.IsInfinity(w) || double.IsInfinity(h))
			{
				h = 0;
				w = 0;

				foreach (var child in Children)
				{
					var childSize = child.Measure(widthConstraint, heightConstraint);

					h = Math.Max(h, childSize.Request.Height);
					w = Math.Max(w, childSize.Request.Width);
				}

				if (Orientation == HexOrientation.Horizontal)
					return new SizeRequest(new Size(w * ((ColumnCount * 3) + 1) / 4, h * ((RowCount * 2) + 1) / 2));

				return new SizeRequest(new Size(w * ((ColumnCount * 2) + 1) / 2, h * ((RowCount * 3) + 1) / 4));
			}

			return Measure(widthConstraint, heightConstraint);
		}

		protected override void LayoutChildren(double x, double y, double width, double height)
		{
			HasShift(out var first, out var last);

			var hexSize = GetHexSize(new Size(width, height));

			double columnWidth, rowHeight;

			if (Orientation == HexOrientation.Horizontal)
			{
				rowHeight = 0.5 * hexSize.Height;
				columnWidth = 0.25 * hexSize.Width;
			}
			else
			{
				rowHeight = 0.25 * hexSize.Height;
				columnWidth = 0.5 * hexSize.Width;
			}

			var elements = Children;
			for (var i = 0; i < elements.Count; i++)
			{
				if (!elements[i].IsVisible)
					continue;

				LayoutChild(elements[i], hexSize, columnWidth, rowHeight, first);
			}
		}

		void LayoutChild(VisualElement element, Size hexSize, double columnWidth, double rowHeight, bool shift)
		{
			var row = GetRow(element);
			var column = GetColumn(element);

			double x;
			double y;

			if (Orientation == HexOrientation.Horizontal)
			{
				x = 3 * columnWidth * column;
				y = rowHeight * ((2 * row) + (column % 2 == 1 ? 1 : 0) + (shift ? -1 : 0));
			}
			else
			{
				x = columnWidth * ((2 * column) + (row % 2 == 1 ? 1 : 0) + (shift ? -1 : 0));
				y = 3 * rowHeight * row;
			}

			LayoutChildIntoBoundingRegion(element, new Rectangle(x, y, hexSize.Width, hexSize.Height));
		}

		void HasShift(out bool first, out bool last)
		{
			if (Orientation == HexOrientation.Horizontal)
				HasRowShift(out first, out last);
			else
				HasColumnShift(out first, out last);
		}

		void HasRowShift(out bool firstRow, out bool lastRow)
		{
			firstRow = lastRow = true;

			var elements = Children;
			for (var i = 0; i < elements.Count && (firstRow || lastRow); i++)
			{
				var e = elements[i];

				if (!e.IsVisible)
					continue;

				var row = GetRow(e);
				var column = GetColumn(e);

				var mod = column % 2;

				if (row == 0 && mod == 0)
					firstRow = false;

				if (row == RowCount - 1 && mod == 1)
					lastRow = false;
			}
		}

		void HasColumnShift(out bool firstColumn, out bool lastColumn)
		{
			firstColumn = lastColumn = true;

			var elements = Children;
			for (var i = 0; i < elements.Count && (firstColumn || lastColumn); i++)
			{
				var e = elements[i];

				if (!e.IsVisible)
					continue;

				var row = GetRow(e);
				var column = GetColumn(e);

				var mod = row % 2;

				if (column == 0 && mod == 0)
					firstColumn = false;

				if (column == ColumnCount - 1 && mod == 1)
					lastColumn = false;
			}
		}

		Size GetHexSize(Size gridSize)
		{
			double minH = 0;
			double minW = 0;

			foreach (var e in Children)
			{
				if (e is VisualElement f)
				{
					if (f.MinimumHeightRequest > minH)
						minH = f.MinimumHeightRequest;

					if (f.MinimumWidthRequest > minW)
						minW = f.MinimumWidthRequest;
				}
			}

			HasShift(out var first, out var last);

			var possibleSize = GetPossibleSize(gridSize);
			var possibleW = possibleSize.Width;
			var possibleH = possibleSize.Height;

			var w = Math.Max(minW, possibleW);
			var h = Math.Max(minH, possibleH);

			return new Size(w, h);
		}

		Size GetPossibleSize(Size gridSize)
		{
			HasShift(out var first, out var last);

			if (Orientation == HexOrientation.Horizontal)
				return GetPossibleSizeHorizontal(gridSize, first, last);

			return GetPossibleSizeVertical(gridSize, first, last);
		}

		Size GetPossibleSizeVertical(Size gridSize, bool first, bool last)
		{
			var columns = (first ? 0 : 1) + (2 * ColumnCount) - (last ? 1 : 0);
			var w = 2 * (gridSize.Width / columns);

			var rows = 1 + (3 * RowCount);
			var h = 4 * (gridSize.Height / rows);

			return new Size(w, h);
		}

		Size GetPossibleSizeHorizontal(Size gridSize, bool first, bool last)
		{
			var columns = 1 + (3 * ColumnCount);
			var w = 4 * (gridSize.Width / columns);

			var rows = (first ? 0 : 1) + (2 * RowCount) - (last ? 1 : 0);
			var h = 2 * (gridSize.Height / rows);

			return new Size(w, h);
		}
	}
}