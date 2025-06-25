namespace CommunityToolkit.Maui.UnitTests.Mocks;

public sealed class MockDispatcher : IDispatcher
{
	public MockDispatcher() => ManagedThreadId = Environment.CurrentManagedThreadId;

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
		Thread.Sleep(delay);

		action();

		return true;
	}

	sealed class DispatcherTimerStub : IDispatcherTimer, IDisposable
	{
		readonly IDispatcher dispatcher;

		Timer? timer;

		public DispatcherTimerStub(IDispatcher dispatcher)
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