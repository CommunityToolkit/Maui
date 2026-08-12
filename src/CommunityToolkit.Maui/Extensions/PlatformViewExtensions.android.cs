using Android.Views;
using AView = Android.Views.View;
using JavaObject = Java.Lang.Object;

namespace CommunityToolkit.Maui.Extensions;

/// <summary>
/// Android platform helpers owned by the toolkit.
/// </summary>
/// <remarks>
/// These replace equivalents that used to be consumed from .NET MAUI's internal
/// <c>Microsoft.Maui.Platform.ViewExtensions</c> and <c>Microsoft.Maui.JavaObjectExtensions</c>.
/// Those are not part of MAUI's public API surface, so the toolkit owns them here instead.
/// <para>
/// The names deliberately differ from MAUI's (<c>GetParentOfType</c>, <c>IsAlive</c>, <c>IsDisposed</c>).
/// While MAUI still grants us <c>InternalsVisibleTo</c> those identical names are in scope at the same
/// time as these, which makes every call site ambiguous (CS0121). Distinct names bind correctly whether 
/// that grant is present.
/// </para>
/// </remarks>
static class PlatformViewExtensions
{
	/// <summary>
	/// Returns <paramref name="view"/> itself when it is a <typeparamref name="T"/>, otherwise walks up
	/// the native view tree and returns the first parent of type <typeparamref name="T"/>.
	/// </summary>
	public static T? FindParentOfType<T>(this AView? view) where T : class
	{
		if (view is T self)
		{
			return self;
		}

		IViewParent? parent = view?.Parent;

		while (parent is not null)
		{
			if (parent is T match)
			{
				return match;
			}

			parent = parent.Parent;
		}

		return null;
	}

	/// <summary>
	/// Returns <see langword="true"/> when the Java peer's native handle has already been released.
	/// </summary>
	public static bool IsPeerDisposed(this JavaObject javaObject)
	{
		ArgumentNullException.ThrowIfNull(javaObject);

		return javaObject.Handle == IntPtr.Zero;
	}

	/// <summary>
	/// Returns <see langword="true"/> when the Java peer is non-<see langword="null"/> and its native handle is still valid.
	/// </summary>
	public static bool IsPeerAlive(this JavaObject? javaObject) => javaObject is not null && javaObject.Handle != IntPtr.Zero;
}
