using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Maps.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Windows.Devices.Geolocation;
using IMap = Microsoft.Maui.Maps.IMap;

namespace CommunityToolkit.Maui.Maps.Handlers;

/// <inheritdoc />
public partial class MapHandlerWindows : MapHandler
{
	static readonly ConditionalWeakTable<WebView2, Task> pendingInitializations = new();

	MapSpan? regionToGo;

	/// <summary>
	/// Initializes a new instance of the <see cref="MapHandlerWindows"/> class.
	/// </summary>
	public MapHandlerWindows() : base(Mapper, CommandMapper)
	{
		Mapper.ModifyMapping(nameof(IMap.MapType), (handler, map, _) => MapMapType(handler, map));
		Mapper.ModifyMapping(nameof(IMap.IsShowingUser), async (handler, map, _) => await MapIsShowingUser(handler, map));
		Mapper.ModifyMapping(nameof(IMap.IsScrollEnabled), (handler, map, _) => MapIsScrollEnabled(handler, map));
		Mapper.ModifyMapping(nameof(IMap.IsTrafficEnabled), (handler, map, _) => MapIsTrafficEnabled(handler, map));
		Mapper.ModifyMapping(nameof(IMap.IsZoomEnabled), (handler, map, _) => MapIsZoomEnabled(handler, map));
		Mapper.ModifyMapping(nameof(IMap.Pins), async (handler, map, _) => await MapPins(handler, map));
		Mapper.ModifyMapping(nameof(IMap.Elements), (handler, map, _) => MapElements(handler, map));
		CommandMapper.ModifyMapping(nameof(IMap.MoveToRegion), async (handler, map, args, _) => await MapMoveToRegion(handler, map, args));
	}

	internal static string? MapsKey { get; set; }
	internal static string? MapPage { get; private set; }

	/// <summary>
	/// Maps Map type
	/// </summary>
	public static new Task MapMapType(IMapHandler handler, IMap map)
	{
		return TryCallJSMethod(handler.PlatformView, $"setMapType('{map.MapType}');");
	}

	/// <summary>
	/// Maps IsZoomEnabled
	/// </summary>
	public static new Task MapIsZoomEnabled(IMapHandler handler, IMap map)
	{
		return TryCallJSMethod(handler.PlatformView, $"disableMapZoom({(!map.IsZoomEnabled).ToString().ToLower()});");
	}

	/// <summary>
	/// Maps IsScrollEnabled
	/// </summary>
	public static new Task MapIsScrollEnabled(IMapHandler handler, IMap map)
	{
		return TryCallJSMethod(handler.PlatformView, $"disablePanning({(!map.IsScrollEnabled).ToString().ToLower()});");
	}

	/// <summary>
	/// Maps IsTrafficEnabled
	/// </summary>
	public static new Task MapIsTrafficEnabled(IMapHandler handler, IMap map)
	{
		return TryCallJSMethod(handler.PlatformView, $"disableTraffic({(!map.IsTrafficEnabled).ToString().ToLower()});");
	}

	/// <summary>
	/// Maps IsShowingUser
	/// </summary>
	public static new async Task MapIsShowingUser(IMapHandler handler, IMap map)
	{
		if (map.IsShowingUser)
		{
			var location = await GetCurrentLocation();
			if (location is not null)
			{
				await TryCallJSMethod(handler.PlatformView, $"addLocationPin({location.Latitude.ToString(CultureInfo.InvariantCulture)},{location.Longitude.ToString(CultureInfo.InvariantCulture)});");
			}
		}
		else
		{
			await TryCallJSMethod(handler.PlatformView, "removeLocationPin();");
		}
	}

