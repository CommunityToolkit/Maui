using CommunityToolkit.Maui.Behaviors;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class ImpliedOrderGridBehaviorTests() : BaseBehaviorTest<ImpliedOrderGridBehavior, Grid>(new ImpliedOrderGridBehavior(), [])
{
	[Fact]
	public void CorrectRowColumnAssignment()
	{
		var grid = new Grid();
		grid.Behaviors.Add(new ImpliedOrderGridBehavior());

		grid.RowDefinitions.Add(new RowDefinition());
		grid.RowDefinitions.Add(new RowDefinition());
		grid.RowDefinitions.Add(new RowDefinition());
		grid.RowDefinitions.Add(new RowDefinition());
		grid.RowDefinitions.Add(new RowDefinition());

		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition());

		// R0C0
		AssertExpectedCoordinates(grid, new Label(), 0, 0);

		// R0C1
		AssertExpectedCoordinates(grid, new Label(), 0, 1);

		// R0C2
		AssertExpectedCoordinates(grid, new Label(), 0, 2);

		// R1C0
		var columnSpanLabel = new Label();
		Grid.SetColumnSpan(columnSpanLabel, 2);
		AssertExpectedCoordinates(grid, columnSpanLabel, 1, 0);

		// R1C2
		AssertExpectedCoordinates(grid, new Label(), 1, 2);

		// R2C0
		var rowSpanLabel = new Label();
		Grid.SetRowSpan(rowSpanLabel, 2);
		AssertExpectedCoordinates(grid, rowSpanLabel, 2, 0);

		// R2C1
		AssertExpectedCoordinates(grid, new Label(), 2, 1);

		// R2C2
		AssertExpectedCoordinates(grid, new Label(), 2, 2);

		// R3C1
		AssertExpectedCoordinates(grid, new Label(), 3, 1);

		// R0C0 Manual Assignment
		var manualSetToUsedCellLabel = new Label();
		Grid.SetRow(manualSetToUsedCellLabel, 0);
		Grid.SetColumn(manualSetToUsedCellLabel, 0);
		AssertExpectedCoordinates(grid, manualSetToUsedCellLabel, 0, 0);

		// R3C2
		AssertExpectedCoordinates(grid, new Label(), 3, 2);

		// R4C1
		var manualSetToCellLabel = new Label();
		Grid.SetRow(manualSetToCellLabel, 4);
		Grid.SetColumn(manualSetToCellLabel, 1);
		AssertExpectedCoordinates(grid, manualSetToCellLabel, 4, 1);

		// R4C0
		AssertExpectedCoordinates(grid, new Label(), 4, 0);
	}

	[Fact]
	public void ThrowsOnManualAssignmentToUsedCell()
	{
		var grid = CreateExceptionTestGrid();

		// R0C0
		grid.Children.Add(new Label());

		// Manual R0C)
		var throwLabel = new Label();
		Grid.SetColumn(throwLabel, 0);
		Grid.SetRow(throwLabel, 0);

		Assert.Throws<Exception>(() => grid.Children.Add(throwLabel));
	}

	[Fact]
	public void ThrowsOnCellsExceeded()
	{
		var grid = CreateExceptionTestGrid();

		// R0C0
		grid.Children.Add(new Label());

		// R0C1
		grid.Children.Add(new Label());

		// R1C0
		grid.Children.Add(new Label());

		// R1C1
		grid.Children.Add(new Label());

		// Throws
		Assert.Throws<Exception>(() => grid.Children.Add(new Label()));
	}

	[Fact]
	public void ThrowsOnSpanExceedsColumns()
	{
		var grid = CreateExceptionTestGrid();

		var throwLabel = new Label();
		Grid.SetColumnSpan(throwLabel, 10);

		Assert.Throws<Exception>(() => grid.Children.Add(throwLabel));
	}

	[Fact]
	public void ThrowsOnSpanExceedsRows()
	{
		var grid = CreateExceptionTestGrid();

		var throwLabel = new Label();
		Grid.SetRowSpan(throwLabel, 10);

		Assert.Throws<Exception>(() => grid.Children.Add(throwLabel));
	}

	static Grid CreateExceptionTestGrid()
	{
		var grid = new Grid();
		grid.Behaviors.Add(new ImpliedOrderGridBehavior { ThrowOnLayoutWarning = true });

		grid.RowDefinitions.Add(new RowDefinition());
		grid.RowDefinitions.Add(new RowDefinition());

		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition());

		return grid;
	}

	static void AssertExpectedCoordinates(Grid grid, View view, int row, int column)
	{
		grid.Children.Add(view);
		Assert.Equal(row, Grid.GetRow(view));
		Assert.Equal(column, Grid.GetColumn(view));
	}
}