using System.Diagnostics.CodeAnalysis;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// The <see cref="ImpliedOrderGridBehavior"/> enables you to automatically assign a <see cref="Grid"/> row and column to a view based on the order the view is added to the <see cref="Grid"/>. You only need to setup the row and column definitions and then add children to the <see cref="Grid"/>. You may still assign RowSpan and ColumnSpan to views and their values will be taken into account when assigning a row and column to a view. If a view has a user defined row or column value it will be honored.
/// </summary>
public class ImpliedOrderGridBehavior : BaseBehavior<Grid>
{
	bool[][]? _usedMatrix;
	int _rowCount;
	int _columnCount;

	/// <summary>
	/// When set to true, warnings will throw an exception instead of being logged. Defaults to false.
	/// </summary>
	public bool ThrowOnLayoutWarning { get; set; }

	/// <inheritdoc />
	protected override void OnAttachedTo(Grid bindable)
	{
		base.OnAttachedTo(bindable);

		bindable.ChildAdded += OnInternalGridChildAdded;
	}

	/// <inheritdoc />
	protected override void OnDetachingFrom(Grid bindable)
	{
		base.OnDetachingFrom(bindable);

		bindable.ChildAdded -= OnInternalGridChildAdded;
	}

	static void LogWarning(string warning, bool shouldThrowException)
	{
		System.Diagnostics.Debug.WriteLine(warning);

		if (shouldThrowException)
			throw new Exception(warning);
	}

	void OnInternalGridChildAdded(object? sender, ElementEventArgs e) => ProcessElement(e.Element);

	bool[][] InitMatrix()
	{
		ArgumentNullException.ThrowIfNull(View);

		_rowCount = View.RowDefinitions.Count;
		if (_rowCount is 0)
			_rowCount = 1;

		_columnCount = View.ColumnDefinitions.Count;
		if (_columnCount is 0)
			_columnCount = 1;

		var newMatrix = new bool[_rowCount][];

		for (var r = 0; r < _rowCount; r++)
			newMatrix[r] = new bool[_columnCount];

		return newMatrix;
	}

	[MemberNotNull(nameof(_usedMatrix))]
	void FindNextCell(out int rowIndex, out int columnIndex)
	{
		_usedMatrix ??= InitMatrix();

		// Find the first available row
		var row = _usedMatrix.FirstOrDefault(r => r.Any(c => !c));

		// If no row is found, set cell to origin and log
		if (row is null)
		{
			LogWarning("Defined cells exceeded", ThrowOnLayoutWarning);

			rowIndex = Math.Max(_rowCount - 1, 0);
			columnIndex = Math.Max(_columnCount - 1, 0);

			return;
		}
		rowIndex = Array.IndexOf(_usedMatrix, row);

		// Find the first available column
		columnIndex = Array.IndexOf(row, row.FirstOrDefault(c => !c));
	}

	void UpdateUsedCells(int row, int column, int rowSpan, int columnSpan)
	{
		var rowEnd = row + rowSpan;
		var columnEnd = column + columnSpan;

		if (columnEnd > _columnCount)
		{
			columnEnd = _columnCount;
			LogWarning($"View at row {row} column {columnEnd} with column span {columnSpan} exceeds the defined grid columns.", ThrowOnLayoutWarning);
		}

		if (rowEnd > _rowCount)
		{
			rowEnd = _rowCount;
			LogWarning($"View at row {row} column {columnEnd} with row span {rowSpan} exceeds the defined grid rows.", ThrowOnLayoutWarning);
		}

		for (var r = row; r < rowEnd; r++)
		{
			for (var c = column; c < columnEnd; c++)
			{
				if (_usedMatrix?[r][c] ?? throw new InvalidOperationException("Grid Child Not Added"))
					LogWarning($"Cell at row {r} column {c} has already been used.", ThrowOnLayoutWarning);

				_usedMatrix[r][c] = true;
			}
		}
	}

	void ProcessElement(BindableObject view)
	{
		var columnSpan = Grid.GetColumnSpan(view);
		var rowSpan = Grid.GetRowSpan(view);

		FindNextCell(out var row, out var column);

		// Check to see if the user manually assigned a row or column
		if (view.IsSet(Grid.ColumnProperty))
			column = Grid.GetColumn(view);

		if (view.IsSet(Grid.RowProperty))
			row = Grid.GetRow(view);

		UpdateUsedCells(row, column, rowSpan, columnSpan);

		// Set attributes
		view.SetValue(Grid.ColumnProperty, column);
		view.SetValue(Grid.RowProperty, row);
	}
}