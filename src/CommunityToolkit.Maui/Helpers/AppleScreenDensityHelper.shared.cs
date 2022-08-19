namespace CommunityToolkit.Maui.Helpers;

/// <summary>
///     Helper class for retrieving PIxel per Inches (PPI) for apple devices so that the screen diagonal size in inches can
///     be calculated later on.
/// </summary>
/// <remarks>
///     Because Apple does not provide any API for dynamically retrieving Pixel per Pnches (PPI) for all its devices (as
///     Android does),
///     we need to keep it hardcoded inside this class the PPI info based on device models,
///     device names, and screen sizes. So that we can fallback to each one of the info (device name, model and screen
///     size)
///     to get PPI.ided apple-device-model.
///     Probably we will need to update this list as new iphones and apple devices are launched having different PPI.
/// </remarks>
public static class AppleScreenDensityHelper
{
	/// <summary>
	///     Attempts to retrieve the pixel-per-inches (PPI) for the provided apple-device-model
	/// </summary>
	/// <param name="appleDeviceModel">Apple device model. for instance: "iPhone7,2", "iPhone8,4", and etc</param>
	/// <param name="ppi">the amount of pixel per inches a device has.</param>
	/// <returns></returns>
	static bool TryGetPpiByDeviceModel(string appleDeviceModel, out double ppi)
	{
		ppi = 0;
		if (string.IsNullOrWhiteSpace(appleDeviceModel) || IsUnknownIosSimulator(appleDeviceModel))
		{
			return false;
		}

		var match = machineNamesToPpi.FirstOrDefault(f => f.Models.Any(m => m == appleDeviceModel));
		if (match.PixelPerInches.Equals(default))
		{
			return false;
		}

		ppi = match.PixelPerInches;
		return true;
	}

	static bool IsUnknownIosSimulator(string appleDeviceModel)
	{
		if (
			appleDeviceModel.Equals("x86_64", StringComparison.OrdinalIgnoreCase) ||
			appleDeviceModel.Equals("arm64", StringComparison.OrdinalIgnoreCase) ||
			appleDeviceModel.Equals("i386", StringComparison.OrdinalIgnoreCase)
		)
		{
			return true;
		}

		return false;
	}

	/// <summary>
	///     Attempts to retrieve the pixel-per-inches for the provided apple-device-name
	/// </summary>
	/// <param name="appleDeviceName"></param>
	/// <param name="ppi"></param>
	/// <returns></returns>
	static bool TryGetPpiByDeviceName(string appleDeviceName, out double ppi)
	{
		ppi = 0;
		if (!deviceNamesToPpi.TryGetValue(appleDeviceName, out ppi))
		{
			return false;
		}

		return true;
	}


	/// <summary>
	///     Attempts to return pixel-per-inches for the provided screen size.
	/// </summary>
	/// <param name="screenDimensions"></param>
	/// <param name="ppi"></param>
	/// <returns></returns>
	static bool TryGetPpiByScreenDimensions((double Width, double Height) screenDimensions, out double ppi)
	{
		if (!apppleDevicesDimensionsToPpi.TryGetValue(screenDimensions, out ppi))
		{
			return false;
		}

		return true;
	}

	/// <summary>
	///     Attempt to get how many pixel-per-inches a device has by using
	///     the provided <paramref name="appleDeviceModel" /> and in case it does not finds it
	/// </summary>
	/// <param name="appleDeviceModel">Apple device model such as "iPhone14,2" or "iPhone10,4"</param>
	/// <param name="appleDeviceName"></param>
	/// <param name="screenDimensions"></param>
	/// <param name="ppi"></param>
	/// <returns></returns>
	public static bool TryGetPpiWithFallBacks(string appleDeviceModel, string appleDeviceName,
		(double Width, double Height) screenDimensions,
		out double ppi)
	{
		//Attempts to get by the device model, such as "iPhone14,2", "iPhone10,4", and etc.
		if (TryGetPpiByDeviceModel(appleDeviceModel, out ppi))
		{
			return true;
		}

		//Attempts to get by the device name, such as "iPhone XR", "iPhone 10 Pro Max", and etc.
		if (TryGetPpiByDeviceName(appleDeviceName, out ppi))
		{
			return true;
		}

		if (TryGetPpiByScreenDimensions(screenDimensions, out ppi))
		{
			return true;
		}

		if (DeviceInfo.Current.Idiom == DeviceIdiom.Phone)
		{
			ppi = 458;
			return true;
		}

		if (DeviceInfo.Current.Idiom == DeviceIdiom.Tablet)
		{
			ppi = 264;
			return true;
		}

		return false;
	}

	#region hardcoded PPI related values

