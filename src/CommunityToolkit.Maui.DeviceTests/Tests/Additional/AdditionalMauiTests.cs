using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.ImageSources;
using CommunityToolkit.Maui.Layouts;
using Xunit;

namespace CommunityToolkit.Maui.DeviceTests.Tests.Additional;

public class GravatarImageSourceTests
{
	[Fact]
	public void GravatarImageSource_DefaultProperties()
	{
		var source = new GravatarImageSource();

		Assert.Null(source.Email);
		Assert.Equal(DefaultImage.MysteryPerson, source.Image);
		Assert.True(source.CachingEnabled);
		Assert.Equal(TimeSpan.FromDays(1), source.CacheValidity);
		Assert.True(source.IsEmpty);
	}

	[Fact]
	public void GravatarImageSource_SetEmail_IsNotEmpty()
	{
		var source = new GravatarImageSource
		{
			Email = "test@example.com"
		};

		Assert.False(source.IsEmpty);
	}

	[Fact]
	public void GravatarImageSource_SetEmail_UpdatesUri()
	{
		var source = new GravatarImageSource
		{
			Email = "test@example.com"
		};

		Assert.NotNull(source.Uri);
		Assert.Contains("gravatar.com/avatar/", source.Uri.ToString());
	}

	[Fact]
	public void GravatarImageSource_CanSetImage()
	{
		var source = new GravatarImageSource
		{
			Image = DefaultImage.Robohash
		};

		Assert.Equal(DefaultImage.Robohash, source.Image);
	}

	[Fact]
	public void GravatarImageSource_CanDisableCaching()
	{
		var source = new GravatarImageSource
		{
			CachingEnabled = false
		};

		Assert.False(source.CachingEnabled);
	}

	[Fact]
	public void GravatarImageSource_CanSetCacheValidity()
	{
		var source = new GravatarImageSource
		{
			CacheValidity = TimeSpan.FromHours(2)
		};

		Assert.Equal(TimeSpan.FromHours(2), source.CacheValidity);
	}

	[Fact]
	public void DefaultImage_HasExpectedValues()
	{
		Assert.Equal(0, (int)DefaultImage.MysteryPerson);
		Assert.Equal(1, (int)DefaultImage.FileNotFound);
		Assert.Equal(2, (int)DefaultImage.Identicon);
		Assert.Equal(3, (int)DefaultImage.MonsterId);
		Assert.Equal(4, (int)DefaultImage.Wavatar);
		Assert.Equal(5, (int)DefaultImage.Retro);
		Assert.Equal(6, (int)DefaultImage.Robohash);
		Assert.Equal(7, (int)DefaultImage.Blank);
	}
}

public class FadeAnimationTests
{
	[Fact]
	public void FadeAnimation_DefaultProperties()
	{
		var animation = new FadeAnimation();

		Assert.True(animation.Length > 0);
		Assert.NotNull(animation.Easing);
	}

	[Fact]
	public void FadeAnimation_CanSetOpacity()
	{
		var animation = new FadeAnimation
		{
			Opacity = 0.5
		};

		Assert.Equal(0.5, animation.Opacity);
	}

	[Fact]
	public void FadeAnimation_CanSetLength()
	{
		var animation = new FadeAnimation
		{
			Length = 500
		};

		Assert.Equal(500u, animation.Length);
	}

	[Fact]
	public void FadeAnimation_CanSetEasing()
	{
		var animation = new FadeAnimation
		{
			Easing = Easing.CubicOut
		};

		Assert.Equal(Easing.CubicOut, animation.Easing);
	}
}

public class DockLayoutTests
{
	[Fact]
	public void DockLayout_DefaultProperties()
	{
		var dockLayout = new DockLayout();

		Assert.True(dockLayout.ShouldExpandLastChild);
		Assert.Equal(0, dockLayout.HorizontalSpacing);
		Assert.Equal(0, dockLayout.VerticalSpacing);
	}

	[Fact]
	public void DockLayout_CanSetSpacing()
	{
		var dockLayout = new DockLayout
		{
			HorizontalSpacing = 10,
			VerticalSpacing = 5
		};

		Assert.Equal(10, dockLayout.HorizontalSpacing);
		Assert.Equal(5, dockLayout.VerticalSpacing);
	}

	[Fact]
	public void DockLayout_CanSetShouldExpandLastChild()
	{
		var dockLayout = new DockLayout
		{
			ShouldExpandLastChild = false
		};

		Assert.False(dockLayout.ShouldExpandLastChild);
	}

	[Fact]
	public void DockLayout_GetSetDockPosition()
	{
		var view = new Label();
		var dockLayout = new DockLayout();

		DockLayout.SetDockPosition(view, DockPosition.Right);

		Assert.Equal(DockPosition.Right, DockLayout.GetDockPosition(view));
	}

