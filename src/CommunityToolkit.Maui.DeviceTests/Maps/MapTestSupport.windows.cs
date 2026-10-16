using System.Reflection;
using System.Text;
using System.Text.Json;
using CommunityToolkit.Maui.Maps;
using CommunityToolkit.Maui.Maps.Handlers;
using Microsoft.Maui.Controls.Maps;
using Map = Microsoft.Maui.Controls.Maps.Map;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Platform;
using Microsoft.Web.WebView2.Core;
using Windows.Storage.Streams;
using WGrid = Microsoft.UI.Xaml.Controls.Grid;
using WWebView2 = Microsoft.UI.Xaml.Controls.WebView2;

namespace CommunityToolkit.Maui.DeviceTests;

// Support for the Windows map tests (MapHandlerWindows, UseMauiCommunityToolkitMaps, the page messages).
//
// MapHandlerWindows renders https://www.bing.com/api/maps/mapcontrol in a WebView2, which needs the network and a Bing Maps key. The tests
// run the handler's real map page without either: the network state is faked (Connectivity), and the request for the map control is
// answered with BingControlStub, a small stand-in for the Bing control that logs everything the page asks the map to do and raises the
// map events on request. So the C# the handler builds and the JavaScript of its page both run for real, and the tests read the effect
// from the stub's log.

/// <summary>Base of the Windows map tests: a test MauiApp with the Windows map handler, and a faked network.</summary>
public abstract class MapTestBase : CommunityToolkitHandlerTestBase
{
	protected const string mapsTestKey = "device-test-maps-key";

	protected static readonly TimeSpan pageTimeout = TimeSpan.FromSeconds(20);

	/// <summary>The skip reason of every test that creates the handler's WebView2 (MapHandlerWindows.CreatePlatformView).</summary>
	protected const string webView2RuntimeMissingReason = "The WebView2 Runtime is not installed, so the WebView2 of the map handler cannot be created.";

	StaticPropertySnapshot? mapHandlerSnapshot;
	ConnectivityScope? connectivityScope;

	/// <summary>The network state the map handler sees. Offline until the test changes it, so no test loads the real Bing control.</summary>
	protected FakeConnectivity Network { get; } = new(NetworkAccess.None);

	/// <summary>
	/// Builds the test's MauiApp with UseMauiCommunityToolkitMaps(<paramref name="key"/>) and installs the faked network. The handler
	/// registration, and with it the key, only run when MAUI creates the handler factory, so this resolves it.
	/// </summary>
	protected void SetupMapBuilder(string? key = mapsTestKey, Action<MauiAppBuilder>? configureBuilder = null)
	{
		mapHandlerSnapshot = new StaticPropertySnapshot().Capture(typeof(MapHandlerWindows));
		connectivityScope = new ConnectivityScope(Network);

		SetupBuilder(builder =>
		{
			builder.UseMauiCommunityToolkitMaps(key!);
			configureBuilder?.Invoke(builder);
		});

		_ = MauiContext.Handlers;
	}

	/// <summary>
	/// Skips the test (visibly) when the WebView2 Runtime is not installed; fails instead in a run that requires
	/// <see cref="DeviceTestCapabilities.WebView2"/> (the Windows CI jobs), so that a runner without it cannot turn the map tests into skips.
	/// </summary>
	protected static void RequireWebView2Runtime(string unavailableReason = webView2RuntimeMissingReason) =>
		DeviceTestCapabilities.Require(DeviceTestCapabilities.WebView2, IsWebView2RuntimeInstalled(), unavailableReason);

	protected static bool IsWebView2RuntimeInstalled()
	{
		try
		{
			return !string.IsNullOrEmpty(CoreWebView2Environment.GetAvailableBrowserVersionString());
		}
		catch (Exception)
		{
			return false;
		}
	}

