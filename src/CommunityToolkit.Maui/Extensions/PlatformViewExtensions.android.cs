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
/// </remarks>
static class PlatformViewExtensions
{
	/// <summary>
	/// Walks up the native view tree and returns the first parent of type <typeparamref name="T"/>.
	/// </summary>
	public static T? GetParentOfType<T>(this AView? view) where T : class
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
	/// Returns <see langword="true"/> when the peer's native handle has already been released.
	/// </summary>
	public static bool IsDisposed(this JavaObject javaObject)
	{
		ArgumentNullException.ThrowIfNull(javaObject);

		return javaObject.Handle == IntPtr.Zero;
	}

	/// <summary>
	/// Returns <see langword="true"/> when the peer is non-<see langword="null"/> and its native handle is still valid.
	/// </summary>
	public static bool IsAlive(this JavaObject? javaObject) => javaObject is not null && javaObject.Handle != IntPtr.Zero;
}
