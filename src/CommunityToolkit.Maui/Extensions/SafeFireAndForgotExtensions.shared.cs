namespace CommunityToolkit.Maui.Extensions;

static class SafeFireAndForgetExtensions
{
	/// <summary>
	///     Safely execute the ValueTask without waiting for it to complete before moving to the next line of code; commonly
	///     known as "Fire And Forget". Inspired by John Thiriet's blog post, "Removing Async Void":
	///     https://johnthiriet.com/removing-async-void/.
	/// </summary>
	/// <param name="task">ValueTask.</param>
	/// <param name="onException">
	///     If an exception is thrown in the ValueTask, <c>onException</c> will execute. If onException
	///     is null, the exception will be re-thrown
	/// </param>
	/// <param name="continueOnCapturedContext">
	///     If set to <c>true</c>, continue on captured context; this will ensure that the
	///     Synchronization Context returns to the calling thread. If set to <c>false</c>, continue on a different context;
	///     this will allow the Synchronization Context to continue on a different thread
	/// </param>
	internal static void SafeFireAndForget(this ValueTask task, in Action<Exception>? onException = null, in bool continueOnCapturedContext = false)
	{
		HandleSafeFireAndForget(task, continueOnCapturedContext, onException);
	}

	/// <summary>
	///     Safely execute the ValueTask without waiting for it to complete before moving to the next line of code; commonly
	///     known as "Fire And Forget". Inspired by John Thiriet's blog post, "Removing Async Void":
	///     https://johnthiriet.com/removing-async-void/.
	/// </summary>
	/// <param name="task">ValueTask.</param>
	/// <param name="onException">
	///     If an exception is thrown in the Task, <c>onException</c> will execute. If onException is
	///     null, the exception will be re-thrown
	/// </param>
	/// <param name="continueOnCapturedContext">
	///     If set to <c>true</c>, continue on captured context; this will ensure that the
	///     Synchronization Context returns to the calling thread. If set to <c>false</c>, continue on a different context;
	///     this will allow the Synchronization Context to continue on a different thread
	/// </param>
	/// <typeparam name="TException">Exception type. If an exception is thrown of a different type, it will not be handled</typeparam>
	internal static void SafeFireAndForget<TException>(this ValueTask task, in Action<TException>? onException = null, in bool continueOnCapturedContext = false) where TException : Exception
	{
		HandleSafeFireAndForget(task, continueOnCapturedContext, onException);
	}

	/// <summary>
	///     Safely execute the Task without waiting for it to complete before moving to the next line of code; commonly known
	///     as "Fire And Forget". Inspired by John Thiriet's blog post, "Removing Async Void":
	///     https://johnthiriet.com/removing-async-void/.
	/// </summary>
	/// <param name="task">Task.</param>
	/// <param name="onException">
	///     If an exception is thrown in the Task, <c>onException</c> will execute. If onException is
	///     null, the exception will be re-thrown
	/// </param>
	/// <param name="continueOnCapturedContext">
	///     If set to <c>true</c>, continue on captured context; this will ensure that the
	///     Synchronization Context returns to the calling thread. If set to <c>false</c>, continue on a different context;
	///     this will allow the Synchronization Context to continue on a different thread
	/// </param>
	internal static void SafeFireAndForget(this Task task, in Action<Exception>? onException = null, in bool continueOnCapturedContext = false)
	{
		HandleSafeFireAndForget(task, continueOnCapturedContext, onException);
	}

	/// <summary>
	///     Safely execute the Task without waiting for it to complete before moving to the next line of code; commonly known
	///     as "Fire And Forget". Inspired by John Thiriet's blog post, "Removing Async Void":
	///     https://johnthiriet.com/removing-async-void/.
	/// </summary>
	/// <param name="task">Task.</param>
	/// <param name="configureAwaitOptions">
	///		<see cref="ConfigureAwaitOptions"/>
	/// </param>
	/// <param name="onException">
	///     If an exception is thrown in the Task, <c>onException</c> will execute. If onException is
	///     null, the exception will be re-thrown
	/// </param>
	internal static void SafeFireAndForget(this Task task, in ConfigureAwaitOptions configureAwaitOptions, in Action<Exception>? onException = null)
	{
		HandleSafeFireAndForget(task, configureAwaitOptions, onException);
	}

	/// <summary>
	///     Safely execute the Task without waiting for it to complete before moving to the next line of code; commonly known
	///     as "Fire And Forget". Inspired by John Thiriet's blog post, "Removing Async Void":
	///     https://johnthiriet.com/removing-async-void/.
	/// </summary>
	/// <param name="task">Task.</param>
	/// <param name="configureAwaitOptions">
	///		<see cref="ConfigureAwaitOptions"/>
	/// </param>
	/// <param name="onException">
	///     If an exception is thrown in the Task, <c>onException</c> will execute. If onException is
	///     null, the exception will be re-thrown
	/// </param>
	/// <typeparam name="TException">Exception type. If an exception is thrown of a different type, it will not be handled</typeparam>
	internal static void SafeFireAndForget<TException>(this Task task, in ConfigureAwaitOptions configureAwaitOptions, in Action<TException>? onException = null) where TException : Exception
	{
		HandleSafeFireAndForget(task, configureAwaitOptions, onException);
	}

	/// <summary>
	///     Safely execute the Task without waiting for it to complete before moving to the next line of code; commonly known
	///     as "Fire And Forget". Inspired by John Thiriet's blog post, "Removing Async Void":
	///     https://johnthiriet.com/removing-async-void/.
	/// </summary>
	/// <param name="task">Task.</param>
	/// <param name="onException">
	///     If an exception is thrown in the Task, <c>onException</c> will execute. If onException is
	///     null, the exception will be re-thrown
	/// </param>
	/// <param name="continueOnCapturedContext">
	///     If set to <c>true</c>, continue on captured context; this will ensure that the
	///     Synchronization Context returns to the calling thread. If set to <c>false</c>, continue on a different context;
	///     this will allow the Synchronization Context to continue on a different thread
	/// </param>
	/// <typeparam name="TException">Exception type. If an exception is thrown of a different type, it will not be handled</typeparam>
	internal static void SafeFireAndForget<TException>(this Task task, in Action<TException>? onException = null, in bool continueOnCapturedContext = false) where TException : Exception
	{
		HandleSafeFireAndForget(task, continueOnCapturedContext, onException);
	}

	static async void HandleSafeFireAndForget<TException>(ValueTask valueTask, bool continueOnCapturedContext, Action<TException>? onException) where TException : Exception
	{
		try
		{
			await valueTask.ConfigureAwait(continueOnCapturedContext);
		}
		catch (TException ex) when (onException is not null)
		{
			onException(ex);
		}
	}

	static async void HandleSafeFireAndForget<TException>(Task task, bool continueOnCapturedContext, Action<TException>? onException) where TException : Exception
	{
		try
		{
			await task.ConfigureAwait(continueOnCapturedContext);
		}
		catch (TException ex) when (onException is not null)
		{
			onException(ex);
		}
	}

	static async void HandleSafeFireAndForget<TException>(Task task, ConfigureAwaitOptions configureAwaitOptions, Action<TException>? onException) where TException : Exception
	{
		try
		{
			await task.ConfigureAwait(configureAwaitOptions);
		}
		catch (TException ex) when (onException is not null)
		{
			onException(ex);
		}
	}
}