	static Dictionary<string, double> deviceNamesToPpi = new(StringComparer.InvariantCultureIgnoreCase)
	{
		{ "iPhone 3GS", 163 }, //	iPhone 3GS	
		{ "iPhone 3G", 163 }, //	iPhone 3G	
		{ "iPhone 1st gen", 163 }, //	iPhone 1st gen	
		{ "iPod touch 3rd gen", 163 }, //	iPod touch 3rd gen	
		{ "iPod touch 2nd gen", 163 }, //	iPod touch 2nd gen	
		{ "iPod touch 1st gen", 163 }, //	iPod touch 1st gen	
		{ "iPhone SE 1st gen", 326 }, //	iPhone SE 1st gen	
		{ "iPhone 5C", 326 }, //	iPhone 5C	
		{ "iPhone 5S", 326 }, //	iPhone 5S	
		{ "iPhone 5", 326 }, //	iPhone 5	
		{ "iPhone 4S", 326 }, //	iPhone 4S	
		{ "iPhone 4", 326 }, //	iPhone 4	
		{ "iPod touch 7th gen", 326 }, //	iPod touch 7th gen	
		{ "iPod touch 6th gen", 326 }, //	iPod touch 6th gen	
		{ "iPod touch 5th gen", 326 }, //	iPod touch 5th gen	
		{ "iPod touch 4th gen", 326 }, //	iPod touch 4th gen	
		{ "iPhone SE 2nd gen", 326 }, //	iPhone SE 2nd gen	
		{ "iPhone 8", 326 }, //	iPhone 8	
		{ "iPhone 7", 326 }, //	iPhone 7	
		{ "iPhone 6s", 326 }, //	iPhone 6s	
		{ "iPhone 6", 326 }, //	iPhone 6	
		{ "iPad mini", 163 }, //	iPad mini	
		{ "iPad 2", 132 }, //	iPad 2	
		{ "iPad 1st gen", 132 }, //	iPad 1st gen	
		{ "iPhone 11", 326 }, //	iPhone 11	
		{ "iPhone XR", 326 }, //	iPhone XR	
		{ "iPhone 13 mini", 476 }, //	iPhone 13 mini	
		{ "iPhone 12 mini", 476 }, //	iPhone 12 mini	
		{ "iPhone 8 Plus", 401 }, //	iPhone 8 Plus	
		{ "iPhone 11 Pro", 458 }, //	iPhone 11 Pro	
		{ "iPhone XS", 458 }, //	iPhone XS	
		{ "iPhone X", 458 }, //	iPhone X	
		{ "iPhone 13", 460 }, //	iPhone 13	
		{ "iPhone 13 Pro", 460 }, //	iPhone 13 Pro	
		{ "iPhone 12", 460 }, //	iPhone 12	
		{ "iPhone 12 Pro", 460 }, //	iPhone 12 Pro	
		{ "iPhone 11 Pro Max", 458 }, //	iPhone 11 Pro Max	
		{ "iPhone XS Max", 458 }, //	iPhone XS Max	
		{ "iPhone 7 Plus", 401 }, //	iPhone 7 Plus	
		{ "iPhone 6s Plus", 401 }, //	iPhone 6s Plus	
		{ "iPhone 6 Plus", 401 }, //	iPhone 6 Plus	
		{ "iPhone 13 Pro Max", 458 }, //	iPhone 13 Pro Max	
		{ "iPhone 12 Pro Max", 458 }, //	iPhone 12 Pro Max	
		{ "iPad Mini (6th gen)", 326 }, //	iPad Mini (6th gen)	
		{ "iPad Mini (5th gen)", 326 }, //	iPad Mini (5th gen)	
		{ "iPad 6th gen", 264 }, //	iPad 6th gen	
		{ "iPad 5th gen", 264 }, //	iPad 5th gen	
		{ "iPad Pro (1st gen 9.7\")", 264 }, //	iPad Pro (1st gen 9.7”)	
		{ "iPad mini 4", 326 }, //	iPad mini 4	
		{ "iPad Air 2", 326 }, //	iPad Air 2	
		{ "iPad mini 3", 264 }, //	iPad mini 3	
		{ "iPad mini 2", 326 }, //	iPad mini 2	
		{ "iPad Air", 264 }, //	iPad Air	
		{ "iPad 4th gen", 264 }, //	iPad 4th gen	
		{ "iPad 3rd gen", 264 }, //	iPad 3rd gen	
		{ "iPad 9th gen", 264 }, //	iPad 9th gen	
		{ "iPad 8th gen", 264 }, //	iPad 8th gen	
		{ "iPad 7th gen", 264 }, //	iPad 7th gen	
		{ "iPad Air (4th gen)", 264 }, //	iPad Air (4th gen)	
		{ "iPad Pro (5th gen 11\")", 264 }, //	iPad Pro (5th gen 11)	
		{ "iPad Pro (4th gen 11\")", 264 }, //	iPad Pro (4th gen 11)	
		{ "iPad Air (3rd gen)", 264 }, //	iPad Air (3rd gen)	
		{ "iPad Pro (3rd gen 11\")", 264 }, //	iPad Pro (3rd gen 11)	
		{ "iPad Pro (2nd gen 10.5\")", 264 }, //	iPad Pro (2nd gen 10.5)	
		{ "iPad Pro (5th gen 12.9\")", 264 }, //	iPad Pro (5th gen 12.9)	
		{ "iPad Pro (4th gen 12.9\")", 264 }, //	iPad Pro (4th gen 12.9)	
		{ "iPad Pro (3rd gen 12.9\")", 264 }, //	iPad Pro (3rd gen 12.9)	
		{ "iPad Pro (2nd gen 12.9\")", 264 }, //	iPad Pro (2nd gen 12.9)	
		{ "iPad Pro (1st gen 12.9\")", 264 } //	iPad Pro (1st gen 12.9)	
	};

