using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;
using Xunit;

namespace CommunityToolkit.Maui.DeviceTests.Tests.Core;

public class TouchStatusEnumTests
{
	[Theory]
	[InlineData(TouchStatus.Started, 0)]
	[InlineData(TouchStatus.Completed, 1)]
	[InlineData(TouchStatus.Canceled, 2)]
	public void TouchStatus_HasExpectedValues(TouchStatus status, int expected)
	{
		Assert.Equal(expected, (int)status);
	}
}

public class TouchStateEnumTests
{
	[Theory]
	[InlineData(TouchState.Default, 0)]
	[InlineData(TouchState.Pressed, 1)]
	public void TouchState_HasExpectedValues(TouchState state, int expected)
	{
		Assert.Equal(expected, (int)state);
	}
}

public class TouchInteractionStatusEnumTests
{
	[Theory]
	[InlineData(TouchInteractionStatus.Started, 0)]
	[InlineData(TouchInteractionStatus.Completed, 1)]
	public void TouchInteractionStatus_HasExpectedValues(TouchInteractionStatus status, int expected)
	{
		Assert.Equal(expected, (int)status);
	}
}

public class StatusBarStyleEnumTests
{
	[Theory]
	[InlineData(StatusBarStyle.Default, 0)]
	[InlineData(StatusBarStyle.LightContent, 1)]
	[InlineData(StatusBarStyle.DarkContent, 2)]
	public void StatusBarStyle_HasExpectedValues(StatusBarStyle style, int expected)
	{
		Assert.Equal(expected, (int)style);
	}
}

public class HoverStatusEnumTests
{
	[Theory]
	[InlineData(HoverStatus.Entered, 0)]
	[InlineData(HoverStatus.Exited, 1)]
	public void HoverStatus_HasExpectedValues(HoverStatus status, int expected)
	{
		Assert.Equal(expected, (int)status);
	}
}

public class HoverStateEnumTests
{
	[Theory]
	[InlineData(HoverState.Default, 0)]
	[InlineData(HoverState.Hovered, 1)]
	public void HoverState_HasExpectedValues(HoverState state, int expected)
	{
		Assert.Equal(expected, (int)state);
	}
}

public class ExpandDirectionEnumTests
{
	[Theory]
	[InlineData(ExpandDirection.Down, 0)]
	[InlineData(ExpandDirection.Up, 1)]
	public void ExpandDirection_HasExpectedValues(ExpandDirection direction, int expected)
	{
		Assert.Equal(expected, (int)direction);
	}
}

public class DockPositionEnumTests
{
	[Theory]
	[InlineData(DockPosition.None, 0)]
	[InlineData(DockPosition.Top, 1)]
	[InlineData(DockPosition.Left, 2)]
	[InlineData(DockPosition.Right, 3)]
	[InlineData(DockPosition.Bottom, 4)]
	public void DockPosition_HasExpectedValues(DockPosition position, int expected)
	{
		Assert.Equal(expected, (int)position);
	}
}

public class DrawingViewOutputOptionEnumTests
{
	[Theory]
	[InlineData(DrawingViewOutputOption.Lines, 0)]
	[InlineData(DrawingViewOutputOption.FullCanvas, 1)]
	public void DrawingViewOutputOption_HasExpectedValues(DrawingViewOutputOption option, int expected)
	{
		Assert.Equal(expected, (int)option);
	}
}

public class NavigationBarStyleEnumTests
{
	[Theory]
	[InlineData(NavigationBarStyle.Default, 0)]
	[InlineData(NavigationBarStyle.LightContent, 1)]
	[InlineData(NavigationBarStyle.DarkContent, 2)]
	public void NavigationBarStyle_HasExpectedValues(NavigationBarStyle style, int expected)
	{
		Assert.Equal(expected, (int)style);
	}
}

