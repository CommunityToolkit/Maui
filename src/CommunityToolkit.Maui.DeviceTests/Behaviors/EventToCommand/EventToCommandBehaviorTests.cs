using System.Globalization;
using CommunityToolkit.Maui.Behaviors;

namespace CommunityToolkit.Maui.DeviceTests;

// The behavior finds its event by name with reflection and binds the handler with CreateDelegate, which is what full trimming and NativeAOT must keep working.
[Category(TestCategory.EventToCommandBehavior)]
[Collection(TestCollections.DisableParallelization)]
public sealed class EventToCommandBehaviorTests : CommunityToolkitHandlerTestBase
{
	[Fact]
	public async Task ClickedEventOfAButtonExecutesTheCommandWithTheCommandParameter()
	{
		SetupBuilder();
		var executed = new List<object?>();
		var button = new Button { Text = "Tap" };
		button.Behaviors.Add(new EventToCommandBehavior
		{
			EventName = nameof(Button.Clicked),
			Command = new Command<string>(executed.Add),
			CommandParameter = "parameter"
		});

		await AttachAndRun(button, async _ =>
		{
			await button.OnLoadedAsync();

			((IButtonController)button).SendClicked();
			((IButtonController)button).SendClicked();
		});

		Assert.Equal(["parameter", "parameter"], executed);
	}

	[Fact]
	public async Task EventArgsConverterTurnsTheEventArgsOfTheEntryIntoTheCommandParameter()
	{
		SetupBuilder();
		var executed = new List<object?>();
		var entry = new Entry { WidthRequest = 200 };
		entry.Behaviors.Add(new EventToCommandBehavior
		{
			EventName = nameof(Entry.TextChanged),
			Command = new Command<string>(executed.Add),
			EventArgsConverter = new NewTextValueConverter()
		});

		await AttachAndRun(entry, async _ =>
		{
			await entry.OnLoadedAsync();

			entry.Text = "first";
			entry.Text = "second";
		});

		Assert.Equal(["first", "second"], executed);
	}

	[Theory]
	[InlineData("en-US")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public async Task EventArgsConverterReceivesTheConfiguredCulture(string cultureName)
	{
		SetupBuilder();
		var culture = CultureInfo.GetCultureInfo(cultureName);
		var converter = new NewTextValueConverter();
		var entry = new Entry { WidthRequest = 200 };
		entry.Behaviors.Add(new EventToCommandBehavior
		{
			EventName = nameof(Entry.TextChanged),
			Command = new Command(static () => { }),
			EventArgsConverter = converter,
			EventArgsConverterCulture = culture
		});

		await AttachAndRun(entry, async _ =>
		{
			await entry.OnLoadedAsync();

			var originalCulture = CultureInfo.CurrentCulture;
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");

			try
			{
				entry.Text = "Iıiİ";
			}
			finally
			{
				CultureInfo.CurrentCulture = originalCulture;
			}
		});

		Assert.Same(culture, converter.LastCulture);
		Assert.Equal("Iıiİ", converter.LastValue);
	}

	[Fact]
	public async Task GenericBehaviorPassesTheEventArgsOfTheMatchingTypeToTheCommand()
	{
		SetupBuilder();
		var executed = new List<string?>();
		var entry = new Entry { WidthRequest = 200 };
		entry.Behaviors.Add(new EventToCommandBehavior<string>
		{
			EventName = nameof(Entry.TextChanged),
			Command = new Command<string>(executed.Add),
			EventArgsConverter = new NewTextValueConverter()
		});

		await AttachAndRun(entry, async _ =>
		{
			await entry.OnLoadedAsync();

			entry.Text = "typed";
		});

		Assert.Equal(["typed"], executed);
	}

	[Fact]
	public async Task GenericBehaviorPassesTheDefaultValueWhenTheParameterIsOfAnotherType()
	{
		SetupBuilder();
		var executed = new List<string?>();
		var entry = new Entry { WidthRequest = 200 };
		entry.Behaviors.Add(new EventToCommandBehavior<string>
		{
			EventName = nameof(Entry.TextChanged),
			Command = new Command<string?>(executed.Add)
		});

		await AttachAndRun(entry, async _ =>
		{
			await entry.OnLoadedAsync();

			entry.Text = "typed";
		});

		Assert.Equal([null], executed);
	}

	[Fact]
	public async Task ChangingTheEventNameMovesTheSubscriptionToTheNewEvent()
	{
		SetupBuilder();
		var executed = new List<string>();
		var entry = new Entry { WidthRequest = 200 };
		var behavior = new EventToCommandBehavior
		{
			EventName = nameof(Entry.Completed),
			Command = new Command<string>(executed.Add),
			CommandParameter = "completed"
		};
		entry.Behaviors.Add(behavior);

		await AttachAndRun(entry, async _ =>
		{
			await entry.OnLoadedAsync();

			((IEntryController)entry).SendCompleted();
			behavior.EventName = nameof(Entry.TextChanged);
			behavior.CommandParameter = "changed";
			((IEntryController)entry).SendCompleted();
			entry.Text = "x";
		});

		Assert.Equal(["completed", "changed"], executed);
	}

	[Fact]
	public async Task DetachedBehaviorStopsExecutingTheCommand()
	{
		SetupBuilder();
		var executionCount = 0;
		var button = new Button { Text = "Tap" };
		var behavior = new EventToCommandBehavior
		{
			EventName = nameof(Button.Clicked),
			Command = new Command(() => executionCount++)
		};
		button.Behaviors.Add(behavior);

		await AttachAndRun(button, async _ =>
		{
			await button.OnLoadedAsync();

			((IButtonController)button).SendClicked();
			button.Behaviors.Remove(behavior);
			((IButtonController)button).SendClicked();
		});

		Assert.Equal(1, executionCount);
	}

	[Fact]
	public async Task UnknownEventNameThrowsAnArgumentExceptionWhenTheBehaviorIsAttached()
	{
		SetupBuilder();
		var button = new Button { Text = "Tap" };
		var behavior = new EventToCommandBehavior
		{
			EventName = "NoSuchEvent",
			Command = new Command(static () => { })
		};

		await AttachAndRun(button, async _ =>
		{
			await button.OnLoadedAsync();

			var exception = Assert.Throws<ArgumentException>(() => button.Behaviors.Add(behavior));

			Assert.Equal(nameof(EventToCommandBehavior.EventName), exception.ParamName);
		});
	}

	sealed class NewTextValueConverter : IValueConverter
	{
		public CultureInfo? LastCulture { get; private set; }

		public object? LastValue { get; private set; }

		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
		{
			LastCulture = culture;
			LastValue = ((TextChangedEventArgs?)value)?.NewTextValue;
			return LastValue;
		}

		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture) => throw new NotSupportedException();
	}
}