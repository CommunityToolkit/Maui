using Microsoft.Maui.Dispatching;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

// Inspired by https://github.com/dotnet/maui/blob/main/src/Core/tests/UnitTests/TestClasses/DispatcherStub.cs
sealed class MockDispatcherProvider : IDispatcherProvider, IDisposable
{
	static readonly DispatcherMock dispatcherMock = new();

	readonly ThreadLocal<IDispatcher> dispatcherInstance = new(() => dispatcherMock);

	public IDispatcher GetForCurrentThread() => dispatcherInstance.Value ?? throw new InvalidOperationException();

	void IDisposable.Dispose() => dispatcherInstance.Dispose();

	sealed class DispatcherMock : IDispatcher
	{
		public DispatcherMock() => ManagedThreadId = Environment.CurrentManagedThreadId;

		public bool IsDispatchRequired => false;

		public int ManagedThreadId { get; }

		public IDispatcherTimer CreateTimer()
		{
			return new DispatcherTimerStub(this);
		}

		public bool Dispatch(Action action)
		{
			action();

			return true;
		}

		public bool DispatchDelayed(TimeSpan delay, Action action)
		{
			return false;
		}
	}

	sealed class DispatcherTimerStub : IDispatcherTimer, IDisposable
	{
		readonly DispatcherMock dispatcher;

		Timer? timer;

		public DispatcherTimerStub(DispatcherMock dispatcher)
		{
			this.dispatcher = dispatcher;
		}

		public TimeSpan Interval { get; set; }

		public bool IsRepeating { get; set; }

		public bool IsRunning => timer != null;

		public event EventHandler? Tick;

		public void Start()
		{
			timer = new Timer(OnTimeout, null, Interval, IsRepeating ? Interval : Timeout.InfiniteTimeSpan);

			void OnTimeout(object? state)
			{
				dispatcher.Dispatch(() => Tick?.Invoke(this, EventArgs.Empty));
			}
		}

		public void Stop()
		{
			Dispose();
		}

		public void Dispose()
		{
			timer?.Dispose();
			timer = null;
		}
	}
}