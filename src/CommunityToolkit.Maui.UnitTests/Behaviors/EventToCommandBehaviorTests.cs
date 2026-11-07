using System.Globalization;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class EventToCommandBehaviorTests() : BaseBehaviorTest<EventToCommandBehavior, VisualElement>(new EventToCommandBehavior(), new MockView())
{
	[Fact]
	public void ArgumentExceptionIfSpecifiedEventDoesNotExist()
	{
		var listView = new ListView();
		var behavior = new EventToCommandBehavior
		{
			EventName = "Wrong Event Name"
		};
		Assert.Throws<ArgumentException>(() => listView.Behaviors.Add(behavior));
	}

	[Fact]
	public void NoExceptionIfSpecifiedEventExists()
	{
		var listView = new ListView();
		var behavior = new EventToCommandBehavior
		{
			EventName = nameof(ListView.ItemTapped)
		};
		listView.Behaviors.Add(behavior);
	}

	[Fact]
	public void NoExceptionIfAttachedToPage()
	{
		var page = new ContentPage();
		var behavior = new EventToCommandBehavior
		{
			EventName = nameof(Page.Appearing)
		};
		page.Behaviors.Add(behavior);
	}

	[Fact]
	public void ListView_ItemSelected_Test()
	{
		bool didEventToCommandBehaviorFire = false;

		var listView = new ListView
		{
			ItemsSource = new[]
			{
				"Item 0",
				"Item 1",
				"Item 2",
				"Item 3",
				"Item 4",
				"Item 5",
			},
			Behaviors =
			{
				new EventToCommandBehavior
				{
					EventName = nameof(ListView.ItemSelected),
					Command = new Command(HandleItemSelected)
				}
			}
		};

		listView.SelectedItem = "Item 3";

		Assert.True(didEventToCommandBehaviorFire);

		void HandleItemSelected() => didEventToCommandBehaviorFire = true;
	}

	[Fact]
	public void CollectionView_ItemSelected_Test()
	{
		bool didEventToCommandBehaviorFire = false;

		var collectionView = new CollectionView
		{
			ItemsSource = new[]
			{
				"Item 0",
				"Item 1",
				"Item 2",
				"Item 3",
				"Item 4",
				"Item 5",
			},
			Behaviors =
			{
				new EventToCommandBehavior
				{
					EventName = nameof(CollectionView.SelectionChanged),
					Command = new Command(HandleSelectionChanged)
				}
			}
		};

		collectionView.SelectedItem = "Item 3";

		Assert.True(didEventToCommandBehaviorFire);

		void HandleSelectionChanged() => didEventToCommandBehaviorFire = true;
	}

	[Fact]
	public void EventArgsConverterCulture_DefaultsToNullAndConverterReceivesInvariantCulture()
	{
		Thread.CurrentThread.CurrentUICulture = new CultureInfo("de-DE");
		var spy = new CultureSpyConverter();
		var behavior = new EventToCommandBehavior
		{
			EventName = nameof(ListView.ItemTapped),
			EventArgsConverter = spy,
			Command = new Command(() => { })
		};

		Assert.Null(behavior.EventArgsConverterCulture);

		TriggerOnTriggerHandled(behavior);

		Assert.Same(CultureInfo.InvariantCulture, spy.ReceivedCulture);
	}

	[Fact]
	public void EventArgsConverterCulture_IsPassedToEventArgsConverter()
	{
		var culture = new CultureInfo("de-DE");
		var spy = new CultureSpyConverter();
		var behavior = new EventToCommandBehavior
		{
			EventName = nameof(ListView.ItemTapped),
			EventArgsConverter = spy,
			EventArgsConverterCulture = culture,
			Command = new Command(() => { })
		};

		TriggerOnTriggerHandled(behavior);

		Assert.Same(culture, spy.ReceivedCulture);
	}

	[Fact]
	public void EventArgsConverterCulture_CanBeSetFromXamlWithACultureName()
	{
		var page = new EventToCommandBehaviorCulturePage();

		Assert.Equal(new CultureInfo("de-DE"), page.GermanBehavior.EventArgsConverterCulture);
		Assert.Null(page.DefaultBehavior.EventArgsConverterCulture);
		Assert.Equal(new CultureInfo("de-DE"), page.GenericGermanBehavior.EventArgsConverterCulture);
	}

	static void TriggerOnTriggerHandled(EventToCommandBehavior behavior)
	{
		var method = typeof(EventToCommandBehavior).GetMethod("OnTriggerHandled", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		method?.Invoke(behavior, [null, null]);
	}
}