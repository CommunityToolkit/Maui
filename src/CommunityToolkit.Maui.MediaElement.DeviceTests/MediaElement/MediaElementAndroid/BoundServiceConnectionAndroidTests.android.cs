using Android.Content;
using Android.OS;
using CommunityToolkit.Maui.Media.Services;
using CommunityToolkit.Maui.Services;

namespace CommunityToolkit.Maui.DeviceTests;

// The connection is driven with a MediaControlsService object that the test creates itself and that the system never starts, so no real
// service is involved (MediaControlsServiceAndroidTests runs the real service). Such an object can raise TaskRemoved, which is all the
// connection listens to. The service objects are not disposed: an object that was never attached to the system cannot leave the
// foreground (MediaControlsService.Dispose asks the ActivityManager to), and it is garbage collected with the test.
[Category(TestCategory.MediaElementAndroid)]
public class BoundServiceConnectionAndroidTests
{
	static IServiceConnection AsServiceConnection(BoundServiceConnection connection) => connection;

	static BoundServiceConnection CreateConnectedConnection(MediaControlsService service)
	{
		var connection = new BoundServiceConnection(null!);
		AsServiceConnection(connection).OnServiceConnected(null, new BoundServiceBinder(service));
		return connection;
	}

	[Fact]
	public void NewConnectionIsNotConnected()
	{
		var connection = new BoundServiceConnection(null!);

		Assert.False(connection.IsConnected);
		Assert.Null(connection.Binder);
		Assert.Null(connection.Activity);
	}

	[Fact]
	public void ConnectingToTheBinderOfTheServiceStoresItAndReportsConnected()
	{
		var service = new MediaControlsService();
		var binder = new BoundServiceBinder(service);
		var connection = new BoundServiceConnection(null!);

		AsServiceConnection(connection).OnServiceConnected(null, binder);

		Assert.True(connection.IsConnected);
		Assert.Same(binder, connection.Binder);
		Assert.Same(service, connection.Binder!.Service);
	}

	[Fact]
	public void ConnectingToAnotherKindOfBinderLeavesTheConnectionDisconnected()
	{
		var connection = new BoundServiceConnection(null!);

		AsServiceConnection(connection).OnServiceConnected(null, new Binder());
		Assert.False(connection.IsConnected);
		Assert.Null(connection.Binder);

		AsServiceConnection(connection).OnServiceConnected(null, null);
		Assert.False(connection.IsConnected);
		Assert.Null(connection.Binder);
	}

	[Fact]
	public void ConnectingWithoutAMediaManagerDoesNotThrow()
	{
		var connection = new BoundServiceConnection(null!);

		var exception = Record.Exception(() => AsServiceConnection(connection).OnServiceConnected(null, new BoundServiceBinder(new MediaControlsService())));

		Assert.Null(exception);
		Assert.True(connection.IsConnected);
	}

	[Fact]
	public void DisconnectingClearsTheBinder()
	{
		var connection = CreateConnectedConnection(new MediaControlsService());
		Assert.True(connection.IsConnected);

		AsServiceConnection(connection).OnServiceDisconnected(null);

		Assert.False(connection.IsConnected);
		Assert.Null(connection.Binder);
	}

	[Fact]
	public void DisconnectingAConnectionThatWasNeverConnectedDoesNothing()
	{
		var connection = new BoundServiceConnection(null!);

		var exception = Record.Exception(() => AsServiceConnection(connection).OnServiceDisconnected(null));

		Assert.Null(exception);
		Assert.False(connection.IsConnected);
	}

	[Fact]
	public void TaskRemovedOfTheConnectedServiceReachesTheSubscribers()
	{
		var service = new MediaControlsService();
		var connection = CreateConnectedConnection(service);
		var subscriber = new TaskRemovedSubscriber();
		connection.MediaControlsServiceTaskRemoved += subscriber.Handle;

		service.OnTaskRemoved(null);
		service.OnTaskRemoved(null);

		Assert.Equal(2, subscriber.Count);
		Assert.Same(connection, subscriber.Sender);
		GC.KeepAlive(subscriber);
	}

	[Fact]
	public void TaskRemovedNoLongerReachesASubscriberThatUnsubscribed()
	{
		var service = new MediaControlsService();
		var connection = CreateConnectedConnection(service);
		var subscriber = new TaskRemovedSubscriber();
		var other = new TaskRemovedSubscriber();
		connection.MediaControlsServiceTaskRemoved += subscriber.Handle;
		connection.MediaControlsServiceTaskRemoved += other.Handle;
		service.OnTaskRemoved(null);

		connection.MediaControlsServiceTaskRemoved -= subscriber.Handle;
		service.OnTaskRemoved(null);

		Assert.Equal(1, subscriber.Count);
		Assert.Equal(2, other.Count);
		GC.KeepAlive(subscriber);
		GC.KeepAlive(other);
	}

	[Fact]
	public void TaskRemovedNoLongerReachesSubscribersAfterTheServiceDisconnected()
	{
		var service = new MediaControlsService();
		var connection = CreateConnectedConnection(service);
		var subscriber = new TaskRemovedSubscriber();
		connection.MediaControlsServiceTaskRemoved += subscriber.Handle;
		service.OnTaskRemoved(null);
		Assert.Equal(1, subscriber.Count);

		AsServiceConnection(connection).OnServiceDisconnected(null);
		service.OnTaskRemoved(null);

		Assert.Equal(1, subscriber.Count);
		GC.KeepAlive(subscriber);
	}

	[Fact]
	public void TaskRemovedOfAServiceThatTheConnectionNeverBoundIsNotForwarded()
	{
		var connection = CreateConnectedConnection(new MediaControlsService());
		var subscriber = new TaskRemovedSubscriber();
		connection.MediaControlsServiceTaskRemoved += subscriber.Handle;

		new MediaControlsService().OnTaskRemoved(null);

		Assert.Equal(0, subscriber.Count);
		GC.KeepAlive(subscriber);
	}

	[Fact]
	public void BinderExposesTheServiceItWasCreatedFor()
	{
		var service = new MediaControlsService();

		var binder = new BoundServiceBinder(service);

		Assert.Same(service, binder.Service);
	}

	// The connection keeps subscribers weakly (WeakEventManager): a lambda would be collected, so the subscriber is an object that the
	// test keeps alive.
	sealed class TaskRemovedSubscriber
	{
		public int Count { get; private set; }

		public object? Sender { get; private set; }

		public void Handle(object? sender, EventArgs e)
		{
			Sender = sender;
			Count++;
		}
	}
}