	[Fact]
	public void DockLayout_DefaultDockPosition_IsNone()
	{
		var view = new Label();

		Assert.Equal(DockPosition.None, DockLayout.GetDockPosition(view));
	}

	[Fact]
	public void DockLayout_Add_WithPosition()
	{
		var dockLayout = new DockLayout();
		var view = new Label();

		dockLayout.Add(view, DockPosition.Left);

		Assert.Contains(view, dockLayout.Children);
		Assert.Equal(DockPosition.Left, DockLayout.GetDockPosition(view));
	}
}

public class UniformItemsLayoutTests
{
	[Fact]
	public void UniformItemsLayout_DefaultProperties()
	{
		var layout = new UniformItemsLayout();

		Assert.Equal(int.MaxValue, layout.MaxRows);
		Assert.Equal(int.MaxValue, layout.MaxColumns);
	}

	[Fact]
	public void UniformItemsLayout_CanSetMaxRows()
	{
		var layout = new UniformItemsLayout
		{
			MaxRows = 3
		};

		Assert.Equal(3, layout.MaxRows);
	}

	[Fact]
	public void UniformItemsLayout_CanSetMaxColumns()
	{
		var layout = new UniformItemsLayout
		{
			MaxColumns = 4
		};

		Assert.Equal(4, layout.MaxColumns);
	}

	[Fact]
	public void UniformItemsLayout_MaxRowsLessThanOne_Throws()
	{
		var layout = new UniformItemsLayout();
		var thrown = false;

		try
		{
			layout.MaxRows = 0;
		}
		catch (ArgumentOutOfRangeException)
		{
			thrown = true;
		}

		Assert.True(thrown);
	}

	[Fact]
	public void UniformItemsLayout_MaxColumnsLessThanOne_Throws()
	{
		var layout = new UniformItemsLayout();
		var thrown = false;

		try
		{
			layout.MaxColumns = 0;
		}
		catch (ArgumentOutOfRangeException)
		{
			thrown = true;
		}

		Assert.True(thrown);
	}
}

public class StateContainerTests
{
	[Fact]
	public void StateContainer_GetSetCurrentState()
	{
		var layout = new Grid();

		// StateContainer requires StateViews to be configured before setting CurrentState,
		// otherwise it throws "Unable to determine StateView for State".
		var loadingView = new Label { Text = "Loading" };
		StateView.SetStateKey(loadingView, "Loading");
		StateContainer.SetStateViews(layout, [loadingView]);

		StateContainer.SetCurrentState(layout, "Loading");

		Assert.Equal("Loading", StateContainer.GetCurrentState(layout));
	}

	[Fact]
	public void StateContainer_DefaultCurrentState_IsNull()
	{
		var layout = new Grid();

		Assert.Null(StateContainer.GetCurrentState(layout));
	}

	[Fact]
	public void StateContainer_GetCanStateChange_DefaultTrue()
	{
		var layout = new Grid();

		Assert.True(StateContainer.GetCanStateChange(layout));
	}

	[Fact]
	public void StateContainer_CanStateChangeProperty_Exists()
	{
		Assert.NotNull(StateContainer.CanStateChangeProperty);
	}

	[Fact]
	public void StateContainer_StateViews_DefaultEmpty()
	{
		var layout = new Grid();
		var stateViews = StateContainer.GetStateViews(layout);

		Assert.NotNull(stateViews);
		Assert.Empty(stateViews);
	}
}

public class StateViewTests
{
	[Fact]
	public void StateView_GetSetStateKey()
	{
		var view = new Label();

		StateView.SetStateKey(view, "Error");

		Assert.Equal("Error", StateView.GetStateKey(view));
	}

	[Fact]
	public void StateView_DefaultStateKey()
	{
		var view = new Label();
		var stateKey = StateView.GetStateKey(view);

		Assert.NotNull(stateKey);
	}
}

public class ExpandedChangedEventArgsTests
{
	[Fact]
	public void ExpandedChangedEventArgs_SetsIsExpanded()
	{
		var args = new ExpandedChangedEventArgs(true);

		Assert.True(args.IsExpanded);
	}

	[Fact]
	public void ExpandedChangedEventArgs_FalseValue()
	{
		var args = new ExpandedChangedEventArgs(false);

		Assert.False(args.IsExpanded);
	}
}

public class RatingChangedEventArgsTests
{
	[Fact]
	public void RatingChangedEventArgs_SetsRating()
	{
		var args = new RatingChangedEventArgs(3.5);

		Assert.Equal(3.5, args.Rating);
	}

	[Fact]
	public void RatingChangedEventArgs_ZeroRating()
	{
		var args = new RatingChangedEventArgs(0);

		Assert.Equal(0, args.Rating);
	}
}