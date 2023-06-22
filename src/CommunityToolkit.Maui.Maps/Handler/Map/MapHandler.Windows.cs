using System.Text.Json;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Maps.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Devices.Geolocation;
using IMap = Microsoft.Maui.Maps.IMap;

namespace CommunityToolkit.Maui.Maps.Handlers;

/// <inheritdoc />
public partial class MapHandlerWindows : MapHandler
{
	internal static string? MapsKey;

	/// <summary>
	/// Initializes a new instance of the <see cref="MapHandlerWindows"/> class.
	/// </summary>
	public MapHandlerWindows() : base(Mapper, CommandMapper)
	{
		Mapper.ModifyMapping(nameof(IMap.MapType), (handler, map, _) => MapMapType(handler, map));
		Mapper.ModifyMapping(nameof(IMap.IsShowingUser), (handler, map, _) => MapIsShowingUser(handler, map));
		Mapper.ModifyMapping(nameof(IMap.IsScrollEnabled), (handler, map, _) => MapIsScrollEnabled(handler, map));
		Mapper.ModifyMapping(nameof(IMap.IsTrafficEnabled), (handler, map, _) => MapIsTrafficEnabled(handler, map));
		Mapper.ModifyMapping(nameof(IMap.IsZoomEnabled), (handler, map, _) => MapIsZoomEnabled(handler, map));
		Mapper.ModifyMapping(nameof(IMap.Pins), (handler, map, _) => MapPins(handler, map));
		Mapper.ModifyMapping(nameof(IMap.Elements), (handler, map, _) => MapElements(handler, map));
		CommandMapper.ModifyMapping(nameof(IMap.MoveToRegion), (handler, map, args, _) => MapMoveToRegion(handler, map, args));
	}

	/// <inheritdoc/>

	protected override FrameworkElement CreatePlatformView()
	{
		if (string.IsNullOrEmpty(MapsKey))
		{
			throw new InvalidOperationException("You need to specify a Bing Maps Key");
		}

		var mapPage = GetMapHtmlPage(MapsKey);
		var webView = new MauiWebView();
		webView.NavigationCompleted += WebViewNavigationCompleted;
		webView.WebMessageReceived += WebViewWebMessageReceived;
		webView.LoadHtml(mapPage, null);
		return webView;
	}

	/// <inheritdoc />
	protected override void DisconnectHandler(FrameworkElement platformView)
	{
		if (PlatformView is MauiWebView mauiWebView)
		{
			mauiWebView.NavigationCompleted -= WebViewNavigationCompleted;
			mauiWebView.WebMessageReceived -= WebViewWebMessageReceived;
		}
		base.DisconnectHandler(platformView);
	}

	/// <summary>
	/// Maps Map type
	/// </summary>
	public static new void MapMapType(IMapHandler handler, IMap map)
	{
		CallJSMethod(handler.PlatformView, $"setMapType('{map.MapType}');");
	}

	/// <summary>
	/// Maps IsZoomEnabled
	/// </summary>
	public static new void MapIsZoomEnabled(IMapHandler handler, IMap map)
	{
		CallJSMethod(handler.PlatformView, $"disableMapZoom({(!map.IsZoomEnabled).ToString().ToLower()});");
	}

	/// <summary>
	/// Maps IsScrollEnabled
	/// </summary>
	public static new void MapIsScrollEnabled(IMapHandler handler, IMap map)
	{
		CallJSMethod(handler.PlatformView, $"disablePanning({(!map.IsScrollEnabled).ToString().ToLower()});");
	}

	/// <summary>
	/// Maps IsTrafficEnabled
	/// </summary>
	public static new void MapIsTrafficEnabled(IMapHandler handler, IMap map)
	{
		CallJSMethod(handler.PlatformView, $"disableTraffic({(!map.IsTrafficEnabled).ToString().ToLower()});");
	}

	/// <summary>
	/// Maps IsShowingUser
	/// </summary>
	public static new async void MapIsShowingUser(IMapHandler handler, IMap map)
	{
		if (map.IsShowingUser)
		{
			var location = await GetCurrentLocation();
			if (location != null)
			{
				CallJSMethod(handler.PlatformView, $"addLocationPin({location.Latitude},{location.Longitude});");
			}
		}
		else
		{
			CallJSMethod(handler.PlatformView, "removeLocationPin();");
		}
	}

