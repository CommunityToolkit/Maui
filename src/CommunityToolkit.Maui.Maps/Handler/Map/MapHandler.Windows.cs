﻿using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
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
[RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
[RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
public partial class MapHandlerWindows : MapHandler
{
	MapSpan? regionToGo;

	readonly JsonSerializerOptions jsonSerializerOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

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

	/// <inheritdoc/>
	[RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
	[RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
#pragma warning disable IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.
#pragma warning disable IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
	protected override FrameworkElement CreatePlatformView()
#pragma warning restore IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
#pragma warning restore IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.
	{
		if (string.IsNullOrEmpty(MapsKey))
		{
			throw new InvalidOperationException("You need to specify a Bing Maps Key");
		}

		var mapPage = GetMapHtmlPage(MapsKey);
		var webView = new MauiWebView(new WebViewHandler());
		webView.NavigationCompleted += HandleWebViewNavigationCompleted;
		webView.WebMessageReceived += WebViewWebMessageReceived;
		webView.LoadHtml(mapPage, null);
		return webView;
	}

	/// <inheritdoc />
	[RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
	[RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
#pragma warning disable IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.
#pragma warning disable IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
	protected override void DisconnectHandler(FrameworkElement platformView)
#pragma warning restore IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
#pragma warning restore IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.
	{
		if (PlatformView is MauiWebView mauiWebView)
		{
			mauiWebView.NavigationCompleted -= HandleWebViewNavigationCompleted;
			mauiWebView.WebMessageReceived -= WebViewWebMessageReceived;
		}

		base.DisconnectHandler(platformView);
	}

	/// <summary>
	/// Maps Map type
	/// </summary>
	public static new Task MapMapType(IMapHandler handler, IMap map)
	{
		return CallJSMethod(handler.PlatformView, $"setMapType('{map.MapType}');");
	}

	/// <summary>
	/// Maps IsZoomEnabled
	/// </summary>
	public static new Task MapIsZoomEnabled(IMapHandler handler, IMap map)
	{
		return CallJSMethod(handler.PlatformView, $"disableMapZoom({(!map.IsZoomEnabled).ToString().ToLower()});");
	}

	/// <summary>
	/// Maps IsScrollEnabled
	/// </summary>
	public static new Task MapIsScrollEnabled(IMapHandler handler, IMap map)
	{
		return CallJSMethod(handler.PlatformView, $"disablePanning({(!map.IsScrollEnabled).ToString().ToLower()});");
	}

	/// <summary>
	/// Maps IsTrafficEnabled
	/// </summary>
	public static new Task MapIsTrafficEnabled(IMapHandler handler, IMap map)
	{
		return CallJSMethod(handler.PlatformView, $"disableTraffic({(!map.IsTrafficEnabled).ToString().ToLower()});");
	}

	/// <summary>
	/// Maps IsShowingUser
	/// </summary>
	public static new async Task MapIsShowingUser(IMapHandler handler, IMap map)
	{
		if (map.IsShowingUser)
		{
			var location = await GetCurrentLocation();
			if (location != null)
			{
				await CallJSMethod(handler.PlatformView, $"addLocationPin({location.Latitude.ToString(CultureInfo.InvariantCulture)},{location.Longitude.ToString(CultureInfo.InvariantCulture)});");
			}
		}
		else
		{
			await CallJSMethod(handler.PlatformView, "removeLocationPin();");
		}
	}

	/// <summary>
	/// Map Pins
	/// </summary>
	public static new async Task MapPins(IMapHandler handler, IMap map)
	{
		await CallJSMethod(handler.PlatformView, "removeAllPins();");

		var addPinTaskList = new List<Task>();

		foreach (var pin in map.Pins)
		{
			addPinTaskList.Add(CallJSMethod(handler.PlatformView, $"addPin({pin.Location.Latitude.ToString(CultureInfo.InvariantCulture)}," +
				$"{pin.Location.Longitude.ToString(CultureInfo.InvariantCulture)},'{pin.Label}', '{pin.Address}', '{(pin as Pin)?.Id}');"));
		}

		await Task.WhenAll(addPinTaskList);
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

		await CallJSMethod(handler.PlatformView, $"setRegion({newRegion.Center.Latitude.ToString(CultureInfo.InvariantCulture)},{newRegion.Center.Longitude.ToString(CultureInfo.InvariantCulture)},{newRegion.LatitudeDegrees.ToString(CultureInfo.InvariantCulture)},{newRegion.LongitudeDegrees.ToString(CultureInfo.InvariantCulture)});");
	}

	static async Task CallJSMethod(FrameworkElement platformWebView, string script)
	{
		if (platformWebView is WebView2 webView2)
		{
			var tcs = new TaskCompletionSource();
			webView2.DispatcherQueue.TryEnqueue(async () =>
			{
				await webView2.ExecuteScriptAsync(script);
				tcs.SetResult();
			});

			await tcs.Task;
		}
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
								});
							}

							function disableTraffic(disable)
							{
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
								map.setView({bounds: new Microsoft.Maps.LocationRect(new Microsoft.Maps.Location(latitude, longitude), latitudeDegrees, longitudeDegrees) });
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
										center: location
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
										description: e.target.getSubTitle(),
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
		var geoLocator = new Geolocator();
		var position = await geoLocator.GetGeopositionAsync();
		return new Location(position.Coordinate.Latitude, position.Coordinate.Longitude);
	}

	async void HandleWebViewNavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
	{
		// Update initial properties when our page is loaded
		Mapper.UpdateProperties(this, VirtualView);

		if (regionToGo != null)
		{
			await MapMoveToRegion(this, VirtualView, regionToGo);
		}
	}

	[RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
	[RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
	async void WebViewWebMessageReceived(WebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
	{
		// For some reason the web message is empty
		if (string.IsNullOrEmpty(args.WebMessageAsJson))
		{
			return;
		}

		var eventMessage = JsonSerializer.Deserialize<EventMessage>(args.WebMessageAsJson, jsonSerializerOptions);

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
				var mapRect = JsonSerializer.Deserialize<Bounds>(payloadAsString, jsonSerializerOptions);
				if (mapRect?.Center is not null)
				{
					VirtualView.VisibleRegion = new MapSpan(new Location(mapRect.Center.Latitude, mapRect.Center.Longitude),
						mapRect.Height, mapRect.Width);
				}
				break;
			case EventIdentifier.MapClicked:
				var clickedLocation = JsonSerializer.Deserialize<Location>(payloadAsString,
					jsonSerializerOptions);
				if (clickedLocation is not null)
				{
					VirtualView.Clicked(clickedLocation);
				}
				break;

			case EventIdentifier.InfoWindowClicked:
				var clickedInfoWindowWebView = JsonSerializer.Deserialize<InfoWindow>(payloadAsString,
					jsonSerializerOptions);
				var clickedInfoWindowWebViewId = clickedInfoWindowWebView?.InfoWindowMarkerId;

				if (!string.IsNullOrEmpty(clickedInfoWindowWebViewId))
				{
					var clickedPin = VirtualView.Pins.SingleOrDefault(p => (p as Pin)?.Id.ToString().Equals(clickedInfoWindowWebViewId) ?? false);

					var hideInfoWindow = clickedPin?.SendInfoWindowClick();
					if (hideInfoWindow is not false)
					{
						await CallJSMethod(PlatformView, "hideInfoWindow();");
					}
				}
				break;

			case EventIdentifier.PinClicked:
				var clickedPinWebView = JsonSerializer.Deserialize<Pin>(payloadAsString, jsonSerializerOptions);
				var clickedPinWebViewId = clickedPinWebView?.MarkerId?.ToString();

				if (!string.IsNullOrEmpty(clickedPinWebViewId))
				{
					var clickedPin = VirtualView.Pins.SingleOrDefault(p => (p as Pin)?.Id.ToString().Equals(clickedPinWebViewId) ?? false);

					var hideInfoWindow = clickedPin?.SendMarkerClick();
					if (hideInfoWindow is not false)
					{
						await CallJSMethod(PlatformView, "hideInfoWindow();");
					}
				}
				break;
		}
	}
}