using System.Reflection;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class EventToCommandBehaviorGeneric_Tests : BaseTest
{
	[Fact]
	public void ArgumentExceptionIfSpecifiedEventDoesNotExist()
	{
		var listView = new ListView();
		var behavior = new EventToCommandBehavior<Coffee>
		{
			EventName = "Wrong Event Name"
		};
		Assert.Throws<ArgumentException>(() => listView.Behaviors.Add(behavior));
	}

	[Fact]
	public void NoExceptionIfSpecifiedEventExists()
	{
		var listView = new ListView();
		var behavior = new EventToCommandBehavior<Coffee>
		{
			EventName = nameof(ListView.ItemTapped)
		};
		listView.Behaviors.Add(behavior);
	}

	[Fact]
	public void NoExceptionIfAttachedToPage()
	{
		var page = new ContentPage();
		var behavior = new EventToCommandBehavior<Coffee>
		{
			EventName = nameof(Page.Appearing)
		};
		page.Behaviors.Add(behavior);
	}

	[Fact]
	public void NoExceptionWhenTheEventArgsAreNotNull()
	{
		var vm = new ViewModelCoffe();
		var behavior = new EventToCommandBehavior<Coffee>
		{
			EventName = nameof(ListView.ItemTapped),
			EventArgsConverter = new SelectedItemEventArgsConverter(),
			Command = vm.SelectedCommand
		};

		Assert.Null(vm.CoffeeName);
		var coffe = new Coffee { Id = 1, Name = "Café" };
		var eventArgs = new SelectedItemChangedEventArgs(coffe, 1);

		var notNullArgs = new object?[] { null, eventArgs };

		TriggerEventToCommandBehavior(behavior, notNullArgs);

		Assert.Equal(coffe.Name, vm.CoffeeName);
	}

	[Fact]
	public void NoExceptionWhenTheEventArgsAreNotNull_InheritedType()
	{
		var vm = new ViewModelCoffe();
		var behavior = new EventToCommandBehavior<Coffee>
		{
			EventName = nameof(ListView.ItemTapped),
			EventArgsConverter = new SelectedItemEventArgsConverter(),
			Command = vm.SelectedCommand
		};

		Assert.Null(vm.CoffeeName);
		var coffe = new Starbucks { Id = 1, Name = "Latte" };
		var eventArgs = new SelectedItemChangedEventArgs(coffe, 1);

		var notNullArgs = new object?[] { null, eventArgs };

		TriggerEventToCommandBehavior(behavior, notNullArgs);

		Assert.Equal(coffe.Name, vm.CoffeeName);
	}

	[Fact]
	public void ParameterOfTypeInt()
	{
		var vm = new ViewModelCoffe();
		var behavior = new EventToCommandBehavior<int>
		{
			EventName = nameof(ListView.ItemTapped),
			Command = vm.SelectedCommand,
			CommandParameter = 2
		};

		var nullArgs = new object?[] { null, null };

		TriggerEventToCommandBehavior(behavior, nullArgs);
	}

	[Fact]
	public void NoExceptionWhenTheSelectedItemIsNull()
	{
		var vm = new ViewModelCoffe();
		var behavior = new EventToCommandBehavior<Coffee>
		{
			EventName = nameof(ListView.ItemTapped),
			EventArgsConverter = new SelectedItemEventArgsConverter(),
			Command = vm.SelectedCommand
		};

		Assert.Null(vm.CoffeeName);
		var coffeNull = default(Coffee);
		var notNullArgs = new object?[] { null, new SelectedItemChangedEventArgs(coffeNull, -1) };

		TriggerEventToCommandBehavior(behavior, notNullArgs);

		Assert.Null(vm.CoffeeName);
	}

	static void TriggerEventToCommandBehavior<T>(EventToCommandBehavior<T> eventToCommand, object?[] args)
	{
		var method = eventToCommand.GetType().GetMethod("OnTriggerHandled", BindingFlags.Instance | BindingFlags.NonPublic);
		method?.Invoke(eventToCommand, args);
	}

	public class Starbucks : Coffee
	{
	}

	public class Coffee
	{
		public int Id { get; set; }

		public string Roaster { get; set; } = string.Empty;

		public string? Name { get; set; }

		public string Image { get; set; } = string.Empty;
	}

	public class ViewModelCoffe
	{
		public Command<Coffee> SelectedCommand { get; set; }

		public string? CoffeeName { get; set; }

		public ViewModelCoffe()
		{
			SelectedCommand = new Command<Coffee>(Selected);
		}

		void Selected(Coffee coffee)
		{
			if (coffee == null)
			{
				return;
			}

			CoffeeName = coffee?.Name;
		}
	}
}