	/// <summary>
	/// Map Pins
	/// </summary>
	public static new void MapPins(IMapHandler handler, IMap map)
	{
		CallJSMethod(handler.PlatformView, "removeAllPins();");
		foreach (var pin in map.Pins)
		{
			CallJSMethod(handler.PlatformView, $"addPin({pin.Location.Latitude},{pin.Location.Longitude},'{pin.Label}', '{pin.Address}');");
		}
	}

	/// <summary>
	/// Map Elements
	/// </summary>
	public static new void MapElements(IMapHandler handler, IMap map) { }

	MapSpan? regionToGo;
	/// <summary>
	/// Maps MoveToRegion
	/// </summary>
	public static new void MapMoveToRegion(IMapHandler handler, IMap map, object? arg)
	{
		var newRegion = arg as MapSpan;
		if (newRegion == null)
		{
			return;
		}

		if (handler is MapHandlerWindows mapHandler)
		{
			mapHandler.regionToGo = newRegion;
		}
		CallJSMethod(handler.PlatformView, $"setRegion({newRegion.Center.Latitude},{newRegion.Center.Longitude});");
	}

	static void CallJSMethod(FrameworkElement platformWebView, string script)
	{
		if (platformWebView is WebView2 webView2 && webView2.CoreWebView2 != null)
		{
			platformWebView.DispatcherQueue.TryEnqueue(async () => await webView2.ExecuteScriptAsync(script));
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
		str += @"	<script type='text/javascript'>
			                var map;
							var locationPin;
							var trafficManager; 
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
								Microsoft.Maps.Events.addHandler(map, 'viewrendered', function () { var bounds = map.getBounds(); invokeHandlerAction(bounds); });
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

							function setRegion(latitude, longitude)
							{
								map.setView({
									center: new Microsoft.Maps.Location(latitude, longitude),
								});
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
								if(locationPin != null)
								{
									map.entities.remove(locationPin);
									locationPin = null;
								}
							}

							function removeAllPins()
							{
								map.entities.clear();
								locationPin = null;
							}

							function addPin(latitude, longitude, label, address)
							{
								var location = new Microsoft.Maps.Location(latitude, longitude);
								var pin = new Microsoft.Maps.Pushpin(location, {
								            title: label,
											subTitle: address
								        });
								map.entities.push(pin);
								Microsoft.Maps.Events.addHandler(pin, 'click', function (e) 
								{
									var clickedPin = {
										label: e.target.getTitle(),
										address: e.target.getSubTitle(),
										location : e.target.getLocation()
									};
									invokeHandlerAction(clickedPin);
								});
							}

							function invokeHandlerAction(data)
							{
								window.chrome.webview.postMessage(data);
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

	void WebViewNavigationCompleted(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args)
	{
		// Update initial properties when our page is loaded
		Mapper.UpdateProperties(this, VirtualView);

		if (regionToGo != null)
		{
			MapMoveToRegion(this, VirtualView, regionToGo);
		}
	}

	void WebViewWebMessageReceived(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs args)
	{
		var options = new JsonSerializerOptions()
		{
			PropertyNameCaseInsensitive = true
		};

		var clickedPin = JsonSerializer.Deserialize<Pin>(args.WebMessageAsJson, options);
		if (clickedPin?.Location != null)
		{
			clickedPin.SendMarkerClick();
			clickedPin.SendInfoWindowClick();
			return;
		}

		var mapRect = JsonSerializer.Deserialize<Bounds>(args.WebMessageAsJson, options);
		if (mapRect?.Center != null)
		{
			VirtualView.VisibleRegion = new MapSpan(new Location(mapRect.Center?.Latitude ?? 0, mapRect.Center?.Longitude ?? 0), mapRect.Height, mapRect.Width);
		}
	}

	class Center
	{
		public double Latitude { get; set; }
		public double Longitude { get; set; }
		public int Altitude { get; set; }
		public int AltitudeReference { get; set; }
	}

	class Bounds
	{
		public Center? Center { get; set; }
		public double Width { get; set; }
		public double Height { get; set; }
	}
}