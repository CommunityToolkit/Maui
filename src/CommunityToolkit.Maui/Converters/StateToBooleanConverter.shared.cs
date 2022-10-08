using System.ComponentModel;
using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>State used by <see cref="StateToBooleanConverter"/> </summary>
public enum LayoutState
{
	/// <summary>Default; will show the initial view</summary>
	None,
	/// <summary>Layout is Loading</summary>
	Loading,
	/// <summary>Layout is Saving</summary>
	Saving,
	/// <summary>Layout Success</summary>
	Success,
	/// <summary>Layout Error</summary>
	Error,
	/// <summary>Layout Empty</summary>
	Empty,
	/// <summary>Layout Custom</summary>
	Custom
}

/// <summary>
/// This converter can be used to determine if a certain state is visible. This can be useful, for instance, in scenarios where you want to show/hide certain elements based on the current state.
/// </summary>
public class StateToBooleanConverter : BaseConverterOneWay<LayoutState, bool, LayoutState?>
{
	LayoutState stateToCompare = LayoutState.None;

	/// <inheritdoc/>
	public override bool DefaultConvertReturnValue { get; set; } = false;

	/// <summary>
	/// The <see cref="LayoutState"/> to compare to.
	/// </summary>
	public LayoutState StateToCompare
	{
		get => stateToCompare;
		set
		{
			if (!Enum.IsDefined(typeof(LayoutState), value))
			{
				throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(LayoutState));
			}

			stateToCompare = value;
		}

	}

	/// <summary>
	/// Takes the incoming <see cref="LayoutState"/> in <paramref name="value"/> and compares it to <see cref="StateToCompare"/>. If they are equal it returns True, if they are not equal it returns False. Additionally a state to compare against can be provided in <paramref name="parameter"/>.
	/// </summary>
	/// <param name="value">The <see cref="LayoutState"/> to compare.</param>
	/// <param name="parameter">Optionally, a <see cref="LayoutState"/> can be supplied here to compare against.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>True if the provided <see cref="LayoutState"/>s match, otherwise False if they don't match.</returns>
	public override bool ConvertFrom(LayoutState value, LayoutState? parameter = null, CultureInfo? culture = null)
	{
		ArgumentNullException.ThrowIfNull(value);

		if (parameter is not null)
		{
			if (!Enum.IsDefined(typeof(LayoutState), parameter))
			{
				throw new InvalidEnumArgumentException(nameof(parameter), (int)parameter, typeof(LayoutState));
			}

			return value == parameter;
		}

		return value == StateToCompare;
	}
}