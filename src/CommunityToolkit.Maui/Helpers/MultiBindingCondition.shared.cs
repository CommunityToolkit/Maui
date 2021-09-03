namespace CommunityToolkit.Maui.Helpers
{
	/// <summary>
	/// Different types of conditions that can be used in <see cref="Converters.VariableMultiValueConverter"/>.
	/// </summary>
	public enum MultiBindingCondition
	{
		/// <summary>None of the values should be true.</summary>
		None,

		/// <summary>All of the values should be true.</summary>
		All,

		/// <summary>Any of the values should be true.</summary>
		Any,

		/// <summary>The exact number as configured in <see cref="Converters.VariableMultiValueConverter.Count"/> should be true.</summary>
		Exact,

		/// <summary>At least the number as configured in <see cref="Converters.VariableMultiValueConverter.Count"/> should be true.</summary>
		GreaterThan,

		/// <summary>At most the number as configured in <see cref="Converters.VariableMultiValueConverter.Count"/> should be true.</summary>
		LessThan
	}
}