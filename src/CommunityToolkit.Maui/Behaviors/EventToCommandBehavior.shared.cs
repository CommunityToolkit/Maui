using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Windows.Input;
using CommunityToolkit.Maui.Converters;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// The <see cref="EventToCommandBehavior"/> is a behavior that allows the user to invoke a <see cref="ICommand"/> through an event. It is designed to associate Commands to events exposed by controls that were not designed to support Commands. It allows you to map any arbitrary event on a control to a Command.
/// </summary>
public partial class EventToCommandBehavior : BaseBehavior<VisualElement>
{
	[DynamicDependency(nameof(OnTriggerHandled), typeof(EventToCommandBehavior))]
	readonly MethodInfo eventHandlerMethodInfo = typeof(EventToCommandBehavior).GetTypeInfo().GetDeclaredMethod(nameof(OnTriggerHandled)) ?? throw new InvalidOperationException($"Cannot find method {nameof(OnTriggerHandled)}");

	Delegate? eventHandler;

	EventInfo? eventInfo;

	/// <summary>
	/// The name of the event that should be associated with <see cref="Command"/>. This is bindable property.
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnEventNamePropertyChanged))]
	public partial string? EventName { get; set; }

	/// <summary>
	/// The Command that should be executed when the event configured with <see cref="EventName"/> is triggered. This is a bindable property.
	/// </summary>
	[BindableProperty]
	public partial ICommand? Command { get; set; }

	/// <summary>
	/// An optional parameter to forward to the <see cref="Command"/>. This is a bindable property.
	/// </summary>
	[BindableProperty]
	public partial object? CommandParameter { get; set; }

	/// <summary>
	/// An optional <see cref="ICommunityToolkitValueConverter"/> that can be used to convert <see cref="EventArgs"/> values, associated with the event configured with <see cref="EventName"/>, to values passed into the <see cref="Command"/>. This is a bindable property.
	/// </summary>
	[BindableProperty]
	public partial IValueConverter? EventArgsConverter { get; set; }

	/// <summary>
	/// The <see cref="CultureInfo"/> passed to <see cref="EventArgsConverter"/> when it converts the <see cref="EventArgs"/>. When <see langword="null"/>, <see cref="CultureInfo.InvariantCulture"/> is used. This is a bindable property.
	/// </summary>
	/// <remarks>
	/// Unlike a binding, which passes <see cref="CultureInfo.CurrentUICulture"/> when no converter culture is set, this behavior keeps passing <see cref="CultureInfo.InvariantCulture"/> by default.
	/// </remarks>
	[TypeConverter(typeof(CultureInfoConverter))]
	[BindableProperty]
	public partial CultureInfo? EventArgsConverterCulture { get; set; }

	/// <inheritdoc/>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="bindable"/> is <see langword="null"/>.</exception>
	protected override void OnAttachedTo(VisualElement bindable)
	{
		ArgumentNullException.ThrowIfNull(bindable);

		base.OnAttachedTo(bindable);
		RegisterEvent();
	}

	/// <inheritdoc/>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="bindable"/> is <see langword="null"/>.</exception>
	protected override void OnDetachingFrom(VisualElement bindable)
	{
		ArgumentNullException.ThrowIfNull(bindable);

		UnregisterEvent();
		base.OnDetachingFrom(bindable);
	}

	/// <summary>
	/// Virtual method that executes when a Command is invoked
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="eventArgs"></param>
	protected virtual void OnTriggerHandled(object? sender = null, object? eventArgs = null)
	{
		var parameter = CommandParameter
			?? EventArgsConverter?.Convert(eventArgs, typeof(object), null, EventArgsConverterCulture ?? CultureInfo.InvariantCulture);

		var command = Command;
		if (command?.CanExecute(parameter) ?? false)
		{
			command.Execute(parameter);
		}
	}

	static void OnEventNamePropertyChanged(BindableObject bindable, object oldValue, object newValue)
		=> ((EventToCommandBehavior)bindable).RegisterEvent();

	void RegisterEvent()
	{
		UnregisterEvent();

		var eventName = EventName;
		if (View is null || string.IsNullOrWhiteSpace(eventName))
		{
			return;
		}

		eventInfo = View.GetType().GetRuntimeEvent(eventName) ??
			throw new ArgumentException($"{nameof(EventToCommandBehavior)}: Couldn't resolve the event.", nameof(EventName));

		ArgumentNullException.ThrowIfNull(eventInfo.EventHandlerType);
		ArgumentNullException.ThrowIfNull(eventHandlerMethodInfo);

		eventHandler = eventHandlerMethodInfo.CreateDelegate(eventInfo.EventHandlerType, this) ??
			throw new ArgumentException($"{nameof(EventToCommandBehavior)}: Couldn't create event handler.", nameof(EventName));

		eventInfo.AddEventHandler(View, eventHandler);
	}

	void UnregisterEvent()
	{
		if (eventInfo is not null && eventHandler is not null)
		{
			eventInfo.RemoveEventHandler(View, eventHandler);
		}

		eventInfo = null;
		eventHandler = null;
	}
}