public class EventArgsTests
{
	[Fact]
	public void TouchStatusChangedEventArgs_CarriesStatus()
	{
		var args = new TouchStatusChangedEventArgs(TouchStatus.Started);

		Assert.Equal(TouchStatus.Started, args.Status);
	}

	[Fact]
	public void TouchStateChangedEventArgs_CarriesState()
	{
		var args = new TouchStateChangedEventArgs(TouchState.Pressed);

		Assert.Equal(TouchState.Pressed, args.State);
	}

	[Fact]
	public void TouchInteractionStatusChangedEventArgs_CarriesStatus()
	{
		var args = new TouchInteractionStatusChangedEventArgs(TouchInteractionStatus.Completed);

		Assert.Equal(TouchInteractionStatus.Completed, args.TouchInteractionStatus);
	}

	[Fact]
	public void TouchGestureCompletedEventArgs_CarriesParameter()
	{
		var args = new TouchGestureCompletedEventArgs("testParam");

		Assert.Equal("testParam", args.TouchCommandParameter);
	}

	[Fact]
	public void TouchGestureCompletedEventArgs_NullParameter()
	{
		var args = new TouchGestureCompletedEventArgs(null);

		Assert.Null(args.TouchCommandParameter);
	}

	[Fact]
	public void HoverStatusChangedEventArgs_CarriesStatus()
	{
		var args = new HoverStatusChangedEventArgs(HoverStatus.Entered);

		Assert.Equal(HoverStatus.Entered, args.Status);
	}

	[Fact]
	public void HoverStateChangedEventArgs_CarriesState()
	{
		var args = new HoverStateChangedEventArgs(HoverState.Hovered);

		Assert.Equal(HoverState.Hovered, args.State);
	}

	[Fact]
	public void ExpandedChangedEventArgs_CarriesIsExpanded()
	{
		var args = new ExpandedChangedEventArgs(true);

		Assert.True(args.IsExpanded);
	}

	[Fact]
	public void LongPressCompletedEventArgs_CarriesParameter()
	{
		var args = new LongPressCompletedEventArgs(42);

		Assert.Equal(42, args.LongPressCommandParameter);
	}

	[Fact]
	public void RatingChangedEventArgs_CarriesRating()
	{
		var args = new RatingChangedEventArgs(3.5);

		Assert.Equal(3.5, args.Rating);
	}
}

public class SnackbarOptionsTests
{
	[Fact]
	public void SnackbarOptions_HasDefaultValues()
	{
		var options = new SnackbarOptions();

		Assert.NotNull(options);
	}

	[Fact]
	public void SnackbarOptions_CanSetProperties()
	{
		var options = new SnackbarOptions
		{
			CharacterSpacing = 1.5,
			Font = Microsoft.Maui.Font.SystemFontOfSize(20),
			TextColor = Colors.Red,
			BackgroundColor = Colors.Blue,
			CornerRadius = new CornerRadius(10),
		};

		Assert.Equal(1.5, options.CharacterSpacing);
		Assert.Equal(Colors.Red, options.TextColor);
		Assert.Equal(Colors.Blue, options.BackgroundColor);
		Assert.Equal(new CornerRadius(10), options.CornerRadius);
	}
}

public class FolderRecordTests
{
	[Fact]
	public void Folder_Record_HasExpectedProperties()
	{
		var folder = new Folder("/test/path", "TestFolder");

		Assert.Equal("/test/path", folder.Path);
		Assert.Equal("TestFolder", folder.Name);
	}

	[Fact]
	public void Folder_Record_Equality()
	{
		var folder1 = new Folder("/path", "Name");
		var folder2 = new Folder("/path", "Name");

		Assert.Equal(folder1, folder2);
	}

	[Fact]
	public void Folder_Record_Inequality()
	{
		var folder1 = new Folder("/path1", "Name1");
		var folder2 = new Folder("/path2", "Name2");

		Assert.NotEqual(folder1, folder2);
	}
}
