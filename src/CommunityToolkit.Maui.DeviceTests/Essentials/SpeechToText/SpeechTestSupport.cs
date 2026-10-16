using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;
using CommunityToolkit.Maui.Media;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// Records every event of an <see cref="ISpeechToText"/> in the order it was raised, as text such as
/// <c>StateChanged:Listening</c>, <c>Updated:hello</c>, <c>Completed:Success:hello world</c> and <c>Completed:Failed:message</c>.
/// Events can arrive on any thread (the Speech framework and the Android recognizer call back on their own queues).
/// </summary>
public sealed class SpeechEventRecorder
{
	readonly object gate = new();
	readonly List<string> entries = [];
	readonly List<object?> senders = [];
	readonly List<SpeechToTextResult> results = [];
	readonly ISpeechToText speech;

	public SpeechEventRecorder(ISpeechToText speech)
	{
		this.speech = speech;
		speech.StateChanged += OnStateChanged;
		speech.RecognitionResultUpdated += OnUpdated;
		speech.RecognitionResultCompleted += OnCompleted;
	}

	public string[] Entries
	{
		get
		{
			lock (gate)
			{
				return [.. entries];
			}
		}
	}

	public IReadOnlyList<object?> Senders
	{
		get
		{
			lock (gate)
			{
				return [.. senders];
			}
		}
	}

	public IReadOnlyList<SpeechToTextResult> Results
	{
		get
		{
			lock (gate)
			{
				return [.. results];
			}
		}
	}

	public int Count
	{
		get
		{
			lock (gate)
			{
				return entries.Count;
			}
		}
	}

	/// <summary>Asserts the exact sequence of events raised so far.</summary>
	public void AssertEntries(params string[] expected) => Assert.Equal(expected, Entries);

	/// <summary>Waits (polling, no fixed delay) until at least <paramref name="count"/> events were recorded.</summary>
	public Task WaitForCountAsync(int count, int timeoutMilliseconds = 5000) =>
		AssertionExtensions.AssertEventually(() => Count >= count, timeoutMilliseconds, 25, $"Expected at least {count} speech events but got: {string.Join(", ", Entries)}");

	/// <summary>Unsubscribes from all three events.</summary>
	public void Detach()
	{
		speech.StateChanged -= OnStateChanged;
		speech.RecognitionResultUpdated -= OnUpdated;
		speech.RecognitionResultCompleted -= OnCompleted;
	}

	void OnStateChanged(object? sender, SpeechToTextStateChangedEventArgs e) => Add(sender, $"StateChanged:{e.State}");

	void OnUpdated(object? sender, SpeechToTextRecognitionResultUpdatedEventArgs e) => Add(sender, $"Updated:{e.RecognitionResult}");

	void OnCompleted(object? sender, SpeechToTextRecognitionResultCompletedEventArgs e)
	{
		var result = e.RecognitionResult;
		lock (gate)
		{
			results.Add(result);
		}

		Add(sender, result.IsSuccessful ? $"Completed:Success:{result.Text}" : $"Completed:Failed:{result.Exception.Message}");
	}

	void Add(object? sender, string entry)
	{
		lock (gate)
		{
			entries.Add(entry);
			senders.Add(sender);
		}
	}
}

/// <summary>
/// A culture whose <see cref="Name"/> cancels a token when it is read for the <paramref name="cancelOnRead"/>-th time. The toolkit reads
/// the culture name while it starts, after its first cancellation check and before the later ones, so a test can cancel a start at a
/// precise point without a race. <see cref="Reads"/> tells how far the start got.
/// </summary>
public sealed class SpeechCancellingCulture(string name, CancellationTokenSource cancellation, int cancelOnRead = 1) : CultureInfo(name)
{
	int reads;

	public int Reads => Volatile.Read(ref reads);

	public override string Name
	{
		get
		{
			if (Interlocked.Increment(ref reads) >= cancelOnRead)
			{
				cancellation.Cancel();
			}

			return base.Name;
		}
	}
}

/// <summary>An event subscriber that tests drop to prove that the implementation holds its handlers weakly.</summary>
sealed class ShortLivedSpeechSubscriber
{
	public void OnStateChanged(object? sender, SpeechToTextStateChangedEventArgs e)
	{
	}

	public void OnUpdated(object? sender, SpeechToTextRecognitionResultUpdatedEventArgs e)
	{
	}

	public void OnCompleted(object? sender, SpeechToTextRecognitionResultCompletedEventArgs e)
	{
	}
}

/// <summary>
/// Reaches the private members of <c>SpeechToTextImplementation</c> and <c>OfflineSpeechToTextImplementation</c> (both classes use
/// the same member names). The platform recognizers raise their callbacks from the OS, and a test cannot make the OS do that on demand,
/// so the tests call the toolkit's own callback entry points and fields directly, with real platform objects as arguments.
/// </summary>
static class SpeechInternals
{
	const BindingFlags instanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
	const BindingFlags staticMembers = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

	public static void RaiseUpdated(ISpeechToText speech, string text) => Invoke(speech, "OnRecognitionResultUpdated", text);

	public static void RaiseCompleted(ISpeechToText speech, SpeechToTextResult result) => Invoke(speech, "OnRecognitionResultCompleted", result);

	public static void RaiseStateChanged(ISpeechToText speech, SpeechToTextState state) => Invoke(speech, "OnSpeechToTextStateChanged", state);

	public static object? Invoke(object target, string methodName, params object?[] arguments) =>
		InvokeCore(FindMethod(target.GetType(), methodName, instanceMembers), target, arguments);

	public static object? InvokeStatic(Type type, string methodName, params object?[] arguments) =>
		InvokeCore(FindMethod(type, methodName, staticMembers), null, arguments);

	public static T? GetField<T>(object target, string fieldName) where T : class =>
		(T?)FindField(target.GetType(), fieldName).GetValue(target);

	public static void SetField(object target, string fieldName, object? value) =>
		FindField(target.GetType(), fieldName).SetValue(target, value);

	public static Type GetNestedType(Type type, string name) =>
		type.GetNestedType(name, BindingFlags.NonPublic | BindingFlags.Public) ?? throw new MissingMemberException(type.FullName, name);

	public static TDelegate CreateHandler<TDelegate>(object target, string methodName) where TDelegate : Delegate =>
		(TDelegate)FindMethod(target.GetType(), methodName, instanceMembers).CreateDelegate(typeof(TDelegate), target);

	static object? InvokeCore(MethodInfo method, object? target, object?[] arguments)
	{
		try
		{
			return method.Invoke(target, arguments);
		}
		catch (TargetInvocationException exception) when (exception.InnerException is not null)
		{
			ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
			throw;
		}
	}

	static MethodInfo FindMethod(Type type, string name, BindingFlags flags) =>
		type.GetMethod(name, flags) ?? throw new MissingMethodException(type.FullName, name);

	static FieldInfo FindField(Type type, string name) =>
		type.GetField(name, instanceMembers) ?? throw new MissingFieldException(type.FullName, name);
}