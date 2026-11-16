using System.Reflection;
using Android.Content;
using Android.Content.Res;
using AndroidX.Media3.ExoPlayer;
using AndroidX.Media3.Session;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.DeviceTests.Runners;
using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.MediaElementAndroid)]
[Collection(TestCollections.DisableParallelization)]
public class MediaManagerLifecycleAndroidTests : MediaElementAndroidTestBase
{
	[Theory]
	[InlineData("context")]
	[InlineData("mediaElement")]
	[InlineData("dispatcher")]
	public async Task ConstructorRejectsANullArgument(string nullArgument)
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();

		await InvokeOnMainThreadAsync(() =>
		{
			var exception = Assert.Throws<ArgumentNullException>(() => new AndroidMediaManagerProbe(
				nullArgument == "context" ? null! : MauiContext,
				nullArgument == "mediaElement" ? null! : mediaElement,
				nullArgument == "dispatcher" ? null! : TestDispatcher.Current));

			Assert.Equal(nullArgument, exception.ParamName);
		});
	}

	[Fact]
	public async Task ConstructorNeedsALoggerFactoryInTheServicesOfTheContext()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync();

		await InvokeOnMainThreadAsync(() =>
		{
			var contextWithoutLogging = new Microsoft.Maui.MauiContext(new ServiceCollection().BuildServiceProvider(), MauiContext.Context!);

			Assert.Throws<InvalidOperationException>(() => new AndroidMediaManagerProbe(contextWithoutLogging, mediaElement, TestDispatcher.Current));
		});
	}

	[Fact]
	public async Task UpdatingTheSourceWithoutAPlayerDoesNothing()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		DisposeWhenTheTestEnds(mediaElement);

		await InvokeOnMainThreadAsync(async () =>
		{
			using var manager = new AndroidMediaManagerProbe(MauiContext, mediaElement, TestDispatcher.Current);

			await manager.UpdateSource();

			Assert.Null(manager.ExposedPlayer);
			Assert.Equal(MediaElementState.None, mediaElement.CurrentState);
		});
	}

	[Fact]
	public async Task DisposeReleasesTheStreamFactoryTheTokenSourceAndTheClient()
	{
		SetupProbeBuilder();
		var tone = await ReadAssetAsync(toneAsset);
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromStream(new MemoryStream(tone)));
		using var recorder = new MediaElementRecorder(mediaElement);
		StreamDataSourceFactory? factory = null;
		CancellationTokenSource? tokenSource = null;
		HttpClient? client = null;

		await HostAsync(mediaElement, async handler =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);
			var manager = handler.Manager!;
			factory = (StreamDataSourceFactory?)GetPrivateField(manager, "currentStreamDataSourceFactory");
			tokenSource = (CancellationTokenSource?)GetPrivateField(manager, "cancellationTokenSource");
			client = (HttpClient?)GetPrivateField(manager, "client");
			Assert.NotNull(factory);
			Assert.NotNull(tokenSource);
		});
		// HostAsync disconnected the handler, which disposed the manager.

		Assert.Equal(IntPtr.Zero, factory!.Handle);
		Assert.Throws<ObjectDisposedException>(() => tokenSource!.Token);
		await Assert.ThrowsAsync<ObjectDisposedException>(() => client!.GetAsync("http://127.0.0.1:1/"));
	}

	[Fact]
	public async Task DisposeWithoutDisposingReleasesNothing()
	{
		SetupProbeBuilder();
		var tone = await ReadAssetAsync(toneAsset);
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromStream(new MemoryStream(tone)));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);
			var manager = handler.Manager!;
			var factory = (StreamDataSourceFactory)GetPrivateField(manager, "currentStreamDataSourceFactory")!;
			var tokenSource = (CancellationTokenSource)GetPrivateField(manager, "cancellationTokenSource")!;
			var client = (HttpClient)GetPrivateField(manager, "client")!;
			var disposeCore = typeof(MediaManager).GetMethod("Dispose", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, [typeof(bool)])!;

			// The finalizer path: the managed objects may be finalized already, so nothing may be released.
			disposeCore.Invoke(manager, [false]);

			Assert.NotEqual(IntPtr.Zero, factory.Handle);
			Assert.False(tokenSource.IsCancellationRequested);
			Assert.Null(Record.Exception(() => tokenSource.Token));
			var request = await Record.ExceptionAsync(() => client.GetAsync("http://127.0.0.1:1/"));
			Assert.NotNull(request);
			Assert.IsNotType<ObjectDisposedException>(request);
		});
	}

	[Fact]
	public async Task DisposeReleasesTheRegistrationOfTheServiceAndAllowsTheSessionIdAgain()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var tone = await ReadAssetAsync(toneAsset);
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromStream(new MemoryStream(tone)));
		using var recorder = new MediaElementRecorder(mediaElement);
		string? sessionId = null;
		StreamDataSourceFactory? factory = null;

		await HostAsync(mediaElement, async handler =>
		{
			await WaitForRegistrationAsync(handler.Manager!);
			await WaitUntilLoadedAsync(mediaElement, recorder);
			var manager = handler.Manager!;
			sessionId = GetMediaSession(manager)!.Id;
			factory = (StreamDataSourceFactory?)GetPrivateField(manager, "currentStreamDataSourceFactory");
			Assert.NotNull(factory);
			Assert.Throws<Java.Lang.IllegalStateException>(() => BuildAndReleaseSession(sessionId!));
		});
		// HostAsync disconnected the handler, which sent the release command to the service.

		await WaitUntilAsync(() => !GetRegisteredPlayerIds().Contains(sessionId!), "The service never released the registration of the disconnected MediaElement.");
		await InvokeOnMainThreadAsync(() => BuildAndReleaseSession(sessionId!));
		Assert.Equal(IntPtr.Zero, factory!.Handle);
	}

	[Fact]
	public async Task DisposeWithoutDisposingReleasesNoRegistrationOfTheService()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async handler =>
		{
			await WaitForRegistrationAsync(handler.Manager!);
			await WaitUntilLoadedAsync(mediaElement, recorder);
			var manager = handler.Manager!;
			var sessionId = GetMediaSession(manager)!.Id!;
			var disposeCore = typeof(MediaManager).GetMethod("Dispose", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, [typeof(bool)])!;

			disposeCore.Invoke(manager, [false]);

			Assert.Throws<Java.Lang.IllegalStateException>(() => BuildAndReleaseSession(sessionId));
			Assert.Contains(sessionId, GetRegisteredPlayerIds());
		});
	}

	[Fact]
	public async Task CreatingThePlatformViewAgainKeepsTheOneControllerAndRegistration()
	{
		SetupProbeBuilder(isAndroidForegroundServiceEnabled: true);
		var mediaElement = await CreateMediaElementAsync();

		await WithHandlerAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await WaitForRegistrationAsync(manager);
			await WaitForControllerAsync(mediaElement);
			var controllerHandle = GetController(mediaElement).Handle;
			var session = GetMediaSession(manager)!;
			var sessionId = session.Id;

			var secondPlatformView = handler.CreatePlatformViewDirectly();
			try
			{
				Assert.Equal(controllerHandle, GetController(mediaElement).Handle);
				Assert.Same(session, GetMediaSession(manager));
				Assert.Equal(sessionId, Assert.Single(GetRegisteredPlayerIds()));
			}
			finally
			{
				secondPlatformView.Dispose();
			}
		});
	}

	[Fact]
	public async Task TextureViewNeedsTheResourcesOfTheContext()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(() => new MediaElement { AndroidViewType = AndroidViewType.TextureView });

		await InvokeOnMainThreadAsync(() =>
		{
			var context = new Microsoft.Maui.MauiContext(MauiContext.Services, new ContextWithoutResources(MauiContext.Context!));
			var manager = new AndroidMediaManagerProbe(context, mediaElement, TestDispatcher.Current);
			try
			{
				var exception = Assert.Throws<InvalidOperationException>(() => manager.CreatePlatformView(AndroidViewType.TextureView, false));

				Assert.Contains("Android Resources", exception.Message);
			}
			finally
			{
				manager.Dispose();
			}
		});
	}

	[Fact]
	public async Task SourceWithoutDataAfterAValidSourceDoesNotOpenTheOldMediaAgain()
	{
		SetupProbeBuilder();
		var mediaElement = await CreateMediaElementAsync(element => element.Source = MediaSource.FromResource(toneAsset));
		using var recorder = new MediaElementRecorder(mediaElement);

		await HostAsync(mediaElement, async _ =>
		{
			await WaitUntilLoadedAsync(mediaElement, recorder);
			Assert.Equal(1, recorder.OpenedCount);

			mediaElement.Source = new UriMediaSource();

			// MediaOpened is raised synchronously by the source mapping when media is (re)loaded.
			Assert.Equal(1, recorder.OpenedCount);
		});
	}

	static object? GetPrivateField(object instance, string name) =>
		instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance);

	sealed class ContextWithoutResources(Context baseContext) : ContextWrapper(baseContext)
	{
		public override Resources? Resources => null;
	}
}