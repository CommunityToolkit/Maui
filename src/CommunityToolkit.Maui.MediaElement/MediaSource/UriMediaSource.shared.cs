using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Maui.Controls;
using UriTypeConverter = Microsoft.Maui.Controls.UriTypeConverter;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// Represents a source, loaded from a remote URI, that can be played by <see cref="MediaElement"/>.
/// </summary>
public sealed partial class UriMediaSource : MediaSource
{
	/// <summary>
	/// Bindable property for the <see cref="Uri"/> property.
	/// </summary>
	public static readonly BindableProperty UriProperty =
		BindableProperty.Create(nameof(Uri), typeof(Uri), typeof(UriMediaSource), propertyChanged: OnUriSourceChanged, validateValue: UriValueValidator);

	/// <summary>
	/// Initializes <see cref="UriMediaSource"/>
	/// </summary>
	public UriMediaSource()
	{
		((ObservableDictionary<string, string>)HttpHeaders).ContentsChanged += HandleHeadersContentsChanged;
	}

	/// <summary>
	/// An implicit operator to convert a string value into a <see cref="UriMediaSource"/>.
	/// </summary>
	/// <param name="uri">The URI string to convert into a <see cref="UriMediaSource"/>.</param>
	public static implicit operator UriMediaSource(string uri) => (UriMediaSource)FromUri(uri);

	/// <summary>
	/// An implicit operator to convert a <see cref="UriMediaSource"/> into a string value.
	/// </summary>
	/// <param name="uriMediaSource">A <see cref="UriMediaSource"/> instance to convert to a string value.</param>
	public static implicit operator string?(UriMediaSource? uriMediaSource) => uriMediaSource?.Uri?.ToString();

	/// <summary>
	/// Gets the HTTP headers to include in the request when loading the media from <see cref="Uri"/>.
	/// </summary>
	/// <remarks>
	/// Use this to provide authentication tokens (e.g. <c>Authorization: Bearer &lt;token&gt;</c>) or other custom HTTP headers.
	/// Mutating the contents of the returned dictionary triggers a source update on the underlying platform player.
	/// Not supported on Tizen.
	/// </remarks>
	public IDictionary<string, string> HttpHeaders { get; } = new ObservableDictionary<string, string>();

	/// <summary>
	/// Gets or sets the URI to use as a media source.
	/// This is a bindable property.
	/// </summary>
	/// <remarks>The URI has to be absolute.</remarks>
	[TypeConverter(typeof(UriTypeConverter))]
	public Uri? Uri
	{
		get => (Uri?)GetValue(UriProperty);
		set => SetValue(UriProperty, value);
	}

	/// <inheritdoc/>
	public override string ToString() => $"Uri: {Uri}";

	static bool UriValueValidator(BindableObject bindable, object value) =>
		value is null || ((Uri)value).IsAbsoluteUri;

	static void OnUriSourceChanged(BindableObject bindable, object oldValue, object newValue) =>
		((UriMediaSource)bindable).OnSourceChanged();

	void HandleHeadersContentsChanged(object? sender, EventArgs e)
	{
		OnSourceChanged();
	}

	sealed class ObservableDictionary<K, V> : IDictionary<K, V> where K : notnull
	{
		readonly IDictionary<K, V> innerDictionary = new Dictionary<K, V>();

		public event EventHandler? ContentsChanged;

		public int Count => innerDictionary.Count;
		public bool IsReadOnly => false;

		public V this[K key]
		{
			get => innerDictionary[key];
			set
			{
				if (innerDictionary.TryGetValue(key, out var existingValue) && EqualityComparer<V>.Default.Equals(existingValue, value))
				{
					return;
				}

				innerDictionary[key] = value;
				ContentsChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		public ICollection<K> Keys => innerDictionary.Keys;
		public ICollection<V> Values => innerDictionary.Values;

		public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => innerDictionary.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public void Add(KeyValuePair<K, V> item)
		{
			innerDictionary.Add(item.Key, item.Value);
			ContentsChanged?.Invoke(this, EventArgs.Empty);
		}

		public void Clear()
		{
			innerDictionary.Clear();
			ContentsChanged?.Invoke(this, EventArgs.Empty);
		}

		public bool Contains(KeyValuePair<K, V> item) => innerDictionary.Contains(item);

		public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex) => innerDictionary.CopyTo(array, arrayIndex);

		public bool Remove(KeyValuePair<K, V> item)
		{
			var isRemoved = innerDictionary.Remove(item);
			if (isRemoved)
			{
				ContentsChanged?.Invoke(this, EventArgs.Empty);
			}

			return isRemoved;
		}

		public void Add(K key, V value)
		{
			innerDictionary.Add(key, value);
			ContentsChanged?.Invoke(this, EventArgs.Empty);
		}

		public bool ContainsKey(K key) => innerDictionary.ContainsKey(key);

		public bool Remove(K key)
		{
			var isRemoved = innerDictionary.Remove(key);
			if (isRemoved)
			{
				ContentsChanged?.Invoke(this, EventArgs.Empty);
			}

			return isRemoved;
		}

		public bool TryGetValue(K key, [MaybeNullWhen(false)] out V value) => innerDictionary.TryGetValue(key, out value);
	}
}