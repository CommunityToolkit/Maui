using Microsoft.Maui.Dispatching;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

// Inspired by https://github.com/dotnet/maui/blob/main/src/Core/tests/UnitTests/TestClasses/DispatcherStub.cs
sealed class DispatcherProviderMock : IDispatcherProvider, IDisposable
{
	readonly static DispatcherMock dispatcherMock = new();

	readonly ThreadLocal<IDispatcher> dispatcherInstance = new(() => dispatcherMock);

	public IDispatcher GetForCurrentThread() => dispatcherInstance.Value ?? throw new InvalidOperationException();

	void IDisposable.Dispose() => dispatcherInstance.Dispose();

	sealed class DispatcherMock : IDispatcher
	{
		public DispatcherMock() => ManagedThreadId = Environment.CurrentManagedThreadId;

		public bool IsDispatchRequired => false;

		public int ManagedThreadId { get; }

		public bool Dispatch(Action action)
		{
			action();

			return true;
		}
	}
}