	/// <summary>
	/// Maps Pins: replaces the pins of the map page with the pins of <paramref name="map"/>, so that pins added one after the other are each on
	/// the map once. A pin without a <see cref="IMapPin.Location"/> is left out.
	/// </summary>
	/// <remarks>
	/// The info window that a click on a pin opens shows the pin's <see cref="Pin.Label"/> and <see cref="Pin.Address"/> as plain text: markup in
	/// them is shown as it is written, not rendered as HTML.
	/// </remarks>
	public static new async Task MapPins(IMapHandler handler, IMap map)
	{
		// One script per change replaces the pins with those of this moment. The scripts run in the order they are sent, so pins changed several
		// times in a row (added in a loop) end up on the map once each. A removal sent on its own ran before the additions of the earlier changes,
		// which, built after an await, each added the latest pins again
		var script = new StringBuilder("removeAllPins();");

		foreach (var pin in map.Pins)
		{
			// A Pin has no Location until one is set (a Map only requires its Label): there is no position to show it at
			if (pin.Location is null)
			{
				continue;
			}

			// The label and the address are text of the app: encoded for the JavaScript string literals, so that none of their characters ends the literal or runs as script
			script.Append($"addPin({pin.Location.Latitude.ToString(CultureInfo.InvariantCulture)}," +
				$"{pin.Location.Longitude.ToString(CultureInfo.InvariantCulture)},'{JavaScriptEncoder.Default.Encode(pin.Label ?? string.Empty)}', '{JavaScriptEncoder.Default.Encode(pin.Address ?? string.Empty)}', '{(pin as Pin)?.Id}');");
		}

		await TryCallJSMethod(handler.PlatformView, script.ToString());
	}

	/// <summary>
	/// Map Elements
	/// </summary>
	public static new void MapElements(IMapHandler handler, IMap map) { }

	/// <summary>
	/// Maps MoveToRegion
	/// </summary>
	public static new async Task MapMoveToRegion(IMapHandler handler, IMap map, object? arg)
	{
		if (arg is not MapSpan newRegion)
		{
			return;
		}

		if (handler is MapHandlerWindows mapHandler)
		{
			mapHandler.regionToGo = newRegion;
		}

		await TryCallJSMethod(handler.PlatformView, $"setRegion({newRegion.Center.Latitude.ToString(CultureInfo.InvariantCulture)},{newRegion.Center.Longitude.ToString(CultureInfo.InvariantCulture)},{newRegion.LatitudeDegrees.ToString(CultureInfo.InvariantCulture)},{newRegion.LongitudeDegrees.ToString(CultureInfo.InvariantCulture)});");
	}

	/// <inheritdoc/>
	protected override FrameworkElement CreatePlatformView()
	{
		if (string.IsNullOrEmpty(MapsKey))
		{
			throw new InvalidOperationException("You need to specify a Bing Maps Key");
		}

		MapPage = GetMapHtmlPage(MapsKey);
		var webView = new MauiWebView(new WebViewHandler());

		return webView;
	}

	/// <inheritdoc />
	protected override void ConnectHandler(FrameworkElement platformView)
	{
		if (platformView is MauiWebView mauiWebView)
		{
			LoadMap(platformView);

			mauiWebView.NavigationCompleted += HandleWebViewNavigationCompleted;
			mauiWebView.WebMessageReceived += WebViewWebMessageReceived;
			Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
		}

		base.ConnectHandler(platformView);
	}

	/// <inheritdoc />
	protected override void DisconnectHandler(FrameworkElement platformView)
	{
		if (platformView is MauiWebView mauiWebView)
		{
			Connectivity.ConnectivityChanged -= Connectivity_ConnectivityChanged;
			mauiWebView.NavigationCompleted -= HandleWebViewNavigationCompleted;
			mauiWebView.WebMessageReceived -= WebViewWebMessageReceived;
		}

		base.DisconnectHandler(platformView);
	}

