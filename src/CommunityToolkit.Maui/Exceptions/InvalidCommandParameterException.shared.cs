using System;

// Inspired by AsyncAwaitBestPractices.MVVM.InvalidCommandParameterException: https://github.com/brminnick/AsyncAwaitBestPractices
namespace CommunityToolkit.Maui.Exceptions
{
	/// <summary>
	/// Represents errors that occur during IAsyncCommand execution.
	/// </summary>
	public class InvalidCommandParameterException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="InvalidCommandParameterException"/> class.
		/// </summary>
		/// <param name="expectedType">Expected parameter type for AsyncCommand.Execute.</param>
		/// <param name="actualType">Actual parameter type for AsyncCommand.Execute.</param>
		/// <param name="innerException">Inner Exception</param>
		public InvalidCommandParameterException(Type expectedType, Type actualType, Exception innerException)
			: base(CreateErrorMessage(expectedType, actualType), innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="InvalidCommandParameterException"/> class.
		/// </summary>
		/// <param name="expectedType">Expected parameter type for AsyncCommand.Execute.</param>
		/// <param name="actualType">Actual parameter type for AsyncCommand.Execute.</param>
		public InvalidCommandParameterException(Type expectedType, Type actualType)
			: base(CreateErrorMessage(expectedType, actualType))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="InvalidCommandParameterException"/> class.
		/// </summary>
		/// <param name="expectedType">Expected parameter type for AsyncCommand.Execute.</param>
		/// <param name="innerException">Inner Exception</param>
		public InvalidCommandParameterException(Type expectedType, Exception innerException)
			: base(CreateErrorMessage(expectedType), innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="InvalidCommandParameterException"/> class.
		/// </summary>
		/// <param name="expectedType">Expected parameter type for AsyncCommand.Execute.</param>
		public InvalidCommandParameterException(Type expectedType)
			: base(CreateErrorMessage(expectedType))
		{
		}

		static string CreateErrorMessage(Type expectedType) => $"Invalid type for parameter. Expected Type {expectedType}";

		static string CreateErrorMessage(Type expectedType, Type actualType) => $"Invalid type for parameter. Expected Type {expectedType}, but received Type {actualType}";
	}
}