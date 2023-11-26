using System.Globalization;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// This <see cref="EventToCommandBehavior"/> cast the sender object to a specific type defined by the user.
/// </summary>
/// <typeparam name="TType">The type that you want to receive in your <see cref="Microsoft.Maui.Controls.Command"/> </typeparam>
public sealed class EventToCommandBehavior<TType> : EventToCommandBehavior
{
	/// <inheritdoc/>
	protected override void OnTriggerHandled(object? sender = null, object? eventArgs = null)
	{
		var parameter = CommandParameter
			?? EventArgsConverter?.Convert(eventArgs, typeof(object), null, CultureInfo.InvariantCulture)
			?? eventArgs;

		if (parameter is not TType)
		{
			// changing it to the default value to avoid a cast exception
			parameter = default(TType);
		}

		var command = Command;
		if (command?.CanExecute(parameter) ?? false)
		{
			command.Execute(parameter);
		}
	}
}