	// WebView2.EnsureCoreWebView2Async completes only the first and the last of the calls made while the CoreWebView2 is being created (six calls
	// in a row: the four in between never complete), and what awaits such a call stays alive with it: the mapping that called TryCallJSMethod,
	// with the handler and the map. The handler connects with a script call per property, so a WebView2 that is being initialized has one
	// initialization, which every script call awaits; once it has completed, a call initializes (or finds closed) the WebView2 itself.
	static Task WaitForCoreWebView2Async(WebView2 webView2)
	{
		if (pendingInitializations.TryGetValue(webView2, out var pendingInitialization))
		{
			return pendingInitialization;
		}

		var initialization = webView2.EnsureCoreWebView2Async().AsTask();
		if (!initialization.IsCompleted)
		{
			pendingInitializations.AddOrUpdate(webView2, initialization);
			_ = initialization.ContinueWith(_ => pendingInitializations.Remove(webView2), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
		}

		return initialization;
	}

	static async Task<bool> TryCallJSMethod(FrameworkElement platformWebView, string script)
	{
		if (platformWebView is not WebView2 webView2)
		{
			return false;
		}

		try
		{
			await WaitForCoreWebView2Async(webView2);
		}
		catch (ObjectDisposedException)
		{
			// The WebView2 was closed (WebView2.Close, e.g. when its window closed): EnsureCoreWebView2Async fails with RO_E_CLOSED and no script runs any more
			return false;
		}

		var tcs = new TaskCompletionSource<bool>();
		var isEnqueueSuccessful = webView2.DispatcherQueue.TryEnqueue(async () =>
		{
			try
			{
				await webView2.ExecuteScriptAsync(script);
				tcs.SetResult(true);
			}
			catch (InvalidOperationException)
			{
				// The WebView2 was closed after EnsureCoreWebView2Async: without its CoreWebView2, ExecuteScriptAsync fails with E_ILLEGAL_METHOD_CALL
				tcs.SetResult(false);
			}
		});

		if (!isEnqueueSuccessful)
		{
			return false;
		}

		return await tcs.Task;
	}

	static string GetMapHtmlPage(string key)
	{
		var str = @$"<!DOCTYPE html>
				<html>
					<head>
						<title></title>
						<meta http-equiv=""Content-Security-Policy"" content=""default-src 'self' data: gap: https://ssl.gstatic.com 'unsafe-eval' 'unsafe-inline' https://*.bing.com https://*.virtualearth.net; style-src 'self' 'unsafe-inline' https://*.bing.com https://*.virtualearth.net; media-src *"">
						<meta name=""viewport"" content=""user-scalable=no, initial-scale=1, maximum-scale=1, minimum-scale=1, width=device-width"">
						<script type='text/javascript' src='https://www.bing.com/api/maps/mapcontrol?key={key}'></script>";
		str += @"<script type='text/javascript'>
			                var map;
							var locationPin;
							var trafficManager;
							var isTrafficDisabled;
							var infobox;

			                function loadMap() {
			                    map = new Microsoft.Maps.Map(document.getElementById('myMap'), {
									disableBirdseye : true,
								//	disableZooming: true,
								//	disablePanning: true,
									showScalebar: false,
									showLocateMeButton: false,
									showDashboard: false,
									showTermsLink: false,
									showTrafficButton: false
								});
								loadTrafficModule();
								Microsoft.Maps.Events.addHandler(map, 'viewrendered', function () { var bounds = map.getBounds(); invokeHandlerAction('BoundsChanged', bounds); });

								Microsoft.Maps.Events.addHandler(map, 'click', function (e) {
									if (!e.isPrimary) {
										return;
									}

									var clickedLocation = {
										latitude: e.location.latitude,
										longitude: e.location.longitude
									};

									invokeHandlerAction('MapClicked', clickedLocation);
								});

								infobox = new Microsoft.Maps.Infobox(map.getCenter(), {
									visible: false
								});
								infobox.setMap(map);
								Microsoft.Maps.Events.addHandler(infobox, 'click', function (args) {
									// If not visible, we assume the user pressed the close button
									if (args.target.getVisible() == false)
										return;

									var infoWindow = {
										infoWindowMarkerId: infobox.infoWindowMarkerId									
									};

									invokeHandlerAction('InfoWindowClicked', infoWindow);
								});
			                }
							
							function loadTrafficModule()
							{
								Microsoft.Maps.loadModule('Microsoft.Maps.Traffic', function () {
									 trafficManager = new Microsoft.Maps.Traffic.TrafficManager(map);

									// Bing loads the module asynchronously: apply the traffic setting that arrived before the module
									if (isTrafficDisabled !== undefined)
										disableTraffic(isTrafficDisabled);
								});
							}

							function disableTraffic(disable)
							{
								isTrafficDisabled = disable;

								// Without the Traffic module there is no TrafficManager yet: the module applies the setting once it has loaded
								if (trafficManager === undefined)
									return;

								if(disable)
									trafficManager.hide();
								else
									trafficManager.show();
							}

							function disableMapZoom(disable)
							{
								map.setOptions({
									disableZooming: disable,
								});
							}
							
							function disablePanning(disable)
							{
								map.setOptions({
									disablePanning: disable,
								});
							}
			
							function setMapType(mauiMapType)
							{
								var mapTypeID = Microsoft.Maps.MapTypeId.road;
								switch(mauiMapType) {
								  case 'Street':
								    mapTypeID = Microsoft.Maps.MapTypeId.road;
								    break;
								  case 'Satellite':
								    mapTypeID = Microsoft.Maps.MapTypeId.aerial;
								    break;
								  case 'Hybrid':
								    mapTypeID = Microsoft.Maps.MapTypeId.aerial;
									break;
								  default:
									mapTypeID = Microsoft.Maps.MapTypeId.road;
								}
								map.setView({
									mapTypeId: mapTypeID
								});
							}

							function setRegion(latitude, longitude, latitudeDegrees, longitudeDegrees)
							{
								// LocationRect(center, width, height): the width is the longitude extent and the height the latitude extent
								map.setView({bounds: new Microsoft.Maps.LocationRect(new Microsoft.Maps.Location(latitude, longitude), longitudeDegrees, latitudeDegrees) });
							}

							function addLocationPin(latitude, longitude)
							{
								var location = new Microsoft.Maps.Location(latitude, longitude);
								locationPin = new Microsoft.Maps.Pushpin(location, null);
								map.entities.push(locationPin);
								map.setView({
									center: location
								});
							}
					
							function removeLocationPin()
							{
								if (locationPin != null)
								{
									map.entities.remove(locationPin);
									locationPin = null;
								}
							}

							function removeAllPins()
							{
								map.entities.clear();
								if (locationPin != null )
								{
									map.entities.push(locationPin);
									map.setView({
										center: locationPin.getLocation()
									});
								}
							}

							function addPin(latitude, longitude, label, address, id)
							{
								var location = new Microsoft.Maps.Location(latitude, longitude);
								var pin = new Microsoft.Maps.Pushpin(location, {
								            title: label,
											subTitle: address
								        });

								pin.markerId = id;
								map.entities.push(pin);
								Microsoft.Maps.Events.addHandler(pin, 'click', function (e) 
								{
									if (e.targetType !== 'pushpin')
										return;

									//Set the infobox options with the metadata of the pushpin.
									infobox.setOptions({
										location: e.target.getLocation(),
										title: e.target.getTitle(),
										description: encodeHtml(e.target.getSubTitle()),
										visible: true
									});

									infobox.infoWindowMarkerId = id;

									var clickedPin = {
										label: e.target.getTitle(),
										address: e.target.getSubTitle(),
										location: e.target.getLocation(),
										markerId: e.target.markerId
									};
									invokeHandlerAction('PinClicked', clickedPin);
								});
							}

							// The infobox shows its title as text, but renders its description as HTML. The address of a pin is text: its markup characters
							// are encoded for the description, so that it shows as it is and no markup in it can run script in this page.
							function encodeHtml(text)
							{
								return String(text === undefined || text === null ? '' : text)
									.replace(/&/g, '&amp;')
									.replace(/</g, '&lt;')
									.replace(/>/g, '&gt;')
									.replace(/""/g, '&quot;')
									.replace(/'/g, '&#39;');
							}

							function invokeHandlerAction(id, data)
							{
								var eventMessage = {
									id: id,
									payload: data
								};

								window.chrome.webview.postMessage(eventMessage);
							}

							function hideInfoWindow()
							{
								infobox.setOptions({
									visible: false
								});
							}
						</script>
						<style>
							body, html{
								padding:0;
								margin:0;
							}
						</style>
					</head>
					<body onload='loadMap();'>
						<div id=""myMap""></div>
					</body>
				</html>";
		return str;
	}

	static async Task<Location?> GetCurrentLocation()
	{
		if (await Geolocator.RequestAccessAsync() != GeolocationAccessStatus.Allowed)
		{
			return null;
		}

		try
		{
			var geoLocator = new Geolocator();
			var position = await geoLocator.GetGeopositionAsync();
			return new Location(position.Coordinate.Latitude, position.Coordinate.Longitude);
		}
		catch
		{
			return null;
		}
	}

	static void LoadMap(FrameworkElement platformView)
	{
		if (platformView is MauiWebView mauiWebView && Connectivity.NetworkAccess == NetworkAccess.Internet)
		{
			mauiWebView.LoadHtml(MapPage, null);
		}
	}

	async void HandleWebViewNavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
	{
		// Update initial properties when our page is loaded
		Mapper.UpdateProperties(this, VirtualView);

		if (regionToGo is not null)
		{
			await MapMoveToRegion(this, VirtualView, regionToGo);
		}
	}

	async void WebViewWebMessageReceived(WebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
	{
		// For some reason the web message is empty
		if (string.IsNullOrEmpty(args.WebMessageAsJson))
		{
			return;
		}

		var eventMessage = DeserializeOrDefault(args.WebMessageAsJson, SerializerContext.Default.EventMessage);

		// The web message (or it's ID) could not be deserialized to something we recognize
		if (eventMessage is null || !Enum.TryParse<EventIdentifier>(eventMessage.Id, true, out var eventId))
		{
			return;
		}

		var payloadAsString = eventMessage.Payload?.ToString();

		// The web message does not have a payload
		if (string.IsNullOrWhiteSpace(payloadAsString))
		{
			return;
		}

		switch (eventId)
		{
			case EventIdentifier.BoundsChanged:
				var mapRect = DeserializeOrDefault(payloadAsString, SerializerContext.Default.Bounds);
				if (mapRect?.Center is not null)
				{
					VirtualView.VisibleRegion = new MapSpan(new Location(mapRect.Center.Latitude, mapRect.Center.Longitude),
						mapRect.Height, mapRect.Width);
				}
				break;
			case EventIdentifier.MapClicked:
				var clickedLocation = DeserializeOrDefault(payloadAsString, SerializerContext.Default.Location);
				if (clickedLocation is not null)
				{
					VirtualView.Clicked(clickedLocation);
				}
				break;

			case EventIdentifier.InfoWindowClicked:
				var clickedInfoWindowWebView = DeserializeOrDefault(payloadAsString, SerializerContext.Default.InfoWindow);
				var clickedInfoWindowWebViewId = clickedInfoWindowWebView?.InfoWindowMarkerId;

				if (!string.IsNullOrEmpty(clickedInfoWindowWebViewId))
				{
					// The same Pin can be in Map.Pins more than once
					var clickedPin = VirtualView.Pins.FirstOrDefault(p => (p as Pin)?.Id.ToString().Equals(clickedInfoWindowWebViewId) ?? false);

					var hideInfoWindow = clickedPin?.SendInfoWindowClick();
					if (hideInfoWindow is not false)
					{
						await TryCallJSMethod(PlatformView, "hideInfoWindow();");
					}
				}
				break;

			case EventIdentifier.PinClicked:
				var clickedPinWebView = DeserializeOrDefault(payloadAsString, SerializerContext.Default.ClickedPin);
				var clickedPinWebViewId = clickedPinWebView?.MarkerId?.ToString();

				if (!string.IsNullOrEmpty(clickedPinWebViewId))
				{
					// The same Pin can be in Map.Pins more than once
					var clickedPin = VirtualView.Pins.FirstOrDefault(p => (p as Pin)?.Id.ToString().Equals(clickedPinWebViewId) ?? false);

					var hideInfoWindow = clickedPin?.SendMarkerClick();
					if (hideInfoWindow is not false)
					{
						await TryCallJSMethod(PlatformView, "hideInfoWindow();");
					}
				}
				break;
		}
	}

	// Valid JSON of another shape than the page posts (a message that is no object, a payload the type cannot hold) is ignored like an
	// unknown message: JsonSerializer throws for it, and nothing may escape the async void WebMessageReceived handler.
	static T? DeserializeOrDefault<T>(string json, JsonTypeInfo<T> jsonTypeInfo) where T : class
	{
		try
		{
			return JsonSerializer.Deserialize(json, jsonTypeInfo);
		}
		catch (JsonException)
		{
			return null;
		}
	}

	void Connectivity_ConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
	{
		LoadMap(PlatformView);
	}
}