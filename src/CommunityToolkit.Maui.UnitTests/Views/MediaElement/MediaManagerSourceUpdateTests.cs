using System.Reflection;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class MediaManagerSourceUpdateTests : BaseTest
{
	const string reportPrefix = "An event handler threw an exception while the source of the MediaElement was updated: ";

	readonly CapturingLoggerFactory loggerFactory = new();

	[Fact]
	public async Task ExceptionOfAStateChangedHandlerDoesNotEndTheUpdateAndIsReportedOnceThroughMediaFailed()
	{
		var mediaElement = new MediaElement();
		var failures = RecordFailures(mediaElement);
		var handlerException = new InvalidOperationException("The StateChanged handler of the app failed.");
		mediaElement.StateChanged += (_, e) =>
		{
			if (e.NewState is MediaElementState.Opening or MediaElementState.Paused)
			{
				throw handlerException;
			}
		};

		var manager = CreateManager(mediaElement, element =>
		{
			element.CurrentStateChanged(MediaElementState.Opening);
			element.Duration = TimeSpan.FromSeconds(5);
			element.CurrentStateChanged(MediaElementState.Paused);
			return ValueTask.CompletedTask;
		});

		await manager.UpdateSource();

		// Paused shows that the update went on after each exception, and that MAUI could still change CurrentState after a handler threw while it
		// was set.
		mediaElement.CurrentState.Should().Be(MediaElementState.Paused);
		mediaElement.Duration.Should().Be(TimeSpan.FromSeconds(5));
		failures.Should().Equal($"{reportPrefix}System.InvalidOperationException: The StateChanged handler of the app failed.");
		loggerFactory.Errors.Should().HaveCount(2).And.OnlyContain(entry => entry.Exception == handlerException);

		// Outside a source update, the exception of a handler reaches the caller, as before.
		var setState = () => ((IMediaElement)mediaElement).CurrentStateChanged(MediaElementState.Opening);
		setState.Should().Throw<TargetInvocationException>().WithInnerException<InvalidOperationException>();
	}

	[Fact]
	public async Task ExceptionOfAPropertyChangedHandlerLeavesDurationAndPositionUpdatable()
	{
		var mediaElement = new MediaElement();
		var failures = RecordFailures(mediaElement);
		var handlerException = new InvalidOperationException("The PropertyChanged handler of the app failed.");
		mediaElement.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName is nameof(MediaElement.Duration) or nameof(MediaElement.Position))
			{
				throw handlerException;
			}
		};

		var manager = CreateManager(mediaElement, element =>
		{
			element.Duration = TimeSpan.FromSeconds(5);
			element.Position = TimeSpan.FromSeconds(1);

			// MAUI applies no later change of a property whose setting threw, so these show that both settings ended.
			element.Duration = TimeSpan.FromSeconds(6);
			element.Position = TimeSpan.FromSeconds(2);
			return ValueTask.CompletedTask;
		});

		await manager.UpdateSource();

		mediaElement.Duration.Should().Be(TimeSpan.FromSeconds(6));
		mediaElement.Position.Should().Be(TimeSpan.FromSeconds(2));
		mediaElement.CurrentState.Should().Be(MediaElementState.None);
		failures.Should().Equal($"{reportPrefix}System.InvalidOperationException: The PropertyChanged handler of the app failed.");
		loggerFactory.Errors.Should().HaveCount(4);
	}

	[Fact]
	public async Task ExceptionOfAPropertyChangingHandlerLeavesDurationAndCurrentStateUpdatable()
	{
		var mediaElement = new MediaElement();
		var failures = RecordFailures(mediaElement);
		var handlerException = new InvalidOperationException("The PropertyChanging handler of the app failed.");
		mediaElement.PropertyChanging += (_, e) =>
		{
			if (e.PropertyName is nameof(MediaElement.Duration) or nameof(MediaElement.CurrentState))
			{
				throw handlerException;
			}
		};

		var manager = CreateManager(mediaElement, element =>
		{
			element.Duration = TimeSpan.FromSeconds(5);
			element.CurrentStateChanged(MediaElementState.Opening);

			// MAUI applies no later change of a property whose setting threw, so these show that both settings ended.
			element.Duration = TimeSpan.FromSeconds(6);
			element.CurrentStateChanged(MediaElementState.Paused);
			return ValueTask.CompletedTask;
		});

		await manager.UpdateSource();

		mediaElement.Duration.Should().Be(TimeSpan.FromSeconds(6));
		mediaElement.CurrentState.Should().Be(MediaElementState.Paused);
		failures.Should().Equal($"{reportPrefix}System.InvalidOperationException: The PropertyChanging handler of the app failed.");
		loggerFactory.Errors.Should().HaveCount(4).And.OnlyContain(entry => entry.Exception == handlerException);

		// Outside a source update, the exception of a handler reaches the caller, as before.
		var setDuration = () => ((IMediaElement)mediaElement).Duration = TimeSpan.FromSeconds(7);
		setDuration.Should().Throw<InvalidOperationException>().Which.Should().BeSameAs(handlerException);
	}

	[Fact]
	public async Task ExceptionOfAMediaFailedHandlerWhileItIsReportedIsOnlyLogged()
	{
		var mediaElement = new MediaElement();
		var failures = RecordFailures(mediaElement);
		var handlerException = new InvalidOperationException("The MediaFailed handler of the app failed.");
		mediaElement.MediaFailed += (_, _) => throw handlerException;

		// The update reports a source that it cannot apply, so the handler throws for that failure and again for the report.
		var manager = CreateManager(mediaElement, element =>
		{
			element.MediaFailed(new MediaFailedEventArgs("The source cannot be applied."));
			return ValueTask.CompletedTask;
		});

		await manager.UpdateSource();

		failures.Should().Equal(
			"The source cannot be applied.",
			$"{reportPrefix}System.InvalidOperationException: The MediaFailed handler of the app failed.");
		mediaElement.CurrentState.Should().Be(MediaElementState.Failed);

		var errors = loggerFactory.Errors;
		errors.Should().HaveCount(2);
		errors[0].Exception.Should().BeSameAs(handlerException);
		errors[1].Message.Should().StartWith("A MediaFailed handler threw an exception while this was reported: ");
		errors[1].Exception.Should().BeSameAs(handlerException);
	}

	[Fact]
	public async Task ExceptionOfAHandlerAfterTheUpdateAwaitedIsReportedWhenTheUpdateEnds()
	{
		var mediaElement = new MediaElement();
		var failures = RecordFailures(mediaElement);
		var handlerException = new InvalidOperationException("The MediaOpened handler of the app failed.");
		mediaElement.MediaOpened += (_, _) => throw handlerException;
		var resume = new TaskCompletionSource();

		var manager = CreateManager(mediaElement, async element =>
		{
			element.CurrentStateChanged(MediaElementState.Opening);
			await resume.Task;
			element.MediaOpened();
			element.CurrentStateChanged(MediaElementState.Playing);
		});

		var update = manager.UpdateSource().AsTask();
		update.IsCompleted.Should().BeFalse();
		resume.SetResult();
		await update;

		mediaElement.CurrentState.Should().Be(MediaElementState.Playing);
		failures.Should().Equal($"{reportPrefix}System.InvalidOperationException: The MediaOpened handler of the app failed.");
		loggerFactory.Errors.Should().ContainSingle().Which.Exception.Should().BeSameAs(handlerException);

		// MediaOpened started the status timer.
		mediaElement.Dispose();
	}

	static List<string> RecordFailures(MediaElement mediaElement)
	{
		var failures = new List<string>();
		mediaElement.MediaFailed += (_, e) => failures.Add(e.ErrorMessage);
		return failures;
	}

	TestMediaManager CreateManager(MediaElement mediaElement, Func<IMediaElement, ValueTask> updateSource) =>
		new(new LoggingMauiContext(loggerFactory), mediaElement, updateSource);

	sealed class TestMediaManager(IMauiContext context, MediaElement mediaElement, Func<IMediaElement, ValueTask> updateSource)
		: MediaManager(context, mediaElement, mediaElement.Dispatcher)
	{
		protected override ValueTask PlatformUpdateSource() => updateSource(MediaElement);
	}

	sealed class LoggingMauiContext(ILoggerFactory loggerFactory) : IMauiContext, IServiceProvider
	{
		public IServiceProvider Services => this;

		public IMauiHandlersFactory Handlers => throw new NotSupportedException();

		public object? GetService(Type serviceType) => serviceType == typeof(ILoggerFactory) ? loggerFactory : null;
	}

	sealed class CapturingLoggerFactory : ILoggerFactory
	{
		readonly List<(string Message, Exception? Exception)> errors = [];

		public IReadOnlyList<(string Message, Exception? Exception)> Errors
		{
			get
			{
				lock (errors)
				{
					return [.. errors];
				}
			}
		}

		public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

		public void AddProvider(ILoggerProvider provider)
		{
		}

		public void Dispose()
		{
		}

		sealed class CapturingLogger(CapturingLoggerFactory owner) : ILogger
		{
			public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

			public bool IsEnabled(LogLevel logLevel) => true;

			public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
			{
				if (logLevel is LogLevel.Error)
				{
					lock (owner.errors)
					{
						owner.errors.Add((formatter(state, exception), exception));
					}
				}
			}
		}
	}
}