	/// <summary>
	/// Hosts <paramref name="map"/> in a window with the handler's real WebView2, installs the Bing control stand-in and, unless
	/// <paramref name="loadPage"/> is false, lets the handler load its map page (it only does so when the network is up, so the
	/// network goes up now). <paramref name="body"/> runs on the UI thread once the page and its stub are ready.
	/// </summary>
	/// <remarks>
	/// Bing loads its modules asynchronously (Microsoft.Maps.loadModule calls back once the module script has arrived), and so does the
	/// stand-in: a page that was loaded here first gets the properties the handler sends when the page has loaded, and only then its
	/// Traffic module (window.__loadModules). What the handler sends before the module is there reaches a page without a TrafficManager.
	/// </remarks>
	protected async Task WithMapPageAsync(Map map, Func<MapHandlerWindows, LoadedMapPage, Task> body, bool loadPage = true)
	{
		RequireWebView2Runtime();

		map.WidthRequest = 400;
		map.HeightRequest = 300;

		await AttachAndRun(map, async handler =>
		{
			var mapHandler = Assert.IsType<MapHandlerWindows>(handler);
			var webView = Assert.IsType<MauiWebView>(handler.PlatformView);
			await webView.EnsureCoreWebView2Async().AsTask().WaitAsync(pageTimeout);
			InstallBingControlStub(webView);
			var page = new LoadedMapPage(webView);

			if (loadPage)
			{
				var pageLoaded = page.WaitForPageLoadedAsync();
				Network.Change(NetworkAccess.Internet);
				await pageLoaded;

				// The handler sends the properties of the map when the page has loaded; the Traffic module arrives after them.
				await page.SettleAsync();
				Assert.Equal(1, await page.LoadModulesAsync());
			}

			Exception? failure = null;

			try
			{
				await body(mapHandler, page);
			}
			catch (Exception exception)
			{
				failure = exception;
				throw;
			}
			finally
			{
				// Nothing may be in flight when the window closes: the handler does not guard its script calls. A WebView2 that the
				// test closed runs no script any more.
				try
				{
					if (!page.IsClosed)
					{
						await page.SettleAsync();
					}
				}
				catch (Exception) when (failure is not null)
				{
					// The failure of the body is the one to report.
				}
			}
		});
	}

	protected override async ValueTask DisposeAsyncCore()
	{
		try
		{
			await base.DisposeAsyncCore();
		}
		finally
		{
			connectivityScope?.Dispose();
			mapHandlerSnapshot?.Restore();
		}
	}

	/// <summary>The log entries of one kind (see BingControlStub), parsed.</summary>
	protected static IReadOnlyList<JsonElement> Entries(IEnumerable<string> log, string kind)
	{
		var entries = new List<JsonElement>();

		foreach (var entry in log)
		{
			using var document = JsonDocument.Parse(entry);
			if (document.RootElement.GetProperty("kind").GetString() == kind)
			{
				entries.Add(document.RootElement.Clone());
			}
		}

		return entries;
	}

	protected static void InstallBingControlStub(WWebView2 webView)
	{
		var core = webView.CoreWebView2;
		core.AddWebResourceRequestedFilter("https://www.bing.com/*", CoreWebView2WebResourceContext.All);
		core.WebResourceRequested += async (sender, args) =>
		{
			var deferral = args.GetDeferral();
			try
			{
				var isMapControl = new Uri(args.Request.Uri).AbsolutePath.Equals("/api/maps/mapcontrol", StringComparison.Ordinal);
				var content = await ToStreamAsync(isMapControl ? BingControlStub : string.Empty);
				args.Response = sender.Environment.CreateWebResourceResponse(content, isMapControl ? 200 : 404, isMapControl ? "OK" : "Not Found", "Content-Type: application/javascript");
			}
			finally
			{
				deferral.Complete();
			}
		};
	}

	static async Task<IRandomAccessStream> ToStreamAsync(string text)
	{
		var stream = new InMemoryRandomAccessStream();
		var writer = new DataWriter(stream);
		writer.WriteString(text);
		await writer.StoreAsync();
		await writer.FlushAsync();
		writer.DetachStream();
		writer.Dispose();
		stream.Seek(0);
		return stream;
	}

	/// <summary>The map page that the handler loaded into its WebView2, and what the tests do with it.</summary>
	public sealed class LoadedMapPage(MauiWebView webView)
	{
		public MauiWebView WebView { get; } = webView;

		/// <summary>Whether the test closed the WebView2 (CloseWebView).</summary>
		public bool IsClosed { get; private set; }

		/// <summary>Closes the WebView2 of the handler (WebView2.Close), as WinUI does when the window of the map goes away.</summary>
		public void CloseWebView()
		{
			WebView.Close();
			IsClosed = true;
		}

