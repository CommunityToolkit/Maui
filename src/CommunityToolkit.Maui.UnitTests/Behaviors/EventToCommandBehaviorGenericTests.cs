using System.Reflection;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class EventToCommandBehaviorGenericTests() : BaseBehaviorTest<EventToCommandBehavior<Coffee>, VisualElement>(new EventToCommandBehavior<Coffee>(), new View())
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
		var vm = new ViewModelCoffee();
		var behavior = new EventToCommandBehavior<Coffee>
		{
			EventName = nameof(ListView.ItemTapped),
			EventArgsConverter = new SelectedItemEventArgsConverter(),
			Command = vm.SelectedCommand
		};

		Assert.Null(vm.CoffeeName);
		var coffee = new Coffee
		{
			Id = 1,
			Name = "Café"
		};
		var eventArgs = new SelectedItemChangedEventArgs(coffee, 1);

		var notNullArgs = new object?[]
		{
			null, eventArgs
		};

		TriggerEventToCommandBehavior(behavior, notNullArgs);

		Assert.Equal(coffee.Name, vm.CoffeeName);
	}

	[Fact]
	public void NoExceptionWhenTheEventArgsAreNotNull_InheritedType()
	{
		var vm = new ViewModelCoffee();
		var behavior = new EventToCommandBehavior<Coffee>
		{
			EventName = nameof(ListView.ItemTapped),
			EventArgsConverter = new SelectedItemEventArgsConverter(),
			Command = vm.SelectedCommand
		};

		Assert.Null(vm.CoffeeName);
		var coffee = new Starbucks
		{
			Id = 1,
			Name = "Latte"
		};
		var eventArgs = new SelectedItemChangedEventArgs(coffee, 1);

		var notNullArgs = new object?[]
		{
			null, eventArgs
		};

		TriggerEventToCommandBehavior(behavior, notNullArgs);

		Assert.Equal(coffee.Name, vm.CoffeeName);
	}

	[Fact]
	public void ParameterOfTypeInt()
	{
		var vm = new ViewModelCoffee();
		var behavior = new EventToCommandBehavior<int>
		{
			EventName = nameof(ListView.ItemTapped),
			Command = vm.SelectedCommand,
			CommandParameter = 2
		};

		var nullArgs = new object?[]
		{
			null, null
		};

		TriggerEventToCommandBehavior(behavior, nullArgs);
	}

	[Fact]
	public void NoExceptionWhenTheSelectedItemIsNull()
	{
		var vm = new ViewModelCoffee();
		var behavior = new EventToCommandBehavior<Coffee>
		{
			EventName = nameof(ListView.ItemTapped),
			EventArgsConverter = new SelectedItemEventArgsConverter(),
			Command = vm.SelectedCommand
		};

		Assert.Null(vm.CoffeeName);
		var coffeeNull = default(Coffee);
		var notNullArgs = new object?[]
		{
			null, new SelectedItemChangedEventArgs(coffeeNull, -1)
		};

		TriggerEventToCommandBehavior(behavior, notNullArgs);

		Assert.Null(vm.CoffeeName);
	}

	[Fact]
	public void HappilyUsesIValueConverterImplementation()
	{
		var vm = new ViewModelCoffee();
		var convertedValue = default(object);
		var cappuccino = new Coffee();

		var behavior = new EventToCommandBehavior<Coffee>
		{
			EventName = nameof(ListView.ItemTapped),
			EventArgsConverter = new MockValueConverter((value) => cappuccino),
			Command = new Command((parameter) => convertedValue = parameter)
		};

		var nullArgs = new object?[]
		{
			null, null
		};

		TriggerEventToCommandBehavior(behavior, nullArgs);

		Assert.Equal(cappuccino, convertedValue);
	}

	static void TriggerEventToCommandBehavior<T>(EventToCommandBehavior<T> eventToCommand, object?[] args)
	{
		var method = eventToCommand.GetType().GetMethod("OnTriggerHandled", BindingFlags.Instance | BindingFlags.NonPublic);
		method?.Invoke(eventToCommand, args);
	}
}

class Starbucks : Coffee
{
}

public class Coffee
{
	public int Id { get; set; }

	public string Roaster { get; set; } = string.Empty;

	public string? Name { get; set; }

	public string Image { get; set; } = string.Empty;
}

public class ViewModelCoffee
{
	public Command<Coffee> SelectedCommand { get; set; }

	public string? CoffeeName { get; set; }

	public ViewModelCoffee()
	{
		SelectedCommand = new Command<Coffee>(Selected);
	}

	void Selected(Coffee coffee)
	{
		if (coffee is null)
		{
			return;
		}

		CoffeeName = coffee.Name;
	}
}