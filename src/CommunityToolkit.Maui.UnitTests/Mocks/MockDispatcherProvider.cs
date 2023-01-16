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

	class DispatcherTimerStub : IDispatcherTimer
	{
		readonly DispatcherMock _dispatcher;

		Timer? _timer;

		public DispatcherTimerStub(DispatcherMock dispatcher)
		{
			_dispatcher = dispatcher;
		}

		public TimeSpan Interval { get; set; }

		public bool IsRepeating { get; set; }

		public bool IsRunning => _timer != null;

		public event EventHandler? Tick;

		public void Start()
		{
			_timer = new Timer(OnTimeout, null, Interval, IsRepeating ? Interval : Timeout.InfiniteTimeSpan);

			void OnTimeout(object? state) {
				_dispatcher.Dispatch(() => Tick?.Invoke(this, EventArgs.Empty));
			}
		}

		public void Stop()
		{
			_timer?.Dispose();
			_timer = null;
		}
	}
}