		/// <summary>
		/// Completes when a page that was loaded from a string (the handler's map page) has loaded, including its body onload. A timeout names
		/// the navigations that completed meanwhile.
		/// </summary>
		public async Task WaitForPageLoadedAsync()
		{
			var loaded = new TaskCompletionSource();
			var completedNavigations = new List<string>();

			void OnNavigationCompleted(WWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
			{
				var source = sender.CoreWebView2.Source;
				completedNavigations.Add($"{(args.IsSuccess ? "succeeded" : $"failed ({args.WebErrorStatus})")}: {source[..Math.Min(source.Length, 60)]}");

				if (args.IsSuccess && IsPageLoadedFromAString(source))
				{
					loaded.TrySetResult();
				}
			}

			WebView.NavigationCompleted += OnNavigationCompleted;

			try
			{
				await loaded.Task.WaitAsync(pageTimeout);
			}
			catch (TimeoutException exception)
			{
				var navigations = completedNavigations.Count is 0 ? "none" : string.Join("; ", completedNavigations);
				throw new TimeoutException($"The map page did not load within {pageTimeout.TotalSeconds} s. Completed navigations: {navigations}.", exception);
			}
			finally
			{
				WebView.NavigationCompleted -= OnNavigationCompleted;
			}
		}

		// The source that WebView2 reports for a page that NavigateToString loaded (MauiWebView.LoadHtml): a data: URI, or about:blank, which is what
		// WebView2 Runtime 154.0.4258.62 reports. The handler's WebView2 navigates nowhere else before the map page.
		static bool IsPageLoadedFromAString(string source) =>
			source.StartsWith("data:", StringComparison.Ordinal) || source.Equals("about:blank", StringComparison.Ordinal);

		/// <summary>Completes when the WebView2 delivered the next message of the page to the handlers that subscribed before this call.</summary>
		public Task WaitForNextMessageAsync()
		{
			var received = new TaskCompletionSource();

			void OnMessageReceived(WWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
			{
				WebView.WebMessageReceived -= OnMessageReceived;
				received.TrySetResult();
			}

			WebView.WebMessageReceived += OnMessageReceived;
			return received.Task.WaitAsync(pageTimeout);
		}

		public async Task<string> ExecuteAsync(string script) => await WebView.ExecuteScriptAsync(script);

		/// <summary>Runs a script that returns a string.</summary>
		public async Task<string?> ExecuteForStringAsync(string script) => JsonSerializer.Deserialize<string>(await ExecuteAsync(script));

		/// <summary>Empties the log of the page, e.g. to leave out what the page did while it loaded.</summary>
		public async Task ClearLogAsync() => await ExecuteAsync("window.__mapLog.length = 0;");

		/// <summary>
		/// Posts <paramref name="message"/> (the source of a JavaScript value) to the handler the way the page does, and completes when the
		/// handler has received it. The handler subscribed before this call, so it has handled everything it does without awaiting by then.
		/// </summary>
		public async Task PostMessageAsync(string message)
		{
			var delivered = WaitForNextMessageAsync();
			await ExecuteAsync($"window.chrome.webview.postMessage({message});");
			await delivered;
		}

		/// <summary>Runs a script of the stub that raises a map event whose page handler posts a message, and completes once it was delivered.</summary>
		public async Task RaiseMapEventAsync(string script)
		{
			var delivered = WaitForNextMessageAsync();
			await ExecuteAsync(script);
			await delivered;
		}

		public async Task<int> ExecuteForCountAsync(string script) => int.Parse(await ExecuteAsync(script), System.Globalization.CultureInfo.InvariantCulture);

		/// <summary>Lets the stand-in deliver the modules the page asked for (Bing loads them asynchronously); returns how many it delivered.</summary>
		public Task<int> LoadModulesAsync() => ExecuteForCountAsync("window.__loadModules()");

		/// <summary>What the page asked the map to do so far, one JSON object per entry, in the order it happened.</summary>
		public async Task<IReadOnlyList<string>> GetLogAsync() =>
			JsonSerializer.Deserialize<string[]>(await ExecuteAsync("window.__mapLog.map(function (entry) { return JSON.stringify(entry); })")) ?? [];

		/// <summary>Waits until the log satisfies <paramref name="condition"/>, and returns the log. The handler runs its scripts in the background.</summary>
		public async Task<IReadOnlyList<string>> WaitForLogAsync(Func<IReadOnlyList<string>, bool> condition, string message)
		{
			IReadOnlyList<string> log = [];

			try
			{
				await AssertHelpers.AssertEventually(async () =>
				{
					log = await GetLogAsync();
					return condition(log);
				}, timeout: 10000, interval: 50, message: message);
			}
			catch (Xunit.Sdk.XunitException)
			{
				throw new Xunit.Sdk.XunitException($"{message} The page log was: {string.Join(' ', log)}");
			}

			return log;
		}

		/// <summary>Runs two scripts the way the handler does, behind everything it has queued, so that no call of the handler is in flight.</summary>
		public async Task SettleAsync()
		{
			for (var i = 0; i < 2; i++)
			{
				var done = new TaskCompletionSource();
				WebView.DispatcherQueue.TryEnqueue(async () =>
				{
					try
					{
						await WebView.ExecuteScriptAsync("1");
						done.TrySetResult();
					}
					catch (Exception exception)
					{
						done.TrySetException(exception);
					}
				});
				await done.Task.WaitAsync(pageTimeout);
			}
		}
	}

	/// <summary>An IConnectivity whose network state, and the event, the test controls.</summary>
	public sealed class FakeConnectivity(NetworkAccess networkAccess) : IConnectivity
	{
		EventHandler<ConnectivityChangedEventArgs>? connectivityChanged;

		public NetworkAccess NetworkAccess { get; private set; } = networkAccess;

		public IEnumerable<ConnectionProfile> ConnectionProfiles { get; } = [ConnectionProfile.WiFi];

		public event EventHandler<ConnectivityChangedEventArgs>? ConnectivityChanged
		{
			add => connectivityChanged += value;
			remove => connectivityChanged -= value;
		}

		/// <summary>How many handlers listen to the network state.</summary>
		public int SubscriberCount => connectivityChanged?.GetInvocationList().Length ?? 0;

		public void Change(NetworkAccess networkAccess)
		{
			NetworkAccess = networkAccess;
			connectivityChanged?.Invoke(this, new ConnectivityChangedEventArgs(networkAccess, ConnectionProfiles));
		}
	}

	/// <summary>
	/// Replaces Connectivity.Current for the lifetime of the scope. .NET MAUI has no public way to do that (Connectivity.SetCurrent is
	/// internal), so it goes through reflection and fails with a clear message if .NET MAUI changed.
	/// </summary>
	public sealed class ConnectivityScope : IDisposable
	{
		static readonly MethodInfo setCurrent = typeof(Connectivity).GetMethod("SetCurrent", BindingFlags.Static | BindingFlags.NonPublic)
			?? throw new InvalidOperationException($"{typeof(Connectivity).FullName} has no internal 'SetCurrent' method: .NET MAUI changed its Connectivity API.");

		static readonly FieldInfo currentImplementation = typeof(Connectivity).GetField("currentImplementation", BindingFlags.Static | BindingFlags.NonPublic)
			?? throw new InvalidOperationException($"{typeof(Connectivity).FullName} has no 'currentImplementation' field: .NET MAUI changed its Connectivity API.");

		readonly object? previous;

		public ConnectivityScope(IConnectivity replacement)
		{
			previous = currentImplementation.GetValue(null);
			setCurrent.Invoke(null, [replacement]);
		}

		public void Dispose() => setCurrent.Invoke(null, [previous]);
	}

	/// <summary>A map handler whose platform view is no WebView2, so its map mappings have no page to talk to.</summary>
	protected sealed partial class NoWebViewMapHandler : MapHandlerWindows
	{
		protected override Microsoft.UI.Xaml.FrameworkElement CreatePlatformView() => new WGrid();
	}

	// A stand-in for the Bing control (see the comment at the top of the file).
	public const string BingControlStub = """
		// Stand-in for https://www.bing.com/api/maps/mapcontrol: the device tests serve this script instead of the real control, so the map page
		// of MapHandlerWindows runs offline. It keeps a log of everything the page asks the map to do (window.__mapLog), lets a test raise
		// the map events (window.__renderMap, __clickMap, __clickInfobox, __clickPin), which makes the page post its messages to the handler,
		// and, like Bing, delivers the modules the page loads asynchronously: only when the test calls window.__loadModules().
		(function () {
			var log = [];
			var listeners = [];
			var maps = {};
			var pendingModules = [];

			function describeLocation(location) {
				return location && typeof location.latitude === 'number' ? { latitude: location.latitude, longitude: location.longitude } : { notALocation: String(location) };
			}

			function describeEntity(entity) {
				return { latitude: entity.getLocation().latitude, longitude: entity.getLocation().longitude, title: entity.options && entity.options.title, subTitle: entity.options && entity.options.subTitle, markerId: entity.markerId };
			}

			function describeView(options) {
				var view = {};
				if (options.mapTypeId !== undefined) view.mapTypeId = options.mapTypeId;
				if (options.center !== undefined) view.center = describeLocation(options.center);
				if (options.bounds !== undefined) view.bounds = { center: describeLocation(options.bounds.center), width: options.bounds.width, height: options.bounds.height };
				return view;
			}

			function Location(latitude, longitude) {
				this.latitude = latitude;
				this.longitude = longitude;
				this.altitude = 0;
				this.altitudeReference = -1;
			}

			function LocationRect(center, width, height) {
				this.center = center;
				this.width = width;
				this.height = height;
			}

			function Pushpin(location, options) {
				this.options = options;
				this.getLocation = function () { return location; };
				this.getTitle = function () { return options && options.title; };
				this.getSubTitle = function () { return options && options.subTitle; };
			}

			// Shown in the element #__infobox the way Bing shows its infobox: the title as text (a TextBlock, whose root is a text node) and the
			// description as HTML (the innerHTML of an element of the infobox).
			function Infobox(location, options) {
				var visible = options && options.visible;
				var element = document.createElement('div');
				var title = document.createTextNode('');
				var description = document.createElement('div');
				element.id = '__infobox';
				element.appendChild(title);
				element.appendChild(description);
				(document.body || document.documentElement).appendChild(element);
				this.setMap = function () { };
				this.setOptions = function (newOptions) {
					log.push({ kind: 'infobox', options: { visible: newOptions.visible, title: newOptions.title, description: newOptions.description } });
					if (newOptions.visible !== undefined) visible = newOptions.visible;
					if (newOptions.title !== undefined) title.nodeValue = newOptions.title;
					if (typeof newOptions.description === 'string') description.innerHTML = newOptions.description;
				};
				this.getVisible = function () { return visible; };
				maps.infobox = this;
			}

			function Map(element, options) {
				log.push({ kind: 'map', options: options });
				this.entities = {
					push: function (entity) { log.push({ kind: 'push', entity: describeEntity(entity) }); maps.lastPushed = entity; },
					remove: function (entity) { log.push({ kind: 'remove', entity: describeEntity(entity) }); },
					clear: function () { log.push({ kind: 'clear' }); }
				};
				this.setView = function (viewOptions) { log.push({ kind: 'setView', view: describeView(viewOptions) }); };
				this.setOptions = function (newOptions) { log.push({ kind: 'setOptions', options: newOptions }); };
				this.getCenter = function () { return new Location(0, 0); };
				this.getBounds = function () { return window.__mapBounds; };
				maps.map = this;
			}

			function TrafficManager() {
				this.show = function () { log.push({ kind: 'traffic', visible: true }); };
				this.hide = function () { log.push({ kind: 'traffic', visible: false }); };
			}

			window.Microsoft = {
				Maps: {
					MapTypeId: { road: 'road', aerial: 'aerial' },
					Location: Location,
					LocationRect: LocationRect,
					Pushpin: Pushpin,
					Infobox: Infobox,
					Map: Map,
					Traffic: { TrafficManager: TrafficManager },
					loadModule: function (name, callback) { log.push({ kind: 'module', name: name }); pendingModules.push(callback); },
					Events: { addHandler: function (target, name, handler) { listeners.push({ target: target, name: name, handler: handler }); } }
				}
			};

			window.__mapLog = log;
			window.__mapBounds = { center: new Location(0, 0), width: 0, height: 0 };
			// Calls back every module the page asked for so far (Bing does so once the module script has arrived); returns how many.
			window.__loadModules = function () {
				var callbacks = pendingModules.splice(0);
				for (var i = 0; i < callbacks.length; i++) {
					callbacks[i]();
				}
				return callbacks.length;
			};

			function fire(target, name, args) {
				var raised = 0;
				for (var i = 0; i < listeners.length; i++) {
					if (listeners[i].target === target && listeners[i].name === name) {
						raised++;
						listeners[i].handler(args);
					}
				}
				return raised;
			}

			// Each of these raises one Bing event on the page and returns how many page handlers received it.
			window.__renderMap = function (bounds) { window.__mapBounds = bounds; return fire(maps.map, 'viewrendered', {}); };
			window.__clickMap = function (latitude, longitude, isPrimary) { return fire(maps.map, 'click', { isPrimary: isPrimary, location: new Location(latitude, longitude) }); };
			window.__clickInfobox = function () { return fire(maps.infobox, 'click', { target: maps.infobox }); };
			// targetType is what Bing reports for the clicked entity; the page only reacts to 'pushpin'.
			window.__clickPin = function (markerId, targetType) {
				for (var i = 0; i < listeners.length; i++) {
					var target = listeners[i].target;
					if (listeners[i].name === 'click' && target.markerId === markerId) {
						return fire(target, 'click', { targetType: targetType || 'pushpin', target: target });
					}
				}
				return -1;
			};
		})();
		""";
}