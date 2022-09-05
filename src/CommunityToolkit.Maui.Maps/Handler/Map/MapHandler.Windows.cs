using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Maps.Handlers;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml.Controls;
using IMap = Microsoft.Maui.Maps.IMap;
using Microsoft.UI.Xaml;

namespace CommunityToolkit.Maui.Maps.Handlers;

public partial class MapHandlerWindows : MapHandler
{
	internal static string? mapsKey;

	public MapHandlerWindows() : base(Mapper, CommandMapper)
	{
		Mapper.ModifyMapping(nameof(IMap.MapType), (handler, map, action) => MapMapType(handler, map));
		Mapper.ModifyMapping(nameof(IMap.IsShowingUser), (handler, map, action) => MapIsShowingUser(handler, map));
		Mapper.ModifyMapping(nameof(IMap.IsScrollEnabled), (handler, map, action) => MapIsScrollEnabled(handler, map));
		Mapper.ModifyMapping(nameof(IMap.IsTrafficEnabled), (handler, map, action) => MapIsTrafficEnabled(handler, map));
		Mapper.ModifyMapping(nameof(IMap.IsZoomEnabled), (handler, map, action) => MapIsZoomEnabled(handler, map));
		Mapper.ModifyMapping(nameof(IMap.Pins), (handler, map, action) => MapPins(handler, map));
		Mapper.ModifyMapping(nameof(IMap.Elements), (handler, map, action) => MapElements(handler, map));
		CommandMapper.ModifyMapping(nameof(IMap.MoveToRegion), (handler, map, args, action) => MapMoveToRegion(handler, map, args));
	}

	/// <inheritdoc/>

	protected override FrameworkElement CreatePlatformView()
	{
		if (string.IsNullOrEmpty(mapsKey))
		{
			throw new InvalidOperationException("You need to specify a Bing Maps Key");
		}

		var mapPage = GetMapHtmlPage(mapsKey);
		var webView = new MauiWebView();
		webView.LoadHtml(mapPage, null);
		return webView;
	}


	public static void MapMapType(IMapHandler handler, IMap map)
	{
		CallJSMethod(handler.PlatformView, $"setMapType('{map.MapType}');");
	}

	public static void MapIsZoomEnabled(IMapHandler handler, IMap map)
	{
		CallJSMethod(handler.PlatformView, $"disableMapZoom({(!map.IsZoomEnabled).ToString().ToLower()});");
	}

	public static void MapIsScrollEnabled(IMapHandler handler, IMap map)
	{
		CallJSMethod(handler.PlatformView, $"disablePanning({(!map.IsScrollEnabled).ToString().ToLower()});");
	}

	public static void MapIsTrafficEnabled(IMapHandler handler, IMap map)
	{
		CallJSMethod(handler.PlatformView, $"disableTraffic({(!map.IsTrafficEnabled).ToString().ToLower()});");
	}

	public static void MapIsShowingUser(IMapHandler handler, IMap map) { }

	public static void MapPins(IMapHandler handler, IMap map) { }

	public static void MapElements(IMapHandler handler, IMap map) { }

	public static void MapMoveToRegion(IMapHandler handler, IMap map, object? arg)
	{
		MapSpan? newRegion = arg as MapSpan;
		if (newRegion != null)
		{
			CallJSMethod(handler.PlatformView, $"setRegion({newRegion.Center.Latitude},{newRegion.Center.Longitude});");
		}
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

	public void UpdateMapElement(IMapElement element)
	{
		throw new NotImplementedException();
	}
}