	static Dictionary<(double Width, double Height), double> apppleDevicesDimensionsToPpi = new()
	{
		{ new ValueTuple<double, double>(320, 480), 163 }, //	iPhone 3GS
		{ new ValueTuple<double, double>(320, 568), 326 }, //	iPhone SE 1st gen
		{ new ValueTuple<double, double>(375, 667), 326 }, //	iPhone 8, iPhone 6s
		{ new ValueTuple<double, double>(375, 812), 476 }, //	iPhone 12 mini
		{ new ValueTuple<double, double>(414, 736), 401 }, //	iPhone 8 Plus
		{ new ValueTuple<double, double>(390, 844), 460 }, //	iPhone 12 Pro
		{ new ValueTuple<double, double>(414, 896), 458 }, //	iPhone XS Max
		{ new ValueTuple<double, double>(476, 847), 401 }, //	iPhone 6 Plus
		{ new ValueTuple<double, double>(428, 926), 458 }, //	iPhone 12 Pro Max
		{ new ValueTuple<double, double>(744, 1133), 326 }, //	iPad Mini (6th gen)
		{ new ValueTuple<double, double>(768, 1024), 326 }, //	iPad Mini (5th gen)
		{ new ValueTuple<double, double>(810, 1080), 264 }, //	iPad 9th gen
		{ new ValueTuple<double, double>(834, 1194), 264 }, //	iPad Pro (5th gen 11")
		{ new ValueTuple<double, double>(834, 1112), 264 }, //	iPad Pro (2nd gen 10.5")
		{ new ValueTuple<double, double>(1024, 1366), 264 } //	iPad Pro (1st gen 12.9")
	};

	static readonly (string[] Models, int PixelPerInches)[] machineNamesToPpi =
	{
		new(new[] { "iPhone14,4", "iPhone13,1" }, 476),
		new(new[] { "iPhone14,5", "iPhone14,2", "iPhone13,2", "iPhone13,3" }, 460),
		new(
			new[]
			{
				"iPhone14,3", "iPhone13,4", "iPhone12,3", "iPhone12,5", "iPhone11,2", "iPhone11,4", "iPhone11,6",
				"iPhone10,3", "iPhone10,6"
			},
			458
		),
		new(new[] { "iPhone10,2", "iPhone10,5", "iPhone9,2", "iPhone9,4", "iPhone8,2", "iPhone7,1" }, 401),
		new(
			new[]
			{
				"iPhone12,1", "iPhone11,8", "iPhone14,6", "iPhone12,8", "iPhone10,1", "iPhone10,4", "iPhone9,1",
				"iPhone9,3", "iPhone8,1", "iPhone7,2", "iPhone8,4", "iPhone6,1", "iPhone6,2", "iPhone5,3",
				"iPhone5,4",
				"iPhone5,1", "iPhone5,2", "iPod9,1", "iPod7,1", "iPod5,1", "iPhone4,1", "iPad14,1", "iPad14,2",
				"iPad11,1", "iPad11,2", "iPad5,1", "iPad5,2", "iPad4,7", "iPad4,8", "iPad4,9", "iPad4,4", "iPad4,5",
				"iPad4,6"
			},
			326
		),
		new(
			new[]
			{
				"iPad13,16", "iPad13,17", "iPad12,1", "iPad12,2", "iPad13,8", "iPad13,9", "iPad13,10", "iPad13,11",
				"iPad13,4", "iPad13,5", "iPad13,6", "iPad13,7", "iPad13,1", "iPad13,2", "iPad11,6", "iPad11,7",
				"iPad8,11", "iPad8,12", "iPad8,9", "iPad8,10", "iPad7,11", "iPad7,12", "iPad11,3", "iPad11,4",
				"iPad8,5", "iPad8,6", "iPad8,7", "iPad8,8", "iPad8,1", "iPad8,2", "iPad8,3", "iPad8,4", "iPad7,5",
				"iPad7,6", "iPad7,3", "iPad7,4", "iPad7,1", "iPad7,2", "iPad6,11", "iPad6,12", "iPad6,7", "iPad6,8",
				"iPad6,3", "iPad6,4", "iPad5,3", "iPad5,4", "iPad4,1", "iPad4,2", "iPad4,3", "iPad3,4", "iPad3,5",
				"iPad3,6", "iPad3,1", "iPad3,2", "iPad3,3"
			},
			264
		),
		new(new[] { "iPad2,5", "iPad2,6", "iPad2,7" }, 163),
		new(new[] { "iPad2,1", "iPad2,2", "iPad2,3", "iPad2,4" }, 132)
	};

	#endregion
}