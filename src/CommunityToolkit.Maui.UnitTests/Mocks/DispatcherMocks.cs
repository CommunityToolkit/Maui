using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Dispatching;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

// Inspired by https://github.com/dotnet/maui/blob/main/src/Core/tests/UnitTests/TestClasses/DispatcherStub.cs
sealed class DispatcherProviderMock : IDispatcherProvider, IDisposable
{
	readonly static DispatcherMock _dispatcherMock = new();

	readonly ThreadLocal<IDispatcher> _dispatcherInstance = new(() => _dispatcherMock);

	public IDispatcher GetForCurrentThread() => _dispatcherInstance.Value ?? throw new InvalidOperationException();

	void IDisposable.Dispose() => _dispatcherInstance.Dispose();

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