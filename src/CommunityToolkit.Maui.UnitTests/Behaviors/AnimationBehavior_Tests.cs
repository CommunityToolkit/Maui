using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Microsoft.Maui.Controls;
using System.Linq;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class AnimationBehavior_Tests : BaseTest
{
	[Fact]
	public void TabGestureRecognizerAttachedWhenNoEventSpecified()
	{
		var boxView = new BoxView();
		boxView.Behaviors.Add(new AnimationBehavior());
		var gestureRecognizers = boxView.GestureRecognizers.ToList();
		
		Assert.Single(gestureRecognizers);
		Assert.IsType<TapGestureRecognizer>(gestureRecognizers[0]);
	}

	[Fact]
	public void TabGestureRecognizerNotAttachedWhenEventSpecified()
	{
		var boxView = new BoxView();
		boxView.Behaviors.Add(new AnimationBehavior()
		{
			EventName = nameof(BoxView.Focused),
		});
		var gestureRecognizers = boxView.GestureRecognizers.ToList();

		Assert.Empty(gestureRecognizers);
	}

	[Fact]
	public void TabGestureRecognizerNotAttachedWhenViewIsInputView()
	{
		var entry = new Entry();
		entry.Behaviors.Add(new AnimationBehavior()
		{
			EventName = nameof(Entry.Focused),
		});
		var gestureRecognizers = entry.GestureRecognizers.ToList();

		Assert.Empty(gestureRecognizers);
	}

	[Fact]
	public void CommandIsInvokedOnlyOneTimePerEvent()
	{
		var mockView = new MockEventView();

		var commandInvokedCount = 0;
		mockView.Behaviors.Add(new AnimationBehavior
		{
			EventName = nameof(MockEventView.Event),
			Command = new Command(() => commandInvokedCount++),
		});


		mockView.Event += (sender, args) => { };
		mockView.InvokeEvent();

		Assert.Equal(1, commandInvokedCount);
	}

	[Fact]
	public void AnimateCommandStartsAnimation()
	{
		var mockAnimation = new MockAnimationType();
		var behavior = new AnimationBehavior
		{
			AnimationType = mockAnimation
		};

		new Label
		{
			Behaviors = { behavior }
		};

		behavior.AnimateCommand.Execute(null);
		
		Assert.True(mockAnimation.HasAnimated);
	}
}
