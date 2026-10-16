using System.Reflection;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// Makes StatusBar or NavigationBar believe that they run on an Android version older than API 23, where both do nothing. Each reads its
/// support flag from a private static <see cref="Lazy{T}"/> that evaluates the OS version once. The pools have no Android 5 device (they run
/// API 28 and 36), so this scope replaces the value that the flag holds (the Lazy's private <c>_value</c>) and puts the real value back when it
/// is disposed. It reaches the guard arms that SetColor, SetStyle, UpdateBarSize and the navigation bar mappings take on an unsupported version.
/// The factory of the Lazy (its Trace.WriteLine and its false result below API 23) does not run here: that arm needs a real old device and keeps
/// its visible skip.
/// </summary>
sealed class UnsupportedAndroidVersion : IDisposable
{
	static readonly FieldInfo lazyValueField = typeof(Lazy<bool>).GetField("_value", BindingFlags.Instance | BindingFlags.NonPublic)
		?? throw new InvalidOperationException("Lazy<bool> no longer keeps its value in a private '_value' field.");

	readonly Lazy<bool> supportFlag;
	readonly bool realValue;

	UnsupportedAndroidVersion(Type owner, string fieldName)
	{
		var field = owner.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic)
			?? throw new InvalidOperationException($"{owner.Name} no longer has a private static field '{fieldName}'.");
		supportFlag = field.GetValue(null) as Lazy<bool>
			?? throw new InvalidOperationException($"{owner.Name}.{fieldName} is no longer a Lazy<bool>.");

		// Reading the value runs the factory once; a Lazy that holds its value only returns _value from then on.
		realValue = supportFlag.Value;
		lazyValueField.SetValue(supportFlag, false);

		if (supportFlag.Value)
		{
			lazyValueField.SetValue(supportFlag, realValue);
			throw new InvalidOperationException($"{owner.Name}.{fieldName} still reports support after its value was replaced.");
		}
	}

	/// <summary>Marks CommunityToolkit.Maui.Core.Platform.StatusBar (StatusBar.android.cs) as not supported.</summary>
	public static UnsupportedAndroidVersion ForStatusBar() => new(typeof(CommunityToolkit.Maui.Core.Platform.StatusBar), "isSupportedHolder");

	/// <summary>Marks CommunityToolkit.Maui.PlatformConfiguration.AndroidSpecific.NavigationBar (NavigationBar.android.cs) as not supported.</summary>
	public static UnsupportedAndroidVersion ForNavigationBar() => new(typeof(CommunityToolkit.Maui.PlatformConfiguration.AndroidSpecific.NavigationBar), "isSupportedHandler");

	public void Dispose() => lazyValueField.SetValue(supportFlag, realValue);
}